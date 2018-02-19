using System;
using System.Collections.Generic;
using static System.Math;

namespace WebArena {
	static partial class Globals {
		public static Vec2 vec2() => new Vec2();
		public static Vec2 vec2(float v) => new Vec2(v);
		public static Vec2 vec2(double v) => new Vec2(v);
		public static Vec2 vec2(float x, float y) => new Vec2(x, y);
		public static Vec2 vec2(double x, double y) => new Vec2(x, y);

		public static Vec3 vec3() => new Vec3();
		public static Vec3 vec3(float v) => new Vec3(v);
		public static Vec3 vec3(double v) => new Vec3(v);
		public static Vec3 vec3(float x, float y, float z) => new Vec3(x, y, z);
		public static Vec3 vec3(double x, double y, double z) => new Vec3(x, y, z);

		public static Vec4 vec4() => new Vec4();
		public static Vec4 vec4(float v) => new Vec4(v);
		public static Vec4 vec4(double v) => new Vec4(v);
		public static Vec4 vec4(float x, float y, float z, float w) => new Vec4(x, y, z, w);
		public static Vec4 vec4(double x, double y, double z, double w) => new Vec4(x, y, z, w);

		public static double Clamp(double x, double min, double max) => Min(Max(x, min), max);
		public static double Fract(double x) => x - Math.Floor(x);

		public static Vec3 Floor(Vec3 x) => vec3(Math.Floor(x.X), Math.Floor(x.Y), Math.Floor(x.Z));

		public static Vec3 VMin(Vec3 a, Vec3 b) => vec3(Min(a.X, b.X), Min(a.Y, b.Y), Min(a.Z, b.Z));
		public static Vec3 VMax(Vec3 a, Vec3 b) => vec3(Max(a.X, b.X), Max(a.Y, b.Y), Max(a.Z, b.Z));

		public static double Lerp(double a, double b, double x) => (b - a) * x + a;
		public static Vec3 Lerp(Vec3 a, Vec3 b, double x) => (b - a) * x + a;

		public static Quaternion Slerp(Quaternion a, Quaternion b, double x) {
			a = a.Normalized;
			b = b.Normalized;
			var dot = a % b;
			if(Abs(dot) > 0.9995)
				return (b - a) * x + a;
			
			if(dot < 0) {
				b = -b;
				dot = -dot;
			}

			dot = Clamp(dot, -1, 1);
			var theta = Acos(dot) * x;
			var c = (b - a * dot).Normalized;
			return a * Cos(theta) + c * Sin(theta);
		}

		public static bool RayPlaneIntersection(Vec3 origin, Vec3 dir, Vec3 normal, double distance, out double t, out double vd) {
			t = 0;
			vd = normal.X * dir.X + normal.Y * dir.Y + normal.Z * dir.Z;
			if(vd == 0)
				return false;
			t = -(normal.X * origin.X + normal.Y * origin.Y + normal.Z * origin.Z + distance) / vd;
			return true;
		}

		public static void SortPoints(List<Vec3> points, Vec3 normal) {
			if(points.Count < 3)
				return;
			var first = points[0];
			points.Sort((a, b) => {
				var v = (a - first) ^ (b - first);
				return v % normal < 0 ? 1 : 0;
			});
		}
	}
}
