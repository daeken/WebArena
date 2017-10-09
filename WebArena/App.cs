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
		AssetManager AM;
		Draw Draw;
		float StartTime;

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
			Scene = new SceneGraph();
			Draw = new Draw();
			
			var _ = LoadAssets();

			StartTime = (float) (new Date().GetTime() / 1000);
			OnFrame();
		}

		async Task LoadAssets() {
			try {
				AM = new AssetManager();
				var tourney = new Bsp(await AM.Get<BspData>("tourney.json"));
				Scene.Add(tourney);
			} catch(Exception e) {
				WriteLine(e);
			}
		}

		void OnFrame() {
			Time = (float) (new Date().GetTime() / 1000) - StartTime;
			Draw.Render();
			Window.RequestAnimationFrame(OnFrame);
		}
	}
}
