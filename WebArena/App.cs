using Bridge;
using Bridge.Html5;
using Bridge.WebGL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Console;
using static WebArena.Globals;
using static WebArena.Extensions;

namespace WebArena {
	public class App {
		AssetManager AM;
		Draw Draw;
		double StartTime, LastTime;
		double[] RenderTimes;
		int RTI = 0;
		Dictionary<int, double> Movement = new Dictionary<int, double>();
		HTMLCanvasElement Canvas;

		public static void Main() {
			new App();
		}

		App() {
			var style = new HTMLStyleElement {
				TextContent=@"
					* { margin: 0; padding: 0; }
					html, body { width: 100%; height: 100%; }
					canvas { display: block; }
				"
			};
			Document.Body.AppendChild(style);
			Canvas = new HTMLCanvasElement();
			Document.Body.AppendChild(Canvas);

			Document.Body.OnKeyDown = (e) => {
				if(!Movement.ContainsKey(e.KeyCode))
					Movement[e.KeyCode] = CurTime - StartTime;
			};
			Document.Body.OnKeyUp = (e) => {
				Movement.Remove(e.KeyCode);
			};

			gl = Canvas.GetContext(CanvasTypes.CanvasContextWebGLType.WebGL).As<WebGLRenderingContext>();
			Resize();
			Window.OnResize += (e) => Resize();

			Scene = new SceneGraph();
			Draw = new Draw();
			PlayerCamera = new Camera(vec3(0, 50, -100));
			
			var _ = LoadAssets();

			LastTime = StartTime = CurTime;
			RenderTimes = new double[120];
		}

		void Resize() {
			Canvas.Style.Width = (Canvas.Width = Window.InnerWidth).ToString();
			Canvas.Style.Height = (Canvas.Height = Window.InnerHeight).ToString();
			ProjectionMatrix = Mat4.Perspective(45 * (Math.PI / 180), ((double) Window.InnerWidth) / Window.InnerHeight, 0.1, 10000);
			gl.Viewport(0, 0, Canvas.Width, Canvas.Height);
		}

		async Task LoadAssets() {
			try {
				AM = new AssetManager();
				var tourney = new Bsp(await AM.Get<BspData>("tourney.json"));
				Scene.Add(tourney);
				OnFrame();
			} catch(Exception e) {
				WriteLine(e);
			}
		}

		void OnFrame() {
			Time = CurTime - StartTime;
			var rtime = Time - LastTime;
			RenderTimes[RTI] = rtime;
			RTI = (RTI + 1) % 120;
			Document.Title = $"WebArena | FPS: {1 / (RenderTimes.Sum() / 120)}";
			LastTime = Time;

			foreach(var p in Movement) {
				var elapsed = Time - p.Value;
				if(elapsed < 0)
					break;
				Movement[p.Key] = Time;
				var movemod = 250;
				switch(p.Key) {
					case 87: // W
						PlayerCamera.Move(vec3(0, 0, elapsed * -movemod));
						break;
					case 83: // S
						PlayerCamera.Move(vec3(0, 0, elapsed * movemod));
						break;
					case 65: // A
						PlayerCamera.Move(vec3(elapsed * -movemod, 0, 0));
						break;
					case 68: // D
						PlayerCamera.Move(vec3(elapsed * movemod, 0, 0));
						break;
					case 32: // Space
						PlayerCamera.Move(vec3(0, elapsed * movemod, 0));
						break;
					case 16: // Shift
						PlayerCamera.Move(vec3(0, elapsed * -movemod, 0));
						break;
					case 38: // Up
						PlayerCamera.Look(elapsed, 0);
						break;
					case 40: // Down
						PlayerCamera.Look(-elapsed, 0);
						break;
					case 37: // Left
						PlayerCamera.Look(0, elapsed * 2);
						break;
					case 39: // Right
						PlayerCamera.Look(0, -elapsed * 2);
						break;
					default:
						//WriteLine($"Unknown key pressed: {p.Key}");
						break;
				}
			}

			PlayerCamera.Update();
			Draw.Render();

			Window.RequestAnimationFrame(OnFrame);
		}
	}
}
