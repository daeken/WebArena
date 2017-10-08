using Bridge;
using Bridge.Html5;
using Bridge.WebGL;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using static System.Console;

namespace WebArena {
	public class App {
		AssetManager am;
		Draw draw;

		public static void Main() {
			new App();
		}

		App() {
			var canvas = new HTMLCanvasElement();
			canvas.Width = 800;
			canvas.Height = 600;
			Document.Body.AppendChild(canvas);

			var ctx = canvas.GetContext(CanvasTypes.CanvasContextWebGLType.WebGL).As<WebGLRenderingContext>();
			draw = new Draw(ctx);
			
			var _ = LoadAssets();

			OnFrame();
		}

		async Task LoadAssets() {
			try {
				am = new AssetManager();
				var tourney = new Bsp(await am.Get<BspData>("tourney.json"));
			} catch(Exception e) {
				WriteLine(e);
			}
		}

		void OnFrame() {
			draw.Render();
			Window.RequestAnimationFrame(OnFrame);
		}
	}
}
