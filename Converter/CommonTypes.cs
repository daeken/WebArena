using System.Runtime.InteropServices;

namespace Converter {
	[StructLayout(LayoutKind.Sequential)]
	public struct Vec2 {
		public float X, Y;

		public float[] ToArray => new[] {X, Y};

		public override string ToString() {
			return $"Vec2[ {X} {Y} ]";
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Vec3 {
		public float X, Y, Z;

		public float[] ToArray => new[] {X, Y, Z};

		public override string ToString() {
			return $"Vec3[ {X} {Y} {Z} ]";
		}
	}
}