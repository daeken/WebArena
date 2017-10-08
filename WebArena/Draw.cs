using Bridge.WebGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebArena {
	class Draw {
		WebGLRenderingContext gl;
		public Draw(WebGLRenderingContext gl) {
			this.gl = gl;

			gl.ClearColor(0, 0, 1, 1);
		}

		public void Render() {
			gl.Clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
		}
	}
}
