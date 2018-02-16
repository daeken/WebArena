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
		public readonly BspCollisionTree CollisionTree;
		
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
			
			var bmat = new Material(@"
				void main() {
					gl_FragColor = vec4(1.0, 0.0, 0.0, 0.2);
				}
			");
			bmat.SetBlend(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
			
			var planes = data.Planes.Select(plane => new BspCollisionPlane { Normal=new Vec3(plane.Normal), Distance=plane.Distance }).ToArray();
			var brushes = data.Brushes.Select(brush => new BspCollisionBrush { Collidable=brush.Collidable, Planes=brush.Planes.Select(x => planes[x]).ToArray() }).ToArray();

			CollisionTree = ConvertTree(data.Tree, planes, brushes);

			/*var indices = new List<uint>();
			var vertices = new List<Vec3>();
			foreach(var leaf in FindLeaves(CollisionTree)) {
				var mins = leaf.Mins;
				var maxs = leaf.Maxs;
				
				foreach(var brush in leaf.Brushes) {
					if(!brush.Collidable)
						continue;
					foreach(var plane in brush.Planes) {
						var points = new List<Vec3>();
						
						var dir = vec3(maxs.X - mins.X, 0, 0);
						CheckAddPoint(points, plane, mins, dir);
						CheckAddPoint(points, plane, vec3(mins.X, maxs.Y, mins.Z), dir);
						CheckAddPoint(points, plane, vec3(mins.X, mins.Y, maxs.Z), dir);
						CheckAddPoint(points, plane, vec3(mins.X, maxs.Y, maxs.Z), dir);

						dir = vec3(0, maxs.Y - mins.Y, 0);
						CheckAddPoint(points, plane, mins, dir);
						CheckAddPoint(points, plane, vec3(maxs.X, mins.Y, mins.Z), dir);
						CheckAddPoint(points, plane, vec3(mins.X, mins.Y, maxs.Z), dir);
						CheckAddPoint(points, plane, vec3(maxs.X, mins.Y, maxs.Z), dir);
						
						dir = vec3(0, 0, maxs.Z - mins.Z);
						CheckAddPoint(points, plane, mins, dir);
						CheckAddPoint(points, plane, vec3(maxs.X, mins.Y, mins.Z), dir);
						CheckAddPoint(points, plane, vec3(mins.X, maxs.Y, mins.Z), dir);
						CheckAddPoint(points, plane, vec3(maxs.X, maxs.Y, mins.Z), dir);

						if(points.Count == 0)
							continue;
						SortPoints(points, plane.Normal);

						points = points.Select(v => v + plane.Normal * 1).ToList();

						var indoff = vertices.Count;
						vertices = vertices.Concat(points).ToList();

						var pc = points.Count;

						for(var i = 1; i < pc - 1; ++i) {
							indices.Add((uint) indoff);
							indices.Add((uint) (indoff + i));
							indices.Add((uint) (indoff + i + 1));
						}
					}
				}
			}
			
			var pmesh = new Mesh(indices.ToArray(), new [] {bmat}, Texture.White);
			pmesh.Add(new MeshBuffer(VertexFormat.Position, vertices.SelectMany(x => x.ToArray()).ToArray()));
			AddMesh(pmesh);*/
		}

		void DrawAABB(List<uint> indices, List<Vec3> vertices, Vec3 min, Vec3 max) {
			var A = min;
			var B = vec3(min.X, max.Y, min.Z);
			var C = vec3(max.X, max.Y, min.Z);
			var D = vec3(max.X, min.Y, min.Z);

			var E = vec3(min.X, min.Y, max.Z);
			var F = vec3(min.X, max.Y, max.Z);
			var G = vec3(max.X, max.Y, max.Z);
			var H = vec3(max.X, min.Y, max.Z);
			
			DrawRect(indices, vertices, A, B, C, D);
			DrawRect(indices, vertices, E, F, G, H);
		}

		void DrawRect(List<uint> indices, List<Vec3> vertices, Vec3 A, Vec3 B, Vec3 C, Vec3 D) {
			var points = new List<Vec3>(new [] { A, B, C, D });
			var normal = (A - C) ^ (B - D);
			SortPoints(points, normal);
			var indoff = vertices.Count;
			vertices.Add(points[0]);
			vertices.Add(points[1]);
			vertices.Add(points[2]);
			vertices.Add(points[3]);
			
			indices.Add((uint) (indoff + 0));
			indices.Add((uint) (indoff + 1));
			indices.Add((uint) (indoff + 2));
			
			indices.Add((uint) (indoff + 0));
			indices.Add((uint) (indoff + 2));
			indices.Add((uint) (indoff + 3));
		}

		void CheckAddPoint(List<Vec3> points, BspCollisionPlane plane, Vec3 origin, Vec3 dir) {
			double t, vd;
			if(RayPlaneIntersection(origin, dir, plane.Normal, plane.Distance, out t, out vd) && t >= 0 && t <= 1)
				points.Add(origin + dir * t);
		}

		IEnumerable<BspCollisionTree> FindLeaves(BspCollisionTree node) =>
			node.Leaf ? (IEnumerable<BspCollisionTree>) new [] { node } : FindLeaves(node.Left).Concat(FindLeaves(node.Right));
		
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
