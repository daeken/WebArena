using System;
using static WebArena.Globals;
using static System.Console;

namespace WebArena {
	class Camera {
		public Vec3 Position;
		public double Pitch, Yaw;

		public Mat4 Matrix;
		Mat3 LookRotation = Mat3.Identity;

		Movement Movement = new Movement();

		public Camera(Vec3 pos) {
			Pitch = Yaw = 0;
			Position = pos;
		}

		public void Move(Vec3 movement, double frameTime) {
			Movement.Position = Position;
			Movement.Move(Mat3.Yaw(Yaw) * movement, frameTime * 1000);
			//WriteLine(Movement.Position);
			Position = Movement.Position;
		}

		public void Look(double pitchmod, double yawmod) {
			var eps = 0.0000001;
			Pitch = Clamp(Pitch + pitchmod, -Math.PI / 2 + eps, Math.PI / 2 - eps);
			Yaw += yawmod;
		}

		public void Update() {
			var at = (Mat3.Yaw(Yaw) * Mat3.Roll(Pitch) * vec3(0, 1, 0)).Normalized;
			Matrix = Mat4.LookAt(Position, Position + at, vec3(0, 0, 1));
		}
	}
}
