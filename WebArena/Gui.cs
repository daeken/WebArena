using System;
using Bridge.Html5;
using Bridge.WebGL;
using static WebArena.Globals;

namespace WebArena {
	public class Gui {
		public static HTMLCanvasElement Canvas;

		public Gui() {
			var style = new HTMLStyleElement {
				TextContent=@"
					* { margin: 0; padding: 0; }
					html, body { width: 100%; height: 100%; }
					canvas { display: block; }
				"
			};
			Document.Body.AppendChild(style);
			Canvas = new HTMLCanvasElement();
			Document.Body.AppendChild(Canvas);


			gl = Canvas.GetContext(CanvasTypes.CanvasContextWebGLType.WebGL).As<WebGLRenderingContext>();
			Resize();
			Window.OnResize += e => Resize();
		}
		
		void Resize() {
			Canvas.Style.Width = (Canvas.Width = Window.InnerWidth).ToString();
			Canvas.Style.Height = (Canvas.Height = Window.InnerHeight).ToString();
			ProjectionMatrix = Mat4.Perspective(45 * (Math.PI / 180), (double) Window.InnerWidth / Window.InnerHeight, 0.1, 10000);
			gl.Viewport(0, 0, Canvas.Width, Canvas.Height);
		}
	}
}