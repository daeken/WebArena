using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebArena {
	public struct Mat2 {
		public float _00, _01;
		public float _10, _11;

		public float[] AsArray => new float[] {
			_00, _01,
			_10, _11
		};

		public static Mat2 Identity = new Mat2(
			1, 0, 
			0, 1
		);

		public static Mat2 Rotation(float angle) {
			var c = (float) Math.Cos(angle);
			var s = (float) Math.Sin(angle);
			return new Mat2(
				c, s, 
				-s,  c
			);
		}

		public Mat2(
				float i_00, float i_01,
				float i_10, float i_11
		) {
			_00 = i_00; _01 = i_01;
			_10 = i_10; _11 = i_11;
		}

		public Mat2 Map(Func<float, float> f) {
			return new Mat2(
				f(_00), f(_01),
				f(_10), f(_11)
			);
		}

		public Mat2 Map(Func<int, float, float> f) {
			return new Mat2(
				f(0, _00), f(1, _01),
				f(2, _10), f(3, _11)
			);
		}

		public static Mat2 operator +(Mat2 left, float right) {
			return left.Map(x => x + right);
		}
		public static Mat2 operator +(Mat2 left, Mat2 right) {
			var ra = right.AsArray;
			return left.Map((i, x) => x + ra[i]);
		}

		public static Mat2 operator -(Mat2 left, float right) {
			return left.Map(x => x - right);
		}
		public static Mat2 operator -(Mat2 left, Mat3 right) {
			var ra = right.AsArray;
			return left.Map((i, x) => x - ra[i]);
		}

		public static Mat2 operator *(Mat2 left, float right) {
			return left.Map(x => x * right);
		}
		public static Vec2 operator *(Mat2 left, Vec2 right) {
			var la = left.AsArray;
			return new Vec2(
				la[0] * right.X + la[1] * right.Y,
				la[2] * right.X + la[3] * right.Y
			);
		}
		public static Vec2 operator *(Vec2 left, Mat2 right) {
			return right * left;
		}
		public static Mat2 operator *(Mat2 left, Mat2 right) {
			var la = left.AsArray;
			var ra = right.AsArray;
			return new Mat2(
				la[0] * ra[0] + la[1] * ra[2], la[0] * ra[1] + la[1] * ra[3],
				la[2] * ra[0] + la[3] * ra[2], la[2] * ra[1] + la[3] * ra[3]
			);
		}

		public override string ToString() {
			return $"Mat2[ {_00} {_01}\n      {_10} {_11} ]";
		}
	}

	public struct Mat3 {
		public float _00, _01, _02;
		public float _10, _11, _12;
		public float _20, _21, _22;

		public float[] AsArray => new float[] {
			_00, _01, _02,
			_10, _11, _12,
			_20, _21, _22
		};

		public static Mat3 Identity = new Mat3(
			1, 0, 0, 
			0, 1, 0, 
			0, 0, 1
		);

		public Mat3(
				float i_00, float i_01, float i_02, 
				float i_10, float i_11, float i_12, 
				float i_20, float i_21, float i_22
		) {
			_00 = i_00; _01 = i_01; _02 = i_02;
			_10 = i_10; _11 = i_11; _12 = i_12;
			_20 = i_20; _21 = i_21; _22 = i_22;
		}

		public Mat3 Map(Func<float, float> f) {
			return new Mat3(
				f(_00), f(_01), f(_02),
				f(_10), f(_11), f(_12),
				f(_20), f(_21), f(_22)
			);
		}

		public Mat3 Map(Func<int, float, float> f) {
			return new Mat3(
				f(0, _00), f(1, _01), f(2, _02),
				f(3, _10), f(4, _11), f(5, _12),
				f(6, _20), f(7, _21), f(8, _22)
			);
		}

		public static Mat3 operator +(Mat3 left, float right) {
			return left.Map(x => x + right);
		}
		public static Mat3 operator +(Mat3 left, Mat3 right) {
			var ra = right.AsArray;
			return left.Map((i, x) => x + ra[i]);
		}

		public static Mat3 operator -(Mat3 left, float right) {
			return left.Map(x => x - right);
		}
		public static Mat3 operator -(Mat3 left, Mat3 right) {
			var ra = right.AsArray;
			return left.Map((i, x) => x - ra[i]);
		}

		public static Mat3 operator *(Mat3 left, float right) {
			return left.Map(x => x * right);
		}
		public static Vec3 operator *(Mat3 left, Vec3 right) {
			var la = left.AsArray;
			return new Vec3(
				la[0] * right.X + la[1] * right.Y + la[2] * right.Z,
				la[3] * right.X + la[4] * right.Y + la[5] * right.Z,
				la[6] * right.X + la[7] * right.Y + la[8] * right.Z
			);
		}
		public static Vec3 operator *(Vec3 left, Mat3 right) {
			return right * left;
		}
		public static Mat3 operator *(Mat3 left, Mat3 right) {
			var la = left.AsArray;
			var ra = right.AsArray;
			return new Mat3(
				la[0] * ra[0] + la[1] * ra[3] + la[2] * ra[6], la[0] * ra[1] + la[1] * ra[4] + la[2] * ra[7], la[0] * ra[2] + la[1] * ra[5] + la[2] * ra[8],
				la[3] * ra[0] + la[4] * ra[3] + la[5] * ra[6], la[3] * ra[1] + la[4] * ra[4] + la[5] * ra[7], la[3] * ra[2] + la[4] * ra[5] + la[5] * ra[8],
				la[6] * ra[0] + la[7] * ra[3] + la[8] * ra[6], la[6] * ra[1] + la[7] * ra[4] + la[8] * ra[7], la[6] * ra[2] + la[7] * ra[5] + la[8] * ra[8]
			);
		}

		public override string ToString() {
			return $"Mat3[ {_00} {_01} {_02}\n      {_10} {_11} {_12}\n      {_20} {_21} {_22} ]";
		}
	}

	public struct Mat4 {
		public float _00, _01, _02, _03;
		public float _10, _11, _12, _13;
		public float _20, _21, _22, _23;
		public float _30, _31, _32, _33;

		public float[] AsArray => new float[] {
			_00, _01, _02, _03, 
			_10, _11, _12, _13,
			_20, _21, _22, _23,
			_30, _31, _32, _33
		};

		public static Mat4 Identity = new Mat4(
			1, 0, 0, 0, 
			0, 1, 0, 0, 
			0, 0, 1, 0, 
			0, 0, 0, 1
		);

		public static Mat4 Translation(Vec3 trans) {
			return new Mat4(
				1, 0, 0, 0, 
				0, 1, 0, 0, 
				0, 0, 1, 0, 
				trans.X, trans.Y, trans.Z, 1
			);
		}

		public static Mat4 Perspective(float fovy, float aspect, float near, float far) {
			var f = (float) (1 / Math.Tan(fovy / 2));
			var nf = 1 / (near - far);
			return new Mat4(
				f / aspect, 0, 0, 0, 
				0, f, 0, 0, 
				0, 0, (far + near) * nf, -1, 
				0, 0, (2 * far * near) * nf, 0
			);
		}

		public Mat4(
				float i_00, float i_01, float i_02, float i_03, 
				float i_10, float i_11, float i_12, float i_13, 
				float i_20, float i_21, float i_22, float i_23, 
				float i_30, float i_31, float i_32, float i_33
		) {
			_00 = i_00; _01 = i_01; _02 = i_02; _03 = i_03;
			_10 = i_10; _11 = i_11; _12 = i_12; _13 = i_13;
			_20 = i_20; _21 = i_21; _22 = i_22; _23 = i_23;
			_30 = i_30; _31 = i_31; _32 = i_32; _33 = i_33;
		}

		public Mat4 Map(Func<float, float> f) {
			return new Mat4(
				f(_00), f(_01), f(_02), f(_03), 
				f(_10), f(_11), f(_12), f(_13), 
				f(_20), f(_21), f(_22), f(_23),
				f(_30), f(_31), f(_32), f(_33)
			);
		}

		public Mat4 Map(Func<int, float, float> f) {
			return new Mat4(
				f(0, _00), f(1, _01), f(2, _02), f(3, _03),
				f(4, _10), f(5, _11), f(6, _12), f(7, _13),
				f(8, _20), f(9, _21), f(10, _22), f(11, _23),
				f(12, _30), f(13, _31), f(14, _32), f(15, _33)
			);
		}

		public static Mat4 operator +(Mat4 left, float right) {
			return left.Map(x => x + right);
		}
		public static Mat4 operator +(Mat4 left, Vec3 right) {
			return left.Translate(right);
		}
		public static Mat4 operator +(Mat4 left, Mat4 right) {
			var ra = right.AsArray;
			return left.Map((i, x) => x + ra[i]);
		}

		public static Mat4 operator -(Mat4 left, float right) {
			return left.Map(x => x - right);
		}
		public static Mat4 operator -(Mat4 left, Mat4 right) {
			var ra = right.AsArray;
			return left.Map((i, x) => x - ra[i]);
		}

		public static Mat4 operator *(Mat4 left, float right) {
			return left.Map(x => x * right);
		}
		public static Vec4 operator *(Mat4 left, Vec4 right) {
			var la = left.AsArray;
			return new Vec4(
				la[0] * right.X + la[1] * right.Y + la[2] * right.Z + la[3] * right.W,
				la[4] * right.X + la[5] * right.Y + la[6] * right.Z + la[7] * right.W,
				la[8] * right.X + la[9] * right.Y + la[10] * right.Z + la[11] * right.W,
				la[12] * right.X + la[13] * right.Y + la[14] * right.Z + la[15] * right.W
			);
		}
		public static Vec4 operator *(Vec4 left, Mat4 right) {
			return right * left;
		}
		public static Mat4 operator *(Mat4 left, Mat4 right) {
			var la = left.AsArray;
			var ra = right.AsArray;
			return new Mat4(
				la[0] * ra[0] + la[1] * ra[4] + la[2] * ra[8] + la[3] * ra[12], la[0] * ra[1] + la[1] * ra[5] + la[2] * ra[9] + la[3] * ra[13], la[0] * ra[2] + la[1] * ra[6] + la[2] * ra[10] + la[3] * ra[14], la[0] * ra[3] + la[1] * ra[7] + la[2] * ra[11] + la[3] * ra[15],
				la[4] * ra[0] + la[5] * ra[4] + la[6] * ra[8] + la[7] * ra[12], la[4] * ra[1] + la[5] * ra[5] + la[6] * ra[9] + la[7] * ra[13], la[4] * ra[2] + la[5] * ra[6] + la[6] * ra[10] + la[7] * ra[14], la[4] * ra[3] + la[5] * ra[7] + la[6] * ra[11] + la[7] * ra[15],
				la[8] * ra[0] + la[9] * ra[4] + la[10] * ra[8] + la[11] * ra[12], la[8] * ra[1] + la[9] * ra[5] + la[10] * ra[9] + la[11] * ra[13], la[8] * ra[2] + la[9] * ra[6] + la[10] * ra[10] + la[11] * ra[14], la[8] * ra[3] + la[9] * ra[7] + la[10] * ra[11] + la[11] * ra[15],
				la[12] * ra[0] + la[13] * ra[4] + la[14] * ra[8] + la[15] * ra[12], la[12] * ra[1] + la[13] * ra[5] + la[14] * ra[9] + la[15] * ra[13], la[12] * ra[2] + la[13] * ra[6] + la[14] * ra[10] + la[15] * ra[14], la[12] * ra[3] + la[13] * ra[7] + la[14] * ra[11] + la[15] * ra[15]
			);
		}

		public Mat4 Translate(Vec3 trans) {
			return this * Translation(trans);
		}

		public override string ToString() {
			return $"Mat4[ {_00} {_01} {_02} {_03}\n      {_10} {_11} {_12} {_13}\n      {_20} {_21} {_22} {_23}\n      {_30} {_31} {_32} {_33} ]";
		}
	}
}
