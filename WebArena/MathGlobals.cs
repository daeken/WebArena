using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebArena {
	static partial class Globals {
		public static Vec2 vec2() { return new Vec2(); }
		public static Vec2 vec2(float v) { return new Vec2(v); }
		public static Vec2 vec2(double v) { return new Vec2(v); }
		public static Vec2 vec2(float x, float y) { return new Vec2(x, y); }
		public static Vec2 vec2(double x, double y) { return new Vec2(x, y); }

		public static Vec3 vec3() { return new Vec3(); }
		public static Vec3 vec3(float v) { return new Vec3(v); }
		public static Vec3 vec3(double v) { return new Vec3(v); }
		public static Vec3 vec3(float x, float y, float z) { return new Vec3(x, y, z); }
		public static Vec3 vec3(double x, double y, double z) { return new Vec3(x, y, z); }

		public static Vec4 vec4() { return new Vec4(); }
		public static Vec4 vec4(float v) { return new Vec4(v); }
		public static Vec4 vec4(double v) { return new Vec4(v); }
		public static Vec4 vec4(float x, float y, float z, float w) { return new Vec4(x, y, z, w); }
		public static Vec4 vec4(double x, double y, double z, double w) { return new Vec4(x, y, z, w); }
	}
}
