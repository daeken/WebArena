using System;
using System.Linq;
using Bridge.Html5;
using static WebArena.Globals;
using static System.Console;

namespace WebArena {
	public class Game : ProtocolHandler {
		readonly WebSocket Socket;
		double LastTime;
		readonly double[] RenderTimes;
		int RTI;

		public static void Main() { new Game(); }
		protected override void Send(byte[] data) => Socket.Send(new Uint8Array(data).Buffer);
		public Game() {
			GuiInstance = new Gui();
			InputInstance = new Input();
			
			Scene = new SceneGraph();
			DrawInstance = new Draw();
			PlayerCamera = new Camera(/*vec3(-400, 1000, 750)*/ vec3(0, -100, 75));
			//PlayerCamera.Yaw = Math.PI;
			
			LastTime = StartTime = CurTime;
			RenderTimes = new double[120];
			
			Socket = new WebSocket("ws://localhost:8080/socket") { BinaryType = WebSocket.DataType.ArrayBuffer };
			Socket.OnMessage += message => {
				var data = new Uint8Array((ArrayBuffer) message.Data).ToArray();
				var msg = ParseMessage(data);
				WriteLine($"Got message '{msg}'");
				Handle(msg);
			};
			
			OnFrame();
		}

		void OnFrame() {
			if(CurrentMap == null) {
				Window.RequestAnimationFrame(OnFrame);
				return;
			}

			Time = CurTime - StartTime;
			var rtime = Time - LastTime;
			RenderTimes[RTI] = rtime;
			RTI = (RTI + 1) % 120;
			Document.Title = $"WebArena | FPS: {Math.Round(1 / (RenderTimes.Sum() / 120))}";
			LastTime = Time;

			InputInstance.Update(rtime);

			PlayerCamera.Update();
			Scene.Update();
			DrawInstance.Render();

			Window.RequestAnimationFrame(OnFrame);
		}

		protected override async void Handle(MapChange map) {
			if(CurrentMap != null) {
				Scene.Clear();
				CurrentMap = null;
			}

			var nmap = CurrentMap = new Bsp(await AssetManager.Get<BspData>(map.Path));
			PlayerCamera.Position = nmap.SpawnPoints[0];
			Scene.Add(nmap);
		}
	}
}