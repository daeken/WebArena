using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebArena {
	public struct Mat3 {
		public float _00, _01, _02;
		public float _10, _11, _12;
		public float _20, _21, _22;

		public float[] AsArray => new float[] { _00, _01, _02, _10, _11, _12, _20, _21, _22 };

		public static Mat3 Identity = new Mat3(
			1, 0, 0, 
			0, 1, 0, 
			0, 0, 1
		);

		public Mat3(float i_00, float i_01, float i_02, float i_10, float i_11, float i_12, float i_20, float i_21, float i_22) {
			_00 = i_00;
			_01 = i_01;
			_02 = i_02;
			_10 = i_10;
			_11 = i_11;
			_12 = i_12;
			_20 = i_20;
			_21 = i_21;
			_22 = i_22;
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
				la[3] * ra[0] + la[4] * ra[3] + la[5] * ra[6], la[3] * ra[1] + la[4] * ra[4] + la[8] * ra[7], la[3] * ra[2] + la[4] * ra[5] + la[5] * ra[8],
				la[6] * ra[0] + la[7] * ra[3] + la[8] * ra[6], la[6] * ra[1] + la[7] * ra[4] + la[8] * ra[7], la[6] * ra[2] + la[7] * ra[5] + la[8] * ra[8]
			);
		}
	}
}
