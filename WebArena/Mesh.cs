﻿using Bridge.Html5;
using Bridge.WebGL;
using System;
using System.Collections.Generic;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	[Flags]
	enum VertexFormat {
		Position = 1, 
		Normal = 2, 
		Texcoord = 4, 
		LightmapTexcoord = 8, 
		All = Position | Normal | Texcoord | LightmapTexcoord
	}

	class MeshBuffer {
		public int Stride { get; }
		public VertexFormat Format { get; set; }
		public WebGLBuffer Buffer { get; }

		public MeshBuffer(VertexFormat format, double[] data) {
			Format = format;
			Buffer = gl.CreateBuffer();
			gl.BindBuffer(gl.ARRAY_BUFFER, Buffer);
			gl.BufferData(gl.ARRAY_BUFFER, new Float32Array(data), gl.STATIC_DRAW);
			Stride = 0;
			if((Format & VertexFormat.Position) == VertexFormat.Position)
				Stride += 3 * 4;
			if((Format & VertexFormat.Normal) == VertexFormat.Normal)
				Stride += 3 * 4;
			if((Format & VertexFormat.Texcoord) == VertexFormat.Texcoord)
				Stride += 2 * 4;
			if((Format & VertexFormat.LightmapTexcoord) == VertexFormat.LightmapTexcoord)
				Stride += 2 * 4;
		}

		public void SetAttributes(Program program, int variant) {
			gl.BindBuffer(gl.ARRAY_BUFFER, Buffer);
			var off = 0;
			var suffix = variant == 0 ? "" : variant.ToString();
			if((Format & VertexFormat.Position) == VertexFormat.Position) {
				var attr = program.GetAttribute("aVertexPosition" + suffix);
				gl.VertexAttribPointer(attr, 3, gl.FLOAT, false, Stride, off);
				gl.EnableVertexAttribArray(attr);
				off += 3 * 4;
			}
			if((Format & VertexFormat.Normal) == VertexFormat.Normal) {
				var attr = program.GetAttribute("aVertexNormal" + suffix);
				gl.VertexAttribPointer(attr, 3, gl.FLOAT, false, Stride, off);
				gl.EnableVertexAttribArray(attr);
				off += 3 * 4;
			}
			if((Format & VertexFormat.Texcoord) == VertexFormat.Texcoord) {
				var attr = program.GetAttribute("aVertexTexcoord");
				gl.VertexAttribPointer(attr, 2, gl.FLOAT, false, Stride, off);
				gl.EnableVertexAttribArray(attr);
				off += 2 * 4;
			}
			if((Format & VertexFormat.LightmapTexcoord) == VertexFormat.LightmapTexcoord) {
				var attr = program.GetAttribute("aVertexLmcoord");
				gl.VertexAttribPointer(attr, 2, gl.FLOAT, false, Stride, off);
				gl.EnableVertexAttribArray(attr);
				off += 2 * 4;
			}
		}
	}

	class Mesh {
		WebGLBuffer IndexBuffer;
		int NumIndices;
		public Material[] Materials;
		Texture Lightmap;
		protected List<Tuple<int, MeshBuffer>> Buffers = new List<Tuple<int, MeshBuffer>>();
		
		public Mesh(uint[] indices, Material[] materials, Texture lightmap) {
			IndexBuffer = gl.CreateBuffer();
			gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, IndexBuffer);
			gl.BufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(indices), gl.STATIC_DRAW);

			NumIndices = indices.Length;
			Materials = materials;
			Lightmap = lightmap;
		}

		public void Add(MeshBuffer buffer, int variant = 0) {
			Buffers.Add(new Tuple<int, MeshBuffer>(variant, buffer));
		}

		public void Draw(bool transparent) {
			var isTrans = true;
			foreach(var material in Materials)
				isTrans = isTrans && material.Transparent;
			if(isTrans != transparent)
				return;
			foreach(var material in Materials) {
				material.Use(program => Buffers.ForEach(x => x.Item2.SetAttributes(program, x.Item1)), Lightmap);
				gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, IndexBuffer);
				gl.DrawElements(gl.TRIANGLES, NumIndices, gl.UNSIGNED_SHORT, 0);
			}
		}
	}
}
