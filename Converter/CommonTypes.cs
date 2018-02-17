using System;
using System.Runtime.InteropServices;

namespace Converter {
	[StructLayout(LayoutKind.Sequential)]
	public struct Vec2 {
		public float X, Y;

		public float[] ToArray => new[] {X, Y};

		public override string ToString() {
			return $"Vec2[ {X} {Y} ]";
		}
		
		public static implicit operator Vec2(Vec4 v) => new Vec2 { X = (float) v.X, Y = (float) v.Y };
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Vec3 {
		public float X, Y, Z;

		public float Length => (float) Math.Sqrt(X * X + Y * Y + Z * Z);
		public Vec3 Normalized {
			get {
				var len = Length;
				if(len == 0)
					return new Vec3();
				return new Vec3 { X = X / len, Y = Y / len, Z = Z / len };
			}
		}
		public float[] ToArray => new[] {X, Y, Z};

		public override string ToString() {
			return $"Vec3[ {X} {Y} {Z} ]";
		}

		public static implicit operator Vec3(Vec4 v) => new Vec3 { X = (float) v.X, Y = (float) v.Y, Z = (float) v.Z };
	}
	
	public struct Vec4 {
		public double X, Y, Z, W;
		public double Length => Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
		public Vec4 Normalized {
			get {
				var len = Length;
				if(len == 0)
					return new Vec4();
				return new Vec4(X / len, Y / len, Z / len, W / len);
			}
		}
		
		public double[] ToArray() => new[] { X, Y, Z, W };
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

		public Vec4(double[] v) {
			X = v[0];
			Y = v[1];
			Z = v[2];
			W = v[3];
		}

		public Vec4(int[] v) {
			X = v[0];
			Y = v[1];
			Z = v[2];
			W = v[3];
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

		public static Vec4 operator -(Vec4 v) {
			return new Vec4(-v.X, -v.Y, -v.Z, -v.W);
		}

		public double Dot(Vec4 right) {
			var temp = this * right;
			return temp.X + temp.Y + temp.Z + temp.W;
		}

		public override string ToString() {
			return $"Vec4[ {X} {Y} {Z} {W} ]";
		}
		
		public static implicit operator Vec4(Vec2 v) => new Vec4(v.X, v.Y, 0, 0);
		public static implicit operator Vec4(Vec3 v) => new Vec4(v.X, v.Y, v.Z, 0);
	}
}