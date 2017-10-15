using Bridge.Html5;
using Bridge.WebGL;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	class Texture {
		WebGLTexture GTexture;

		static Texture _White = null;
		public static Texture White {
			get {
				if(_White == null)
					_White = new Texture(new byte[] { 255, 255, 255, 255 }, 1, 1, 4);
				return _White;
			}
		}

		public Texture(string fn, bool clamp) {
			GTexture = gl.CreateTexture();
			gl.BindTexture(gl.TEXTURE_2D, GTexture);
			var pixel = new Uint8Array(new byte[] { 255, 255, 0, 255 });
			gl.TexImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0, gl.RGBA, gl.UNSIGNED_BYTE, pixel);
			var image = new HTMLImageElement();
			image.OnLoad = (e) => {
				gl.BindTexture(gl.TEXTURE_2D, GTexture);
				gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, clamp ? gl.CLAMP_TO_EDGE : gl.REPEAT);
				gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, clamp ? gl.CLAMP_TO_EDGE : gl.REPEAT);
				gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST_MIPMAP_LINEAR);

				if(!IsPOT(image.Width) || !IsPOT(image.Height)) {
					var cvs = new HTMLCanvasElement();
					cvs.Width = NextHighestPOT(image.Width);
					cvs.Height = NextHighestPOT(image.Height);
					var ctx = cvs.GetContext(CanvasTypes.CanvasContext2DType.CanvasRenderingContext2D);
					ctx.DrawImage(image, 0, 0, cvs.Width, cvs.Height);
					gl.TexImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, cvs);
				} else
					gl.TexImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);
				gl.GenerateMipmap(gl.TEXTURE_2D);
			};
			image.Src = fn;
		}
		public Texture(byte[] pixels, int width, int height, int components) {
			GTexture = gl.CreateTexture();
			gl.BindTexture(gl.TEXTURE_2D, GTexture);
			gl.TexImage2D(gl.TEXTURE_2D, 0, components == 4 ? gl.RGBA : gl.RGB, width, height, 0, components == 4 ? gl.RGBA : gl.RGB, gl.UNSIGNED_BYTE, new Uint8Array(pixels));
			gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT);
			gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.REPEAT);
			gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST_MIPMAP_LINEAR);
			gl.GenerateMipmap(gl.TEXTURE_2D);
		}

		public void Use() {
			gl.BindTexture(gl.TEXTURE_2D, GTexture);
		}

		bool IsPOT(int x) {
			return (x & (x - 1)) == 0;
		}

		int NextHighestPOT(int x) {
			x--;
			for(var i = 1; i < 32; i <<= 1)
				x = x | x >> i;
			return x + 1;
		}
	}
}
