using System;
using static WebArena.Globals;

namespace WebArena {
	class Camera {
		public Vec3 Position;
		public double Pitch, Yaw;

		public Mat4 Matrix;

		public Camera(Vec3 pos) {
			Pitch = Yaw = 0;
			Position = pos;
		}

		public void Move(Vec3 movement) {
			Position += Mat3.Pitch(Yaw) * Mat3.Roll(Pitch) * movement;
		}

		public void Look(double pitchmod, double yawmod) {
			Pitch = Math.Max(Math.Min(Pitch + pitchmod, Math.PI / 2), -Math.PI / 2);
			Yaw += yawmod;
		}

		public void Update() {
			var cp = Math.Cos(Pitch);
			var sp = Math.Sin(Pitch);
			var cy = Math.Cos(Yaw);
			var sy = Math.Sin(Yaw);

			var xa = vec3(cy, 0, -sy);
			var ya = vec3(sy * sp, cp, cy * sp);
			var za = vec3(sy * cp, -sp, cp * cy);

			Matrix = new Mat4(
				xa.X, ya.X, za.X, 0, 
				xa.Y, ya.Y, za.Y, 0, 
				xa.Z, ya.Z, za.Z, 0, 
				-xa.Dot(Position), -ya.Dot(Position), -za.Dot(Position), 1
			);
		}
	}
}
