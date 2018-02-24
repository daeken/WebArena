using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using static System.Console;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MoreLinq;
using Newtonsoft.Json;

namespace Converter {
	static class BspConverter {
		[StructLayout(LayoutKind.Sequential)]
		struct BspHeader {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
			public readonly byte[] MagicBytes;
			public readonly int Version;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=17)]
			public readonly BspDirEntry[] DirEntries;
			
			public string Magic => Program.BytesToString(MagicBytes);
		}

		[StructLayout(LayoutKind.Sequential)]
		struct BspDirEntry {
			public readonly int Offset, Length;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		struct BspTexture {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=64)]
			public readonly byte[] NameBytes;
			public readonly int SurfaceFlags, ContentFlags;
			
			public string Name => Program.BytesToString(NameBytes);
		}

		[StructLayout(LayoutKind.Sequential)]
		struct BspPlane {
			public readonly Vec3 Normal;
			public readonly float Distance;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct BspNode {
			public readonly int Plane;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public readonly int[] Children;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public readonly int[] Mins;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public readonly int[] Maxs;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct BspLeaf {
			public readonly int Cluster, Area;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public int[] Mins;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public int[] Maxs;
			public readonly int LeafFace, LeafFaceCount;
			public readonly int LeafBrush, LeafBrushCount;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspLeafFace {
			public readonly int Face;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspLeafBrush {
			public readonly int Brush;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspModel {
			public readonly Vec3 Mins, Maxs;
			public readonly int Face, FaceCount;
			public readonly int Brush, BrushCount;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspBrush {
			public readonly int BrushSide, BrushSideCount;
			public readonly int Texture;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspBrushSide {
			public readonly int Plane, Texture;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspVertex {
			public Vec3 Position;
			public Vec2 TexCoord, LmCoord;
			public Vec3 Normal;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public byte[] Color;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspMeshVert {
			public readonly int Offset;
		}
		
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		struct BspEffect {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
			public readonly byte[] NameBytes;
			public readonly int brush, VisibleSide;
			
			public string Name => Program.BytesToString(NameBytes);
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspFace {
			public readonly int Texture, Effect, Type;
			public readonly int Vertex, VertexCount;
			public readonly int MeshVert, MeshVertCount;
			public readonly int LmIndex;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public readonly int[] LmStart, LmSize;
			public readonly Vec3 LmOrigin, LmVecS, LmVecT;
			public readonly Vec3 Normal;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public readonly int[] Size;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspLightmap {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128 * 128 * 3)]
			public readonly byte[] Pixels;
		}

		class JbspFile {
			public List<Dictionary<string, string>> Entities;
			public Dictionary<int, JLayer[]> Materials;
			public JbspMesh[] Meshes;
			public JbspPlane[] Planes;
			public JbspBrush[] Brushes;
			public JbspTree Tree;
			public int[][] Lightmaps;
		}

		class JbspMesh {
			public int MaterialIndex, LightmapIndex;
			public int[] Indices;
			public float[] Vertices;
		}

		class JbspPlane {
			public float[] Normal;
			public float Distance;
		}

		class JbspBrush {
			public bool Collidable;
			public int[] Planes;
		}

		class JbspTree {
			public bool Leaf;
			public int Plane;
			public int[] Mins, Maxs;
			public JbspTree Left, Right;
			public int[] Brushes;
		}

		struct PVertex {
			public Vec3 Position, Normal;
			public Vec2 TexCoord, LmCoord;
		}
		
		static int[] AdjustBrightness(byte[] ipixels) {
			var rpixels = new int[ipixels.Length];
			for(var i = 0; i < ipixels.Length; i += 3) {
				double r = ipixels[i + 0] * 4.0, g = ipixels[i + 1] * 4.0, b = ipixels[i + 2] * 4.0;
				var scale = 1.0;
				if(r > 255 && 255 / r < scale) scale = 255 / r;
				if(g > 255 && 255 / g < scale) scale = 255 / g;
				if(b > 255 && 255 / b < scale) scale = 255 / b;
				rpixels[i + 0] = (int) (r * scale);
				rpixels[i + 1] = (int) (g * scale);
				rpixels[i + 2] = (int) (b * scale);
			}
			return rpixels;
		}

		static (List<int>, List<PVertex>) SplitMesh(List<int> indices, List<PVertex> vertices) {
			var indmap = new Dictionary<int, int>();
			var outindices = new List<int>();
			var outvertices = new List<PVertex>();

			foreach(var ind in indices) {
				if(indmap.ContainsKey(ind))
					outindices.Add(indmap[ind]);
				else {
					var i = indmap.Count;
					outvertices.Add(vertices[ind]);
					indmap[ind] = i;
					outindices.Add(i);
				}
			}
			
			return (outindices, outvertices);
		}

		static JLayer[] CopyMaterial(JLayer[] material) => material.Select(layer => new JLayer {
			FragShader = layer.FragShader, 
			Blend = layer.Blend?.Select(x => x).ToArray(), 
			Textures = layer.Textures.Select(tex => new JTexture {
				AnimTex = tex.AnimTex?.Select(y => y).ToArray(), 
				Clamp = tex.Clamp, 
				Frequency = tex.Frequency, 
				Texture = tex.Texture
			}).ToArray()
		}).ToArray();

		static (List<int>, List<BspVertex>) Tesselate(int[] size, List<BspVertex> verts, List<int> meshverts) {
			BspVertex GetPoint(BspVertex c0, BspVertex c1, BspVertex c2, double dist) {
				Vec4 Sub(Vec4 v0, Vec4 v1, Vec4 v2) {
					var b = 1 - dist;
					return v0 * (b * b) + v1 * (2 * b * dist) + v2 * (dist * dist);
				}
				return new BspVertex {
					Position = Sub(c0.Position, c1.Position, c2.Position), 
					Normal = Sub(c0.Normal, c1.Normal, c2.Normal).Normalized, 
					TexCoord = Sub(c0.TexCoord, c1.TexCoord, c2.TexCoord), 
					LmCoord = Sub(c0.LmCoord, c1.LmCoord, c2.LmCoord) 
				};
			}
			
			var level = 5.0;
			var L1 = (int) level + 1;

			for(var py = 0; py < size[1] - 2; py += 2) {
				for(var px = 0; px < size[0] - 2; px += 2) {
					var rowOff = py * size[0];
					BspVertex c0 = verts[rowOff + px + 0], c1 = verts[rowOff + px + 1], c2 = verts[rowOff + px + 2];
					rowOff += size[0];
					BspVertex c3 = verts[rowOff + px + 0], c4 = verts[rowOff + px + 1], c5 = verts[rowOff + px + 2];
					rowOff += size[0];
					BspVertex c6 = verts[rowOff + px + 0], c7 = verts[rowOff + px + 1], c8 = verts[rowOff + px + 2];

					var indexOff = verts.Count;
					for(var i = 0; i < L1; ++i)
						verts.Add(GetPoint(c0, c3, c6, i / level));

					for(var i = 1; i < L1; ++i) {
						var a = i / level;

						var tc0 = GetPoint(c0, c1, c2, a);
						var tc1 = GetPoint(c3, c4, c5, a);
						var tc2 = GetPoint(c6, c7, c8, a);

						for(var j = 0; j < L1; ++j)
							verts.Add(GetPoint(tc0, tc1, tc2, j / level));
					}
					
					for(var row = 0; row < level; ++row)
					for(var col = 0; col < level; ++col) {
						meshverts.Add(indexOff + (row + 1) * L1 + col);
						meshverts.Add(indexOff + row * L1 + col);
						meshverts.Add(indexOff + row * L1 + col + 1);
						
						meshverts.Add(indexOff + (row + 1) * L1 + col);
						meshverts.Add(indexOff + row * L1 + col + 1);
						meshverts.Add(indexOff + (row + 1) * L1 + col + 1);
					}
				}
			}
			
			return (meshverts, verts);
		}

		static IEnumerable<Dictionary<string, string>> ParseEntities(string data) {
			data = data.Trim();
			Dictionary<string, string> cur = null;
			string name = null;
			while(true) {
				data = data.TrimStart();
				if(data.Length == 0)
					break;
				if(data[0] == '{') {
					data = data.Substring(1);
					cur = new Dictionary<string, string>();
				}  else if(data[0] == '"') {
					var s = "";
					var i = 1;
					while(true) {
						if(data[i] == '\\') {
							switch(data[++i]) {
								case 'n':
									s += "\n";
									break;
								case 'r':
									s += "\r";
									break;
								case 't':
									s += "\t";
									break;
								case '0':
									s += "\0";
									break;
								case '\\':
									s += "\\";
									break;
								case '"':
									s += "\"";
									break;
							}
							i++;
						} else if(data[i] == '"') {
							i++;
							break;
						} else
							s += data[i++];
					}
					data = data.Substring(i);
					if(name == null)
						name = s;
					else {
						cur[name] = s;
						name = null;
					}
				} else if(data[0] == '}') {
					data = data.Substring(1);
					yield return cur;
				}
			}
		}

		public static void Convert(byte[] istream, Stream ostream) {
			var materials = JsonConvert.DeserializeObject<Dictionary<string, JLayer[]>>(File.ReadAllText("output/materials.json"));
			
			var bread = new BinaryFileReader(istream);
			var header = bread.ReadStruct<BspHeader>();
			Debug.Assert(header.Magic == "IBSP");

			T[] GetLump<T>(int lump) where T : struct => bread.ReadMaxStructs<T>(header.DirEntries[lump].Length, header.DirEntries[lump].Offset);

			var textures = GetLump<BspTexture>(1);
			var planes = GetLump<BspPlane>(2);
			var nodes = GetLump<BspNode>(3);
			var leafs = GetLump<BspLeaf>(4);
			var leaffaces = GetLump<BspLeafFace>(5);
			var leafbrushes = GetLump<BspLeafBrush>(6);
			var models = GetLump<BspModel>(7);
			var brushes = GetLump<BspBrush>(8);
			var brushsides = GetLump<BspBrushSide>(9);
			var vertices = GetLump<BspVertex>(10);
			var meshverts = GetLump<BspMeshVert>(11);
			var effects = GetLump<BspEffect>(12);
			var faces = GetLump<BspFace>(13);
			var lightmaps = GetLump<BspLightmap>(14);

			bread.Seek(header.DirEntries[0].Offset);
			var entities = ParseEntities(Encoding.ASCII.GetString(bread.ReadBytes(header.DirEntries[0].Length - 1)));
			
			var outindices = new Dictionary<int, Dictionary<int, List<int>>>();
			var outvertices = new List<PVertex>();

			var model = models[0];
			foreach(var face in faces.Slice(model.Face, model.FaceCount)) {
				if(!outindices.ContainsKey(face.Texture))
					outindices[face.Texture] = new Dictionary<int, List<int>>();
				if(!outindices[face.Texture].ContainsKey(face.LmIndex))
					outindices[face.Texture][face.LmIndex] = new List<int>();
				var fmv = meshverts.Slice(face.MeshVert, face.MeshVertCount).Select(mv => mv.Offset).ToList();
				var fv = vertices.Slice(face.Vertex, face.VertexCount).ToList();
				var ci = outindices[face.Texture][face.LmIndex];
				switch(face.Type) {
					case 1: case 3:
						outindices[face.Texture][face.LmIndex] = ci.Concat(fmv.Select(mv => mv + outvertices.Count)).ToList();
						foreach(var vert in fv)
							outvertices.Add(new PVertex {
								Position = vert.Position, 
								Normal = vert.Normal, 
								TexCoord = vert.TexCoord, 
								LmCoord = vert.LmCoord
							});
						break;
					case 2:
						(fmv, fv) = Tesselate(face.Size, fv, fmv);
						outindices[face.Texture][face.LmIndex] = ci.Concat(fmv.Select(mv => mv + outvertices.Count)).ToList();
						foreach(var vert in fv)
							outvertices.Add(new PVertex {
								Position = vert.Position, 
								Normal = vert.Normal, 
								TexCoord = vert.TexCoord, 
								LmCoord = vert.LmCoord
							});
						break;
					case 4: // Billboards
						break;
				}
			}

			JbspTree convertTree(int ind) {
				if(ind >= 0) {
					var node = nodes[ind];
					return new JbspTree {
						Leaf = false, 
						Plane = node.Plane, 
						Mins = node.Mins, 
						Maxs = node.Maxs, 
						Left = convertTree(node.Children[0]), 
						Right = convertTree(node.Children[1]),
						Brushes = null
					};
				} else {
					var leaf = leafs[-(ind + 1)];
					return new JbspTree {
						Leaf = true, 
						Plane = -1, 
						Mins = leaf.Mins, 
						Maxs = leaf.Maxs, 
						Brushes = leafbrushes.Slice(leaf.LeafBrush, leaf.LeafBrushCount).Select(lb => lb.Brush).ToArray(), 
						Left = null, 
						Right = null
					};
				}
			}

			var outmaterials = new Dictionary<int, JLayer[]>();
			var outmeshes = new List<JbspMesh>();

			foreach(var mkey in outindices.Keys) {
				var name = textures[mkey].Name;

				JLayer[] material;
				if(materials.ContainsKey(name)) {
					material = materials[name];
					foreach(var layer in material) {
						foreach(var tex in layer.Textures) {
							tex.Texture = TextureConverter.Instance.Convert(tex.Texture);
							if(tex.AnimTex != null)
								for(var i = 0; i < tex.AnimTex.Length; ++i)
									tex.AnimTex[i] = TextureConverter.Instance.Convert(tex.AnimTex[i]);
						}
					}
				} else {
					material = CopyMaterial(materials["plaintexture"]);
					material[0].Textures[0].Texture = TextureConverter.Instance.Convert(name + ".jpg");
				}

				outmaterials[mkey] = material;

				foreach(var (lmkey, indices) in outindices[mkey]) {
					var (sind, svert) = SplitMesh(indices, outvertices);
					outmeshes.Add(new JbspMesh {
						MaterialIndex = mkey, 
						LightmapIndex = lmkey, 
						Indices = sind.ToArray(), 
						Vertices = svert.SelectMany(vert => new [] { vert.Position.ToArray, vert.Normal.ToArray, vert.TexCoord.ToArray, vert.LmCoord.ToArray }).SelectMany(x => x).ToArray()
					});
				}
			}
			
			var output = new JbspFile {
				Entities = entities.ToList(), 
				Materials = outmaterials, 
				Meshes = outmeshes.ToArray(), 
				Planes = planes.Select(plane => new JbspPlane { Normal=plane.Normal.ToArray, Distance=plane.Distance }).ToArray(), 
				Brushes = brushes.Select(brush => new JbspBrush {
					Collidable=(textures[brush.Texture].ContentFlags & 1) == 1, 
					Planes=brushsides.Slice(brush.BrushSide, brush.BrushSideCount).Select(bs => bs.Plane).ToArray()
				}).ToArray(), 
				Tree = convertTree(0), 
				Lightmaps = lightmaps.Select(lm => AdjustBrightness(lm.Pixels)).ToArray()
			};
			
			using(var tw = new StreamWriter(ostream))
				tw.Write(JsonConvert.SerializeObject(output));
		}
	}
}