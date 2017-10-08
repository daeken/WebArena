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
	}
	class Bsp {
		public Bsp(BspData data) {
			var indices = new Uint32Array(data.Indices);
			WriteLine($"Index count: {indices.Length}");
			WriteLine($"First index {indices[0]}");
		}
	}
}
