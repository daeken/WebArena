using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using static System.Console;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MoreLinq;
using Newtonsoft.Json;

using JbspMaterial = System.Collections.Generic.List<Converter.BspConverter.MaterialPass>;

namespace Converter {
	public class BspConverter {
		[StructLayout(LayoutKind.Sequential)]
		struct BspHeader {
			public readonly uint Magic;
			public readonly int Version;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=17)]
			public readonly BspDirEntry[] DirEntries;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct BspDirEntry {
			public readonly int Offset, Length;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		struct BspTexture {
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)]
			public readonly string Name;
			public readonly int SurfaceFlags, ContentFlags;
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
			public readonly Vec3 Position;
			public readonly Vec2 TexCoord, LmCoord;
			public readonly Vec3 Normal;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public readonly byte[] Color;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct BspMeshVert {
			public readonly int Offset;
		}
		
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		struct BspEffect {
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
			public readonly string Name;
			public readonly int brush, VisibleSide;
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
			public Dictionary<int, JbspMaterial> Materials;
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
		
		public class MaterialPass {
			public bool Clamp;
			public string FragShader;
			public string Texture;
			public List<string> AnimTex;
			public int[] Blend;
		}
		
		int[] AdjustBrightness(byte[] ipixels) {
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

		(List<int>, List<PVertex>) SplitMesh(List<int> indices, List<PVertex> vertices) {
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

		JbspMaterial CopyMaterial(JbspMaterial material) => material.Select(pass => new MaterialPass {
			Clamp = pass.Clamp, 
			FragShader = pass.FragShader, 
			Texture = pass.Texture, 
			AnimTex = pass.AnimTex?.Select(x => x).ToList(), 
			Blend = pass.Blend?.Select(x => x).ToArray()
		}).ToList();

		public BspConverter(string fn) {
			var materials = JsonConvert.DeserializeObject<Dictionary<string, JbspMaterial>>(File.ReadAllText("materials.json"));
			
			var bread = new BinaryFileReader(fn);
			var header = bread.ReadStruct<BspHeader>();
			Debug.Assert(header.Magic == 0x50534249);

			T[] GetLump<T>(int lump) => bread.ReadMaxStructs<T>(header.DirEntries[lump].Length, header.DirEntries[lump].Offset);
			
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

			var outindices = new Dictionary<int, Dictionary<int, List<int>>>();
			var outvertices = new List<PVertex>();

			var model = models[0];
			foreach(var face in faces.Slice(model.Face, model.FaceCount)) {
				if(!outindices.ContainsKey(face.Texture))
					outindices[face.Texture] = new Dictionary<int, List<int>>();
				if(!outindices[face.Texture].ContainsKey(face.LmIndex))
					outindices[face.Texture][face.LmIndex] = new List<int>();
				var fmv = meshverts.Slice(face.MeshVert, face.MeshVertCount).Select(mv => mv.Offset);
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

			var outmaterials = new Dictionary<int, JbspMaterial>();
			var outmeshes = new List<JbspMesh>();

			foreach(var mkey in outindices.Keys) {
				var name = textures[mkey].Name;

				JbspMaterial material;
				if(materials.ContainsKey(name))
					material = materials[name];
				else {
					material = CopyMaterial(materials["plaintexture"]);
					material[0].Texture = name + ".jpg";
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
			
			WriteLine(JsonConvert.SerializeObject(output));
		}
	}
}