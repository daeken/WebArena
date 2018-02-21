using System.Collections.Generic;
using Bridge.Html5;
using static WebArena.Globals;

namespace WebArena {
	public struct MouseState {
		public Vec2? Clicked;
		public Vec2? RightClicked;
		public Vec2  Movement;
	}
	
	public class Input {
		readonly Dictionary<int, double> KeyState = new Dictionary<int, double>();
		MouseState MouseState;
		bool IsMouseCaptured => ((dynamic) Window.Document).pointerLockElement == Gui.Canvas;

		public Input() {
			Document.Body.OnKeyDown += e => {
				if(!KeyState.ContainsKey(e.KeyCode))
					KeyState[e.KeyCode] = CurTime - StartTime;
			};
			Document.Body.OnKeyUp += e => KeyState.Remove(e.KeyCode);

			Window.OnBlur += e => KeyState.Clear();
			
			Gui.Canvas.OnClick += e => {
				if(!IsMouseCaptured)
					((dynamic) Gui.Canvas).requestPointerLock();
				else
					MouseState.Clicked = vec2(e.ClientX, e.ClientY);
			};
			Window.Document.OnMouseMove += e => {
				if(IsMouseCaptured)
					MouseState.Movement += vec2(e.MovementX, e.MovementY);
			};
		}

		public void Update(double rtime) {
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

		}
	}
}