using Bridge.Html5;
using Bridge.WebGL;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	class Material {
		int BlendSrc = -1, BlendDest = -1;
		protected Program Program;
		double TextureFreq;
		Texture[] Textures = null;
		public bool Transparent => BlendSrc != -1;

		protected static string FauxDiffuseFS = @"
			precision highp float;
			varying vec3 vNormal;
			varying vec4 vPosition;
			varying vec2 vTexCoord;
			varying vec2 vLmCoord;
			float calcLight(vec3 lightvec) {
				return min(max(dot(vNormal, lightvec), 0.1), 1.5);
			}
				
			void main() {
				gl_FragColor = vec4(vec3(calcLight(vec3(.2, -1, 0)) + calcLight(vec3(.3, .75, .5)) + calcLight(vec3(-.3, .75, -.5))) * 0.6, 1.0);
			}
		";
		static Material _FauxDiffuse = null;
		public static Material FauxDiffuse {
			get {
				if(_FauxDiffuse == null)
					_FauxDiffuse = new Material(FauxDiffuseFS);
				return _FauxDiffuse;
			}
		}

		public Material(string fs) {
			if(fs != null)
				Program = new Program(@"
					precision highp float;
					attribute vec4 aVertexPosition;
					attribute vec3 aVertexNormal;
					attribute vec2 aVertexTexcoord;
					attribute vec2 aVertexLmcoord;

					uniform mat4 uModelMatrix;
					uniform mat4 uViewMatrix;
					uniform mat4 uProjectionMatrix;

					varying vec3 vNormal;
					varying vec4 vPosition;
					varying vec2 vTexCoord;
					varying vec2 vLmCoord;

					void main() {
						vPosition = uViewMatrix * uModelMatrix * aVertexPosition.xzyw;
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

		public void Use(Action<Program> setupAttributes, Texture lightmap) {
			Program.Use();
			Program.SetUniform("uProjectionMatrix", ProjectionMatrix);
			Program.SetUniform("uModelMatrix", ModelMatrix);
			Program.SetUniform("uViewMatrix", PlayerCamera.Matrix);
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

			SetupUniforms();
			DisableAttributes();

			setupAttributes(Program);
		}

		protected virtual void DisableAttributes() {
			gl.DisableVertexAttribArray(Program.GetAttribute("aVertexPosition"));
			gl.DisableVertexAttribArray(Program.GetAttribute("aVertexNormal"));
			gl.DisableVertexAttribArray(Program.GetAttribute("aVertexTexcoord"));
			gl.DisableVertexAttribArray(Program.GetAttribute("aVertexLmcoord"));
		}

		protected virtual void SetupUniforms() {
		}
	}

	class AniMaterial : Material {
		static AniMaterial _FauxDiffuse = null;
		int Fps;
		public new static AniMaterial FauxDiffuse {
			get {
				if(_FauxDiffuse == null)
					_FauxDiffuse = new AniMaterial(FauxDiffuseFS);
				return _FauxDiffuse;
			}
		}

		public AniMaterial(string fs) : base(null) {
			Program = new Program(@"
				precision highp float;
				attribute vec4 aVertexPosition, aVertexPosition2;
				attribute vec3 aVertexNormal, aVertexNormal2;
				attribute vec2 aVertexTexcoord;
				attribute vec2 aVertexLmcoord;

				uniform mat4 uModelMatrix;
				uniform mat4 uViewMatrix;
				uniform mat4 uProjectionMatrix;
				uniform float uFrameLerp;

				varying vec3 vNormal;
				varying vec4 vPosition;
				varying vec2 vTexCoord;
				varying vec2 vLmCoord;

				void main() {
					vPosition = uViewMatrix * uModelMatrix * mix(aVertexPosition, aVertexPosition2, uFrameLerp).xzyw;
					gl_Position = uProjectionMatrix * vPosition;
					vNormal = normalize(mix(aVertexNormal, aVertexNormal2, uFrameLerp));
					vTexCoord = aVertexTexcoord;
					vLmCoord = aVertexLmcoord;
				}
			", "precision highp float;\n" + fs);
		}

		protected override void DisableAttributes() {
			gl.DisableVertexAttribArray(Program.GetAttribute("aVertexPosition"));
			gl.DisableVertexAttribArray(Program.GetAttribute("aVertexNormal"));
			gl.DisableVertexAttribArray(Program.GetAttribute("aVertexPosition2"));
			gl.DisableVertexAttribArray(Program.GetAttribute("aVertexNormal2"));
			gl.DisableVertexAttribArray(Program.GetAttribute("aVertexTexcoord"));
			gl.DisableVertexAttribArray(Program.GetAttribute("aVertexLmcoord"));
		}

		protected override void SetupUniforms() {
			var sub = Time * Fps;
			Program.SetUniform("uFrameLerp", sub - Math.Floor(sub));
		}

		public void SetFPS(int fps) {
			Fps = fps;
		}
	}
}
