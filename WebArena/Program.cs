using Bridge.WebGL;
using System;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	class Program {
		WebGLProgram GLProgram;

		public Program(string vs, string fs) {
			var vss = CompileShader(gl.VERTEX_SHADER, vs);
			var fss = CompileShader(gl.FRAGMENT_SHADER, fs);

			GLProgram = (WebGLProgram) gl.CreateProgram();
			gl.AttachShader(GLProgram, vss);
			gl.AttachShader(GLProgram, fss);
			gl.LinkProgram(GLProgram);

			if(!(bool) gl.GetProgramParameter(GLProgram, gl.LINK_STATUS)) {
				WriteLine($"Program link failed: {gl.GetProgramInfoLog(GLProgram)}");
				throw new Exception("Program link failed");
			}
		}

		WebGLShader CompileShader(int type, string code) {
			var shader = gl.CreateShader(type);
			gl.ShaderSource(shader, code);
			gl.CompileShader(shader);
			if((bool) gl.GetShaderParameter(shader, gl.COMPILE_STATUS))
				return shader;
			else {
				WriteLine($"Shader compilation failed: {gl.GetShaderInfoLog(shader)}");
				throw new Exception("Shader compilation failed");
			}
		}

		public void Use() {
			gl.UseProgram(GLProgram);
		}

		public int GetAttribute(string name) {
			return gl.GetAttribLocation(GLProgram, name);
		}
	}
}
