using Bridge;
using Bridge.Html5;
using Bridge.WebGL;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	public class App {
		AssetManager am;
		Draw draw;

		public static void Main() {
			new App();
		}

		App() {
			var canvas = new Bridge.Html5.HTMLCanvasElement {
				Width = 800,
				Height = 600
			};
			Document.Body.AppendChild(canvas);

			gl = canvas.GetContext(CanvasTypes.CanvasContextWebGLType.WebGL).As<WebGLRenderingContext>();
			draw = new Draw();
			
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
