using System;

namespace WebArena {
	public struct Vec2 {
		public double X, Y;
		public double Length => (double) Math.Sqrt(X * X + Y * Y);
		public Vec2 Normalized {
			get {
				var len = Length;
				if(len == 0)
					return new Vec2();
				return new Vec2(X / len, Y / len);
			}
		}
		public Vec2 Abs => new Vec2(Math.Abs(X), Math.Abs(Y));
		public Vec2 Exp => new Vec2(Math.Exp(X), Math.Exp(Y));
		public Vec2 Log => new Vec2(Math.Log(X), Math.Log(Y));
		public Vec2 Log2 => new Vec2(Math.Log(X, 2), Math.Log(Y, 2));
		public Vec2 Sqrt => new Vec2(Math.Sqrt(X), Math.Sqrt(Y));
		public Vec2 InverseSqrt => new Vec2(1 / Math.Sqrt(X), 1 / Math.Sqrt(Y));

		public Vec2(double v) {
			X = Y = v;
		}

		public Vec2(double x, double y) {
			X = x;
			Y = y;
		}

		public static Vec2 operator +(Vec2 left, double right) {
			return new Vec2(left.X + right, left.Y + right);
		}
		public static Vec2 operator +(Vec2 left, Vec2 right) {
			return new Vec2(left.X + right.X, left.Y + right.Y);
		}

		public static Vec2 operator -(Vec2 left, double right) {
			return new Vec2(left.X - right, left.Y - right);
		}
		public static Vec2 operator -(Vec2 left, Vec2 right) {
			return new Vec2(left.X - right.X, left.Y - right.Y);
		}

		public static Vec2 operator *(Vec2 left, double right) {
			return new Vec2(left.X * right, left.Y * right);
		}
		public static Vec2 operator *(Vec2 left, Vec2 right) {
			return new Vec2(left.X * right.X, left.Y * right.Y);
		}

		public static Vec2 operator /(Vec2 left, double right) {
			return new Vec2(left.X / right, left.Y / right);
		}
		public static Vec2 operator /(Vec2 left, Vec2 right) {
			return new Vec2(left.X / right.X, left.Y / right.Y);
		}

		public static double operator %(Vec2 left, Vec2 right) {
			return left.Dot(right);
		}

		public double Dot(Vec2 right) {
			var temp = this * right;
			return temp.X + temp.Y;
		}

		public override string ToString() {
			return $"Vec2[ {X} {Y} ]";
		}
	}

