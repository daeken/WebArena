using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	class Md3Mesh {
		public string Name { get; set; }
		public double[][] Frames { get; set; } // Position * Normal
		public double[] Texcoords { get; set; }
		public uint[] Indices { get; set; }
	}
	class Md3Data {
		public Md3Mesh[] Meshes { get; set; }
		public Dictionary<string, double[]> RawTags { get; set; }
		public Dictionary<string, Tuple<Vec3, Quaternion>[]> Tags;

		public void ConvertTags() {
			Tags = new Dictionary<string, Tuple<Vec3, Quaternion>[]>();
			foreach(var pair in RawTags) {
				var frames = pair.Value;
				var arr = Tags[pair.Key] = new Tuple<Vec3, Quaternion>[frames.Length / 7];
				for(var i = 0; i < arr.Length; ++i) {
					var off = i * 7;
					arr[i] = new Tuple<Vec3, Quaternion>(vec3(frames[off + 0], frames[off + 1], frames[off + 2]), new Quaternion(frames[off + 3], frames[off + 4], frames[off + 5], frames[off + 6]));
				}
			}
		}
	}
	class Md3 : Model {
		class AniMesh : Mesh {
			public MeshBuffer[] FrameBuffers;
			public AniMaterial[] AniMaterials;

			public AniMesh(Md3Mesh data) : base(data.Indices, null, Texture.White) {
				var fs = @"
					uniform sampler2D uTexSampler;
					varying vec2 vTexCoord;
					void main() {
							gl_FragColor = texture2D(uTexSampler, vTexCoord);
					}
				";
				Materials = AniMaterials = new AniMaterial[] { new AniMaterial(fs) };
				Materials[0].AddTextures(0, new string[] { data.Name + ".png" }, false);
				FrameBuffers = data.Frames.Select(x => new MeshBuffer(VertexFormat.Position | VertexFormat.Normal, x)).ToArray();
				Add(FrameBuffers[0]);
				Add(FrameBuffers[0]);
				Add(new MeshBuffer(VertexFormat.Texcoord, data.Texcoords));
			}

			public void Update(int a, int b) {
				Buffers[0] = new Tuple<int, MeshBuffer>(0, FrameBuffers[a]);
				Buffers[1] = new Tuple<int, MeshBuffer>(1, FrameBuffers[b]);
			}
		}

		AniMesh[] AniMeshes;

		double StartTime;
		int StartFrame, LoopStart, LoopEnd, Fps;
		public int CurA, CurB;

		public Dictionary<string, Tuple<Vec3, Quaternion>[]> Tags;

		public int CurrentFrame {
			get {
				if(StartFrame == LoopEnd)
					return StartFrame;
				var framesPassed = (int) Math.Floor((Time - StartTime) * Fps);
				var cur = StartFrame + framesPassed;
				if(cur > LoopEnd)
					cur = LoopStart + ((framesPassed - (LoopStart - StartFrame)) % (LoopEnd - LoopStart + 1));
				return cur;
			}
		}

		public double CurrentLerp { get; private set; }

		public Md3(Md3Data data) {
			data.ConvertTags();
			AniMeshes = data.Meshes.Select(x => new AniMesh(x)).ToArray();
			AniMeshes.ForEach(AddMesh);
			CurA = CurB = 0;
			Tags = data.Tags;
			CurrentLerp = 0;
		}

		public void SetAnimation(int start, int loopStart, int loopEnd, int fps) {
			StartTime = Time;
			StartFrame = start;
			LoopStart = loopStart;
			LoopEnd = loopEnd;
			Fps = fps;
		}

		public override void Update() {
			if(StartFrame == LoopEnd && (CurA != StartFrame || CurB != StartFrame))
				return;
			CurA = CurrentFrame;
			CurB = CurA == LoopEnd ? LoopStart : CurA + 1;
			CurrentLerp = Fract((Time - StartTime) * Fps);
			AniMeshes.ForEach(x => x.Update(CurA, CurB));
		}

		public override void Draw(bool transparent) {
			foreach(var mesh in AniMeshes)
				foreach(var mat in mesh.AniMaterials)
					mat.Lerp = CurrentLerp;
			base.Draw(transparent);
		}
	}
}
