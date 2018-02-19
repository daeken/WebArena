using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MoreLinq;
using Newtonsoft.Json;
using static System.Console;

namespace Converter {
	public static class Md3Converter {
		[StructLayout(LayoutKind.Sequential)]
		struct Md3Header {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
			public readonly byte[] MagicBytes;
			public readonly int Version;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=64)]
			public readonly byte[] NameBytes;
			public readonly int Flags;
			public readonly int FrameCount, TagCount, SurfaceCount, SkinCount;
			public readonly int FrameOffset, TagOffset, SurfaceOffset, EofOffset;
			
			public string Magic => Program.BytesToString(MagicBytes);
			public string Name => Program.BytesToString(NameBytes);
		}

		[StructLayout(LayoutKind.Sequential)]
		struct Md3Frame {
			public readonly Vec3 Mins, Maxs, LocalOrigin;
			public float Radius;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=16)]
			public readonly byte[] NameBytes;
			
			public string Name => Program.BytesToString(NameBytes);
		}

		[StructLayout(LayoutKind.Sequential)]
		struct Md3Tag {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=64)]
			public readonly byte[] NameBytes;
			public readonly Vec3 Origin;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
			public readonly float[] Axis;
			
			public string Name => Program.BytesToString(NameBytes);
		}

		[StructLayout(LayoutKind.Sequential)]
		struct Md3SurfaceHeader {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
			public readonly byte[] MagicBytes;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=64)]
			public readonly byte[] NameBytes;
			public readonly int Flags;
			public readonly int FrameCount, ShaderCount, VertexCount, TriangleCount;
			public readonly int TriangleOffset, ShaderOffset, StOffset, XyzNormalOffset, EndOffset;
			
			public string Magic => Program.BytesToString(MagicBytes);
			public string Name => Program.BytesToString(NameBytes);
		}

		struct Md3Surface : ICustomBinaryUnpacker<Md3Surface> {
			public string Magic, Name;
			public int Flags;
			public Md3Shader[] Shaders;
			public Md3Triangle[] Triangles;
			public Vec2[] TexCoords;
			public Md3Vertex[] Vertices;
			
			public Md3Surface Unpack(BinaryFileReader bread) {
				var pos = bread.Tell;
				var header = bread.ReadStruct<Md3SurfaceHeader>();
				Magic = header.Magic;
				Name = header.Name;
				Flags = header.Flags;
				Shaders = bread.ReadStructs<Md3Shader>(header.ShaderCount, pos + header.ShaderOffset);
				Triangles = bread.ReadStructs<Md3Triangle>(header.TriangleCount, pos + header.TriangleOffset);
				TexCoords = bread.ReadStructs<Vec2>(header.VertexCount, pos + header.StOffset);
				Vertices = bread.ReadStructs<Md3Vertex>(header.VertexCount * header.FrameCount, pos + header.XyzNormalOffset);
				bread.Seek(pos + header.EndOffset);
				return this;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		struct Md3Shader {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=64)]
			public readonly byte[] NameBytes;
			public readonly int Index;
			
			public string Name => Program.BytesToString(NameBytes);
		}

		[StructLayout(LayoutKind.Sequential)]
		struct Md3Triangle {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public readonly int[] Indices;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct Md3Vertex {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public readonly short[] Coord;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public readonly byte[] Normal;
		}

		class Jmd3File {
			public Dictionary<string, float[]> RawTags;
			public List<Jmd3Mesh> Meshes;
			public Dictionary<string, Dictionary<string, string>> Skins;
		}

		class Jmd3Mesh {
			public string Name;
			public float[][] Frames;
			public float[] Texcoords;
			public int[] Indices;
		}

		static Quaternion Mat2Quat(float[] mat) {
			float m11 = mat[0], m12 = mat[1], m13 = mat[2],
				  m21 = mat[3], m22 = mat[4], m23 = mat[5], 
				  m31 = mat[6], m32 = mat[7], m33 = mat[8];
			var trace = m11 + m22 + m33;

			if(trace > 0) {
				var s = (float) (0.5 / Math.Sqrt(trace + 1));
				return new Quaternion((m32 - m23) * s, (m13 - m31) * s, (m21 - m12) * s, 0.25f / s);
			} else if(m11 > m22 && m11 > m33) {
				var s = (float) (2 * Math.Sqrt(1 + m11 - m22 - m33));
				return new Quaternion(0.25f * s, (m12 + m21) / s, (m13 + m31) / s, (m32 - m23) / s);
			} else if(m22 > m33) {
				var s = (float) (2 * Math.Sqrt(1 + m22 - m11 - m33));
				return new Quaternion((m12 + m21) / s, 0.25f * s, (m23 + m32) / s, (m13 - m31) / s);
			} else {
				var s = (float) (2 * Math.Sqrt(1 + m33 - m11 - m22));
				return new Quaternion((m13 + m31) / s, (m23 + m32) / s, 0.25f * s, (m21 - m12) / s);
			}
		}

		public static void Convert(string fn, byte[] istream, Stream ostream) {
			string CutLastUnderscore(string data) {
				if(!data.Contains('_'))
					return data;
				var sub = data.Split('_');
				return string.Join('_', sub.Take(sub.Length - 1));
			}
			
			var skins = new Dictionary<string, Dictionary<string, string>>();
			bool LoadSkins(string tfn) {
				var found = false;
				foreach(var sfn in AssetManager.Instance.FindFilesByPrefix(tfn))
					if(sfn.EndsWith(".skin")) {
						found = true;
						var sf = Encoding.ASCII.GetString(AssetManager.Instance.Open(sfn));
						skins[sfn.Substring(tfn.Length + 1, sfn.Length - tfn.Length - 6)] = 
							sf.Trim().Split('\n').Select(line => line.Split(',', 2)).Where(x => x.Length == 2 && x[1].Trim().Length != 0)
								.ToDictionary(x => x[0].Trim(), x => x[1].Trim());
					}
				return found;
			}

			if(!LoadSkins(Path.ChangeExtension(fn, null)) && Path.GetFileName(fn).Contains('_'))
				LoadSkins(CutLastUnderscore(Path.ChangeExtension(fn, null)));

			if(!skins.ContainsKey("default"))
				skins["default"] = new Dictionary<string, string>();

			var bread = new BinaryFileReader(istream);
			var header = bread.ReadStruct<Md3Header>();
			Debug.Assert(header.Magic == "IDP3");
			
			var frames = bread.ReadStructs<Md3Frame>(header.FrameCount, header.FrameOffset);
			var tags = bread.ReadStructs<Md3Tag>(header.TagCount * header.FrameCount, header.TagOffset);
			var surfaces = bread.ReadStructs<Md3Surface>(header.SurfaceCount, header.SurfaceOffset);

			var otags = new Dictionary<string, List<(Vec3, Quaternion)>>();
			for(var i = 0; i < header.TagCount; ++i) {
				var name = tags[i].Name;
				var cur = otags[name] = new List<(Vec3, Quaternion)>();
				for(var j = 0; j < header.FrameCount; ++j) {
					var tag = tags[j * header.TagCount + i];
					Debug.Assert(name == tag.Name);
					cur.Add((tag.Origin, Mat2Quat(tag.Axis)));
				}
			}

			var meshes = new List<Jmd3Mesh>();
			foreach(var surface in surfaces) {
				var sname = surface.Name;
				if(!skins["default"].ContainsKey(surface.Name)) {
					if(skins["default"].ContainsKey(CutLastUnderscore(surface.Name)))
						sname = CutLastUnderscore(surface.Name);
					else if(surface.Shaders.Length != 0 && surface.Shaders[0].Name.Length != 0)
						skins["default"][surface.Name] = surface.Shaders[0].Name;
					else
						WriteLine($"Can't find texture for surface {surface.Name} in {fn}");
				}

				var off = 0;
				var sframes = Enumerable.Range(0, header.FrameCount).Select(_ => 
					Enumerable.Range(0, surface.Vertices.Length / header.FrameCount).SelectMany(__ => {
						var vert = surface.Vertices[off++];
						var lat = vert.Normal[0] * 2 * Math.PI / 255;
						var lon = vert.Normal[1] * 2 * Math.PI / 255;
						return new[] {
							vert.Coord[0] / 64f, vert.Coord[1] / 64f, vert.Coord[2] / 64f,
							(float) (Math.Cos(lon) * Math.Sin(lat)), (float) (Math.Sin(lon) * Math.Sin(lat)), (float) Math.Cos(lat)
						};
					}).ToArray()
				).ToArray();
				
				meshes.Add(new Jmd3Mesh {
					Name = sname,
					Frames = sframes, 
					Texcoords = surface.TexCoords.SelectMany(x => x.ToArray).ToArray(), 
					Indices = surface.Triangles.SelectMany(x => x.Indices).ToArray()
				});
			}

			foreach(var (sk, skin) in skins) {
				if(sk == "default") continue;
				foreach(var (k, v) in skins["default"])
					if(!skin.ContainsKey(k))
						skin[k] = v;
			}

			foreach(var skin in skins.Values) {
				var replace = new Dictionary<string, string>();
				foreach(var (k, v) in skin)
					replace[k] = TextureConverter.Instance.Convert(v);
				foreach(var (k, v) in replace)
					skin[k] = v;
			}

			var output = new Jmd3File {
				RawTags = otags.Select(kv => (kv.Key, kv.Value.SelectMany(x => new [] { x.Item1.ToArray, x.Item2.ToArray() }).SelectMany(x => x).ToArray())).ToDictionary(x => x.Item1, x => x.Item2), 
				Meshes = meshes, 
				Skins = skins
			};
			using(var tw = new StreamWriter(ostream))
				tw.Write(JsonConvert.SerializeObject(output));
		}
	}
}