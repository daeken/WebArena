using Bridge;
using Bridge.Html5;
using Bridge.WebGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Console;

namespace WebArena {
	class BspMaterialStage {
		public int[] Blend { get; set; }
		public bool Clamp { get; set; }
		public string Texture { get; set; }
		public string[] AnimTex { get; set; }
		public string FragShader { get; set; }
	}
	class BspMesh {
		public int MaterialIndex { get; set; }
		public int LightmapIndex { get; set; }
		public uint[] Indices { get; set; }
		public double[] Vertices { get; set; }
	}
	class BspPlane {
		public double[] Normal { get; set; }
		public double Distance { get; set; }
	}
	class BspBrush {
		public bool Collidable { get; set; }
		public int[] Planes { get; set; }
	}
	class BspTree {
		public bool Leaf { get; set; }
		public int Plane { get; set; }
		public int[] Mins { get; set; }
		public int[] Maxs { get; set; }
		public BspTree Left { get; set; }
		public BspTree Right { get; set; }
		public int[] Brushes { get; set; }
	}
	class BspData {
		public Dictionary<int, BspMaterialStage[]> Materials { get; set; }
		public BspMesh[] Meshes { get; set; }
		public Dictionary<int, byte[]> Lightmaps { get; set; }
		public BspPlane[] Planes { get; set; }
		public BspBrush[] Brushes { get; set; }
		public BspTree Tree { get; set; }
	}

	class BspCollisionPlane {
		public Vec3 Normal;
		public double Distance;
	}

	class BspCollisionBrush {
		public bool Collidable;
		public BspCollisionPlane[] Planes;
	}
	
	class BspCollisionTree {
		public bool Leaf;
		public BspCollisionPlane Plane;
		public Vec3 Mins, Maxs;
		public BspCollisionTree Left, Right;
		public BspCollisionBrush[] Brushes;
	}
	class Bsp : Model {
		public BspCollisionTree CollisionTree;
		
		public Bsp(BspData data) {
			var materials = new Dictionary<int, Material[]>();
			var lightmaps = new Dictionary<int, Texture>();
			foreach(var pair in data.Materials) {
				var allmats = materials[pair.Key] = new Material[pair.Value.Length];
				for(var i = 0; i < pair.Value.Length; ++i) {
					var mat = pair.Value[i];
					var wmat = allmats[i] = new Material(mat.FragShader);
					if(mat.Texture != null)
						wmat.AddTextures(0, new[] { mat.Texture }, mat.Clamp);
					else if(mat.AnimTex != null) {
						var textures = new string[mat.AnimTex.Length - 1];
						for(var j = 1; j < mat.AnimTex.Length; ++j)
							textures[j - 1] = mat.AnimTex[j];
						wmat.AddTextures(double.Parse(mat.AnimTex[0]), textures, false);
					}
					if(mat.Blend != null)
						wmat.SetBlend(mat.Blend[0], mat.Blend[1]);
				}
			}
			foreach(var pair in data.Lightmaps)
				lightmaps[pair.Key] = new Texture(pair.Value, 128, 128, 3);
			foreach(var mesh in data.Meshes) {
				var wmesh = new Mesh(mesh.Indices, materials[mesh.MaterialIndex], mesh.LightmapIndex != -1 ? lightmaps[mesh.LightmapIndex] : Texture.White);
				wmesh.Add(new MeshBuffer(VertexFormat.All, mesh.Vertices));
				AddMesh(wmesh);
			}

			var planes = data.Planes.Select(plane => new BspCollisionPlane { Normal=new Vec3(plane.Normal), Distance=plane.Distance }).ToArray();
			var brushes = data.Brushes.Select(brush => new BspCollisionBrush { Collidable=brush.Collidable, Planes=brush.Planes.Select(x => planes[x]).ToArray() }).ToArray();

			CollisionTree = ConvertTree(data.Tree, planes, brushes);
		}
		
		BspCollisionTree ConvertTree(BspTree node, BspCollisionPlane[] planes, BspCollisionBrush[] brushes) => new BspCollisionTree {
			Leaf=node.Leaf, 
			Plane=node.Plane != -1 ? planes[node.Plane] : null, 
			Mins=new Vec3(node.Mins), 
			Maxs=new Vec3(node.Maxs), 
			Left=ConvertTree(node.Left, planes, brushes), 
			Right=ConvertTree(node.Right, planes, brushes), 
			Brushes=node.Brushes.Select(x => brushes[x]).ToArray()
		};
	}
}
