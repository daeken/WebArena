using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	class Md3Mesh {
		public double[][] Frames { get; set; } // Position * Normal
		public double[] Texcoords { get; set; }
		public uint[] Indices { get; set; }
	}
	class Md3Data {
		public Md3Mesh[] Meshes { get; set; }
	}
	class Md3 : Model {
		class AniMesh : Mesh {
			public MeshBuffer[] FrameBuffers;
			AniMaterial[] Materials;

			double StartTime;
			int StartFrame, LoopStart, LoopEnd, Fps;

			public AniMesh(Md3Mesh data, AniMaterial[] materials) : base(data.Indices, materials, Texture.White) {
				Materials = materials;
				FrameBuffers = data.Frames.Select(x => new MeshBuffer(VertexFormat.Position | VertexFormat.Normal, x)).ToArray();
				Add(FrameBuffers[90]);
				Add(FrameBuffers[91]);
				Add(new MeshBuffer(VertexFormat.Texcoord, data.Texcoords));
			}

			public void SetAnimation(int start, int loopStart, int loopEnd, int fps) {
				Materials.ForEach(x => x.SetFPS(fps));
				StartTime = Time;
				StartFrame = start;
				LoopStart = loopStart;
				LoopEnd = loopEnd;
				Fps = fps;

				if(StartFrame == LoopEnd)
					Update();
			}

			public void Update(bool force = false) {
				if(StartFrame == LoopEnd) {
					if(!force)
						return;
					Buffers[0] = Buffers[1] = FrameBuffers[StartFrame];
				} else {
					var framesPassed = (int) Math.Floor((Time - StartTime) * Fps);
					var cur = StartFrame + framesPassed;
					if(cur > LoopEnd)
						cur = LoopStart + ((framesPassed - (LoopStart - StartFrame)) % (LoopEnd - LoopStart + 1));
					var sec = cur == LoopEnd ? LoopStart : cur + 1;
					Buffers[0] = FrameBuffers[cur];
					Buffers[1] = FrameBuffers[sec];
				}

				Buffers[0].Format &= ~VertexFormat.Second;
				Buffers[1].Format |=  VertexFormat.Second;
			}
		}

		AniMesh[] AniMeshes;

		public Md3(Md3Data data) {
			var mats = new AniMaterial[] { AniMaterial.FauxDiffuse };
			AniMeshes = data.Meshes.Select(x => new AniMesh(x, mats)).ToArray();
			AniMeshes.ForEach(AddMesh);
			AniMeshes.ForEach(x => x.SetAnimation(0, 0, 152, 20));
		}

		public override void Update() {
			AniMeshes.ForEach(x => x.Update());
		}
	}
}
