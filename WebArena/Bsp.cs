using Bridge;
using Bridge.Html5;
using Bridge.WebGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	class BspMaterialTexture {
		public bool Clamp { get; set; }
		public string Texture { get; set; }
		public string[] AnimTex { get; set; }
		public double Frequency { get; set; }
	}
	class BspMaterialLayer {
		public int[] Blend { get; set; }
		public string FragShader { get; set; }
		public BspMaterialTexture[] Textures { get; set; }
		public bool? DepthWrite { get; set; }
		public bool AlphaTested { get; set; }
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
		public List<Dictionary<string, string>> Entities { get; set; }
		public Dictionary<int, BspMaterialLayer[]> Materials { get; set; }
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
		public readonly BspCollisionTree CollisionTree;
		readonly List<Dictionary<string, string>> Entities;
		public readonly List<Vec3> SpawnPoints;

		List<Dictionary<string, string>> GetEntities(string cls) {
			var list = new List<Dictionary<string, string>>();
			foreach(var ent in Entities)
				if(ent["classname"] == cls)
					list.Add(ent);
			return list;
		}
		
		public Bsp(BspData data) {
			Entities = data.Entities;
			SpawnPoints = GetEntities("info_player_deathmatch").Select(x => new Vec3(x["origin"].Split(' ').Select(y => double.Parse(y)).ToArray())).ToList();
			
			var materials = new Dictionary<int, Material[]>();
			var lightmaps = new Dictionary<int, Texture>();
			foreach(var pair in data.Materials) {
				var allmats = materials[pair.Key] = new Material[pair.Value.Length];
				for(var i = 0; i < pair.Value.Length; ++i) {
					var mat = pair.Value[i];
					var wmat = allmats[i] = new Material(mat.FragShader) { DepthWrite = mat.DepthWrite, AlphaTested = mat.AlphaTested };
					foreach(var tex in mat.Textures) {
						if(tex.Texture != null)
							wmat.AddTextures(0, new[] {tex.Texture}, tex.Clamp);
						else if(tex.AnimTex != null)
							wmat.AddTextures(tex.Frequency, tex.AnimTex, false);
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
			
			var bmat = new Material(@"
				void main() {
					gl_FragColor = vec4(1.0, 0.0, 0.0, 0.2);
				}
			");
			bmat.SetBlend(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
			
			var planes = data.Planes.Select(plane => new BspCollisionPlane { Normal=new Vec3(plane.Normal), Distance=plane.Distance }).ToArray();
			var brushes = data.Brushes.Select(brush => new BspCollisionBrush { Collidable=brush.Collidable, Planes=brush.Planes.Select(x => planes[x]).ToArray() }).ToArray();

			CollisionTree = ConvertTree(data.Tree, planes, brushes);
		}

		BspCollisionTree ConvertTree(BspTree node, BspCollisionPlane[] planes, BspCollisionBrush[] brushes) => new BspCollisionTree {
			Leaf=node.Leaf, 
			Plane=node.Plane != -1 ? planes[node.Plane] : null, 
			Mins=new Vec3(node.Mins), 
			Maxs=new Vec3(node.Maxs), 
			Left=node.Left == null ? null : ConvertTree(node.Left, planes, brushes), 
			Right=node.Right == null ? null : ConvertTree(node.Right, planes, brushes), 
			Brushes=node.Brushes.Select(x => brushes[x]).ToArray()
		};
	}
}
