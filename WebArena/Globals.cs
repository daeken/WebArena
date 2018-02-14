using Bridge.Html5;
using Bridge.WebGL;
using System.Collections.Generic;

namespace WebArena {
	static partial class Globals {
		public static double CurTime => new Date().GetTime() / 1000;
		public static double Time;
		public static WebGLRenderingContext gl;
		public static SceneGraph Scene;
		public static Camera PlayerCamera;
		public static Mat4 ModelMatrix = Mat4.Identity;
		public static Mat4 ProjectionMatrix;
		static readonly Stack<Mat4> MatrixStack = new Stack<Mat4>();
		public static Bsp CurrentMap;

		public static void PushMatrix() {
			MatrixStack.Push(ModelMatrix);
		}

		public static void PopMatrix() {
			ModelMatrix = MatrixStack.Pop();
		}

		public static void TranslateModel(Vec3 trans) {
			ModelMatrix = ModelMatrix.Translate(trans);
		}
	}
}
