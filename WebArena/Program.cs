using Bridge.WebGL;
using System;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	class Program {
		WebGLProgram GLProgram;

		public Program(string vs, string fs) {
			GLProgram = (WebGLProgram) gl.CreateProgram();
			gl.AttachShader(GLProgram, CompileShader(gl.VERTEX_SHADER, vs));
			gl.AttachShader(GLProgram, CompileShader(gl.FRAGMENT_SHADER, fs));
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

		public void SetUniform(string name, Mat4 value) {
			gl.UniformMatrix4fv(gl.GetUniformLocation(GLProgram, name), false, value.AsArray);
		}
	}
}
