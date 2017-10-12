using Bridge.Html5;
using Bridge.WebGL;
using System;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	class Mesh {
		WebGLBuffer IndexBuffer, VertexBuffer;
		int NumIndices;
		Material[] Materials;
		Texture Lightmap;
		
		public Mesh(uint[] indices, float[] vb, Material[] materials, Texture lightmap) {
			IndexBuffer = gl.CreateBuffer();
			gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, IndexBuffer);
			gl.BufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(indices), gl.STATIC_DRAW);

			VertexBuffer = gl.CreateBuffer();
			gl.BindBuffer(gl.ARRAY_BUFFER, VertexBuffer);
			gl.BufferData(gl.ARRAY_BUFFER, new Float32Array(vb), gl.STATIC_DRAW);

			NumIndices = indices.Length;
			Materials = materials;
			Lightmap = lightmap;
		}

		public void Draw(bool transparent) {
			if(Materials[0].Transparent != transparent)
				return;
			foreach(var material in Materials) {
				material.Use(VertexBuffer, Lightmap);
				gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, IndexBuffer);
				gl.DrawElements(gl.TRIANGLES, NumIndices, gl.UNSIGNED_SHORT, 0);
			}
		}
	}
}
