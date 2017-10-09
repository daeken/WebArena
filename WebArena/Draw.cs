using static WebArena.Globals;

namespace WebArena {
	class Draw {
		public Draw() {
			gl.ClearColor(0, 0, 1, 1);
			gl.Enable(gl.DEPTH_TEST);
		}

		public void Render() {
			gl.Clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);

			foreach(var drawable in Scene.Drawables)
				drawable.Draw();
		}
	}
}
