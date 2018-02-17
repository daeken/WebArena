using System.Runtime.InteropServices;

namespace Converter {
	[StructLayout(LayoutKind.Sequential)]
	public struct Vec2 {
		public float X, Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Vec3 {
		public float X, Y, Z;
	}
}