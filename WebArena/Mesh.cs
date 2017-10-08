using Bridge.Html5;
using Bridge.WebGL;
using static WebArena.Globals;

namespace WebArena {
	class Mesh {
		Program Program;
		WebGLBuffer IndexBuffer, PositionBuffer, NormalBuffer;

		public Mesh(uint[] indices, float[] positions, float[] normals) {
			Program = new Program(@"
				attribute vec4 aVertexPosition;

				uniform mat4 uModelViewMatrix;
				uniform mat4 uProjectionMatrix;

				void main() {
					gl_Position = uProjectionMatrix * uModelViewMatrix * aVertexPosition;
				}
			", @"
				void main() {
					gl_FragColor = vec4(1.0, 1.0, 1.0, 1.0);
				}
			");
			IndexBuffer = gl.CreateBuffer();
			gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, IndexBuffer);
			gl.BufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint32Array(indices), gl.STATIC_DRAW);

			PositionBuffer = gl.CreateBuffer();
			gl.BindBuffer(gl.ARRAY_BUFFER, PositionBuffer);
			gl.BufferData(gl.ARRAY_BUFFER, new Float32Array(positions), gl.STATIC_DRAW);

			NormalBuffer = gl.CreateBuffer();
			gl.BindBuffer(gl.ARRAY_BUFFER, NormalBuffer);
			gl.BufferData(gl.ARRAY_BUFFER, new Float32Array(normals), gl.STATIC_DRAW);
		}

		public void Draw() {
			Program.Use();

			var attr = Program.GetAttribute("aVertexPosition");
			gl.VertexAttribPointer(attr, 3, gl.FLOAT, false, 0, 0);
			gl.EnableVertexAttribArray(attr);
		}
	}
}
