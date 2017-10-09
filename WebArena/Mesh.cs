using Bridge.Html5;
using Bridge.WebGL;
using System;
using static WebArena.Globals;

namespace WebArena {
	class Mesh {
		Program Program;
		WebGLBuffer IndexBuffer, PositionBuffer, NormalBuffer;
		int NumIndices;

		public Mesh(uint[] indices, float[] positions, float[] normals) {
			Program = new Program(@"
				precision highp float;
				attribute vec4 aVertexPosition;
				attribute vec3 aVertexNormal;

				uniform mat4 uModelViewMatrix;
				uniform mat4 uProjectionMatrix;

				varying vec3 vNormal;
				varying vec4 vPosition;

				void main() {
					vPosition = uModelViewMatrix * aVertexPosition.xzyw;
					gl_Position = uProjectionMatrix * vPosition;
					vNormal = aVertexNormal;
				}
			", @"
				precision highp float;
				varying vec3 vNormal;
				varying vec4 vPosition;

				float calcLight(vec3 lightvec) {
					return max(dot(vNormal, lightvec), 0.1);
				}
				
				void main() {
					gl_FragColor = vec4(vec3(calcLight(vec3(.2, -1, 0)) + calcLight(vec3(.3, .75, .5)) + calcLight(vec3(-.3, -.75, -.5))) * 0.75, 1.0);
				}
			");

			IndexBuffer = gl.CreateBuffer();
			gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, IndexBuffer);
			gl.BufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(indices), gl.STATIC_DRAW);

			PositionBuffer = gl.CreateBuffer();
			gl.BindBuffer(gl.ARRAY_BUFFER, PositionBuffer);
			gl.BufferData(gl.ARRAY_BUFFER, new Float32Array(positions), gl.STATIC_DRAW);

			NormalBuffer = gl.CreateBuffer();
			gl.BindBuffer(gl.ARRAY_BUFFER, NormalBuffer);
			gl.BufferData(gl.ARRAY_BUFFER, new Float32Array(normals), gl.STATIC_DRAW);

			NumIndices = indices.Length;
		}

		public void Draw() {
			Program.Use();
			Program.SetUniform("uProjectionMatrix", ProjectionMatrix);
			Program.SetUniform("uModelViewMatrix", PlayerCamera.Matrix);

			gl.BindBuffer(gl.ARRAY_BUFFER, PositionBuffer);
			var attr = Program.GetAttribute("aVertexPosition");
			gl.VertexAttribPointer(attr, 3, gl.FLOAT, false, 0, 0);
			gl.EnableVertexAttribArray(attr);
			gl.BindBuffer(gl.ARRAY_BUFFER, NormalBuffer);
			attr = Program.GetAttribute("aVertexNormal");
			gl.VertexAttribPointer(attr, 3, gl.FLOAT, false, 0, 0);
			gl.EnableVertexAttribArray(attr);
			gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, IndexBuffer);
			gl.DrawElements(gl.TRIANGLES, NumIndices, gl.UNSIGNED_SHORT, 0);
		}
	}
}