	public struct Vec3 {
		public double X, Y, Z;
		public double Length => (double) Math.Sqrt(X * X + Y * Y + Z * Z);
		public Vec3 Normalized {
			get {
				var len = Length;
				if(len == 0)
					return new Vec3();
				return new Vec3(X / len, Y / len, Z / len);
			}
		}
		public Vec3 Abs => new Vec3(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
		public Vec3 Exp => new Vec3(Math.Exp(X), Math.Exp(Y), Math.Exp(Z));
		public Vec3 Log => new Vec3(Math.Log(X), Math.Log(Y), Math.Log(Z));
		public Vec3 Log2 => new Vec3(Math.Log(X, 2), Math.Log(Y, 2), Math.Log(Z, 2));
		public Vec3 Sqrt => new Vec3(Math.Sqrt(X), Math.Sqrt(Y), Math.Sqrt(Z));
		public Vec3 InverseSqrt => new Vec3(1 / Math.Sqrt(X), 1 / Math.Sqrt(Y), 1 / Math.Sqrt(Z));

		public Vec3(double v) {
			X = Y = Z = v;
		}

		public Vec3(double x, double y, double z) {
			X = x;
			Y = y;
			Z = z;
		}

		public static Vec3 operator +(Vec3 left, double right) {
			return new Vec3(left.X + right, left.Y + right, left.Z + right);
		}
		public static Vec3 operator +(Vec3 left, Vec3 right) {
			return new Vec3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static Vec3 operator -(Vec3 left, double right) {
			return new Vec3(left.X - right, left.Y - right, left.Z - right);
		}
		public static Vec3 operator -(Vec3 left, Vec3 right) {
			return new Vec3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static Vec3 operator *(Vec3 left, double right) {
			return new Vec3(left.X * right, left.Y * right, left.Z * right);
		}
		public static Vec3 operator *(Vec3 left, Vec3 right) {
			return new Vec3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
		}

		public static Vec3 operator /(Vec3 left, double right) {
			return new Vec3(left.X / right, left.Y / right, left.Z / right);
		}
		public static Vec3 operator /(Vec3 left, Vec3 right) {
			return new Vec3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
		}

		public static double operator %(Vec3 left, Vec3 right) {
			return left.Dot(right);
		}

		public static Vec3 operator ^(Vec3 left, Vec3 right) {
			return left.Cross(right);
		}

		public double Dot(Vec3 right) {
			var temp = this * right;
			return temp.X + temp.Y + temp.Z;
		}

		public Vec3 Cross(Vec3 right) {
			return new Vec3(Y * right.Z - Z * right.Y, Z * right.X - X * right.Z, X * right.Y - Y * right.X);
		}

		public override string ToString() {
			return $"Vec3[ {X} {Y} {Z} ]";
		}
	}

	public struct Vec4 {
		public double X, Y, Z, W;
		public double Length => (double) Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
		public Vec4 Normalized {
			get {
				var len = Length;
				if(len == 0)
					return new Vec4();
				return new Vec4(X / len, Y / len, Z / len, W / len);
			}
		}
		public Vec4 Abs => new Vec4(Math.Abs(X), Math.Abs(Y), Math.Abs(Z), Math.Abs(W));
		public Vec4 Exp => new Vec4(Math.Exp(X), Math.Exp(Y), Math.Exp(Z), Math.Exp(W));
		public Vec4 Log => new Vec4(Math.Log(X), Math.Log(Y), Math.Log(Z), Math.Log(W));
		public Vec4 Log2 => new Vec4(Math.Log(X, 2), Math.Log(Y, 2), Math.Log(Z, 2), Math.Log(W, 2));
		public Vec4 Sqrt => new Vec4(Math.Sqrt(X), Math.Sqrt(Y), Math.Sqrt(Z), Math.Sqrt(W));
		public Vec4 InverseSqrt => new Vec4(1 / Math.Sqrt(X), 1 / Math.Sqrt(Y), 1 / Math.Sqrt(Z), 1 / Math.Sqrt(W));

		public Vec4(double v) {
			X = Y = Z = W = v;
		}

		public Vec4(double x, double y, double z, double w) {
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		public static Vec4 operator +(Vec4 left, double right) {
			return new Vec4(left.X + right, left.Y + right, left.Z + right, left.W + right);
		}
		public static Vec4 operator +(Vec4 left, Vec4 right) {
			return new Vec4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
		}

		public static Vec4 operator -(Vec4 left, double right) {
			return new Vec4(left.X - right, left.Y - right, left.Z - right, left.W - right);
		}
		public static Vec4 operator -(Vec4 left, Vec4 right) {
			return new Vec4(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
		}

		public static Vec4 operator *(Vec4 left, double right) {
			return new Vec4(left.X * right, left.Y * right, left.Z * right, left.W * right);
		}
		public static Vec4 operator *(Vec4 left, Vec4 right) {
			return new Vec4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
		}

		public static Vec4 operator /(Vec4 left, double right) {
			return new Vec4(left.X / right, left.Y / right, left.Z / right, left.W / right);
		}
		public static Vec4 operator /(Vec4 left, Vec4 right) {
			return new Vec4(left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);
		}

		public static double operator %(Vec4 left, Vec4 right) {
			return left.Dot(right);
		}

		public double Dot(Vec4 right) {
			var temp = this * right;
			return temp.X + temp.Y + temp.Z + temp.W;
		}

		public override string ToString() {
			return $"Vec4[ {X} {Y} {Z} {W} ]";
		}
	}
}
