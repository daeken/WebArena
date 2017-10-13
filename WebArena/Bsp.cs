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
	class BspData {
		public Dictionary<int, BspMaterialStage[]> Materials { get; set; }
		public BspMesh[] Meshes { get; set; }
		public Dictionary<int, byte[]> Lightmaps { get; set; }
	}
	class Bsp : Model {
		public Bsp(BspData data) {
			var materials = new Dictionary<int, Material[]>();
			var lightmaps = new Dictionary<int, Texture>();
			foreach(var pair in data.Materials) {
				var allmats = materials[pair.Key] = new Material[pair.Value.Length];
				for(var i = 0; i < pair.Value.Length; ++i) {
					var mat = pair.Value[i];
					var wmat = allmats[i] = new Material(mat.FragShader);
					if(mat.Texture != null)
						wmat.AddTextures(0, new string[] { mat.Texture }, mat.Clamp);
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
		}
	}
}
