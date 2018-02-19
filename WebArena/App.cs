using Bridge.Html5;
using Bridge.WebGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static System.Console;
using static WebArena.Globals;
using static WebArena.Extensions;

namespace WebArena {
	public struct MouseState {
		public Vec2? Clicked;
		public Vec2? RightClicked;
		public Vec2  Movement;
	}
	
	public class App {
		readonly Draw Draw;
		readonly double StartTime;
		double LastTime;
		readonly double[] RenderTimes;
		int RTI;
		readonly Dictionary<int, double> KeyState = new Dictionary<int, double>();
		MouseState MouseState;
		readonly HTMLCanvasElement Canvas;
		bool IsMouseCaptured => ((dynamic) Window.Document).pointerLockElement == Canvas;
		
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

			Document.Body.OnKeyDown += e => {
				if(!KeyState.ContainsKey(e.KeyCode))
					KeyState[e.KeyCode] = CurTime - StartTime;
			};
			Document.Body.OnKeyUp += e => KeyState.Remove(e.KeyCode);

			Window.OnBlur += e => KeyState.Clear();

			gl = Canvas.GetContext(CanvasTypes.CanvasContextWebGLType.WebGL).As<WebGLRenderingContext>();
			Resize();
			Window.OnResize += e => Resize();
			Canvas.OnClick += e => {
				if(!IsMouseCaptured)
					((dynamic) Canvas).requestPointerLock();
				else
					MouseState.Clicked = vec2(e.ClientX, e.ClientY);
			};
			Window.Document.OnMouseMove += e => {
				if(IsMouseCaptured)
					MouseState.Movement += vec2(e.MovementX, e.MovementY);
			};

			Scene = new SceneGraph();
			Draw = new Draw();
			PlayerCamera = new Camera(/*vec3(-400, 1000, 750)*/ vec3(0, -100, 75));
			//PlayerCamera.Yaw = Math.PI;
			
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
				var map = CurrentMap = new Bsp(await AssetManager.Get<BspData>("maps/q3tourney2.json"));
				PlayerCamera.Position = map.SpawnPoints[0];
				Scene.Add(map);
				var sarge = new PlayerModel(await AssetManager.Get<Md3Data>("models/players/sarge/head.json"), await AssetManager.Get<Md3Data>("models/players/sarge/upper.json"),
					await AssetManager.Get<Md3Data>("models/players/sarge/lower.json")) {Position = vec3(100.0, 100, 24.0)};
				Scene.Add(sarge);
				var rocketlauncher = new Md3(await AssetManager.Get<Md3Data>("models/weapons2/rocketl/rocketl.json"));
				var lightning = new Md3(await AssetManager.Get<Md3Data>("models/weapons2/lightning/lightning.json"));
				sarge.Weapon = lightning;
				var rnode = new SpinningItem(rocketlauncher) { Position = vec3(-100, 100, 24) };
				Scene.Add(rnode);
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
			Document.Title = $"WebArena | FPS: {Math.Round(1 / (RenderTimes.Sum() / 120))}";
			LastTime = Time;

			if(IsMouseCaptured) {
				var delta = MouseState.Movement;
				if(delta.Length != 0) {
					PlayerCamera.Look(delta.Y * rtime, -delta.X * rtime * 1.25);
					MouseState.Movement = vec2();
				}
			}

			var movement = vec3();
			foreach(var p in KeyState) {
				var elapsed = Time - p.Value;
				if(elapsed < 0)
					break;
				KeyState[p.Key] = Time;
				const int movemod = 250;
				switch(p.Key) {
					case 87: // W
						movement += vec3(0, elapsed * -movemod, 0);
						break;
					case 83: // S
						movement += vec3(0, elapsed * movemod, 0);
						break;
					case 65: // A
						movement += vec3(elapsed * movemod, 0, 0);
						break;
					case 68: // D
						movement += vec3(elapsed * -movemod, 0, 0);
						break;
					/*case 32: // Space
						PlayerCamera.Move(vec3(0, 0, elapsed * movemod), rtime);
						break;
					case 16: // Shift
						PlayerCamera.Move(vec3(0, 0, elapsed * -movemod), rtime);
						break;*/
					case 38: // Up
						PlayerCamera.Look(-elapsed, 0);
						break;
					case 40: // Down
						PlayerCamera.Look(elapsed, 0);
						break;
					case 37: // Left
						PlayerCamera.Look(0, elapsed * 2);
						break;
					case 39: // Right
						PlayerCamera.Look(0, -elapsed * 2);
						break;
				}
			}
			
			PlayerCamera.Move(movement, rtime);

			PlayerCamera.Update();
			Scene.Update();
			Draw.Render();

			Window.RequestAnimationFrame(OnFrame);
		}
	}
}
