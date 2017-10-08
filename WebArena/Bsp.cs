using Bridge;
using Bridge.Html5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Console;

namespace WebArena {
	class BspData {
		public uint[] Indices { get; set; }
		public float[] VertexPositions { get; set; }
		public float[] VertexNormals { get; set; }
	}
	class Bsp : Model {
		public Bsp(BspData data) {
			var mesh = new Mesh(data.Indices, data.VertexPositions, data.VertexNormals);
			AddMesh(mesh);
		}
	}
}
