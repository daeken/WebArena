using Bridge.Html5;
using Bridge.WebGL;
using System.Linq;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	class Material {
		int BlendSrc = -1, BlendDest = -1;
		Program Program;
		double TextureFreq;
		Texture[] Textures = null;
		public bool Transparent => BlendSrc != -1;

		public Material(string fs) {
			Program = new Program(@"
				precision highp float;
				attribute vec4 aVertexPosition;
				attribute vec3 aVertexNormal;
				attribute vec2 aVertexTexcoord;
				attribute vec2 aVertexLmcoord;

				uniform mat4 uModelViewMatrix;
				uniform mat4 uProjectionMatrix;

				varying vec3 vNormal;
				varying vec4 vPosition;
				varying vec2 vTexCoord;
				varying vec2 vLmCoord;

				void main() {
					vPosition = uModelViewMatrix * aVertexPosition.xzyw;
					gl_Position = uProjectionMatrix * vPosition;
					vNormal = aVertexNormal;
					vTexCoord = aVertexTexcoord;
					vLmCoord = aVertexLmcoord;
				}
			", "precision highp float;\n" + fs);
		}

		public void SetBlend(int src, int dest) {
			BlendSrc = src;
			BlendDest = dest;
		}

		public void AddTextures(double freq, string[] textures, bool clamp) {
			TextureFreq = freq;
			Textures = textures.Select(x => LoadTexture(x, clamp)).ToArray();
		}

		Texture LoadTexture(string texture, bool clamp) {
			var elems = texture.Split("/");
			var fn = elems[elems.Length - 1];
			if(fn.EndsWith(".tga"))
				fn = fn.Substr(0, fn.Length - 3) + "png";
			return new Texture($"textures/{fn}", clamp);
		}

		public void Use(WebGLBuffer VertexBuffer, Texture lightmap) {
			Program.Use();
			Program.SetUniform("uProjectionMatrix", ProjectionMatrix);
			Program.SetUniform("uModelViewMatrix", PlayerCamera.Matrix);
			Program.SetUniform("uTime", Time);
			if(Textures != null) {
				gl.ActiveTexture(gl.TEXTURE0);
				var ti = 0;
				if(TextureFreq != 0)
					ti = (int) (Time * TextureFreq) % Textures.Length;
				Textures[ti].Use();
				Program.SetUniform("uTexSampler", 0);
			}
			gl.ActiveTexture(gl.TEXTURE1);
			lightmap.Use();
			Program.SetUniform("uLmSampler", 1);

			if(!Transparent)
				gl.Disable(gl.BLEND);
			else {
				gl.Enable(gl.BLEND);
				gl.BlendFunc(BlendSrc, BlendDest);
			}

			gl.BindBuffer(gl.ARRAY_BUFFER, VertexBuffer);
			var attr = Program.GetAttribute("aVertexPosition");
			gl.VertexAttribPointer(attr, 3, gl.FLOAT, false, (3 + 3 + 2 + 2) * 4, 0);
			gl.EnableVertexAttribArray(attr);
			attr = Program.GetAttribute("aVertexNormal");
			gl.VertexAttribPointer(attr, 3, gl.FLOAT, false, (3 + 3 + 2 + 2) * 4, (3) * 4);
			gl.EnableVertexAttribArray(attr);
			attr = Program.GetAttribute("aVertexTexcoord");
			gl.VertexAttribPointer(attr, 2, gl.FLOAT, false, (3 + 3 + 2 + 2) * 4, (3 + 3) * 4);
			gl.EnableVertexAttribArray(attr);
			attr = Program.GetAttribute("aVertexLmcoord");
			gl.VertexAttribPointer(attr, 2, gl.FLOAT, false, (3 + 3 + 2 + 2) * 4, (3 + 3 + 2) * 4);
			gl.EnableVertexAttribArray(attr);
		}
	}
}
