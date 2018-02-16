using System;
using static WebArena.Globals;

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

		public void Move(Vec3 movement) {
			Position += LookRotation * movement;
			Movement.Position = Position;
			Movement.Move(vec3(0, 0, 0), 0);
		}

		public void Look(double pitchmod, double yawmod) {
			var eps = 0.0000001;
			Pitch = Clamp(Pitch + pitchmod, -Math.PI / 2 + eps, Math.PI / 2 - eps);
			Yaw += yawmod;
			LookRotation = Mat3.Yaw(Yaw) * Mat3.Roll(Pitch);
		}

		public void Update() {
			var at = (LookRotation * vec3(0, 1, 0)).Normalized;
			Matrix = Mat4.LookAt(Position, Position + at, vec3(0, 0, 1));
		}
	}
}
