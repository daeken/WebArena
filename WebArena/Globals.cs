using Bridge.Html5;
using Bridge.WebGL;

namespace WebArena {
	static partial class Globals {
		public static double CurTime => new Date().GetTime() / 1000;
		public static double Time;
		public static WebGLRenderingContext gl;
		public static SceneGraph Scene;
		public static Camera PlayerCamera;
	}
}
