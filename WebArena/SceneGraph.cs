using System.Collections.Generic;

namespace WebArena {
	interface IDrawable {
		void Draw();
	}

	class SceneGraph {
		public List<IDrawable> Drawables;

		public SceneGraph() {
			Drawables = new List<IDrawable>();
		}

		public void Add(IDrawable drawable) {
			Drawables.Add(drawable);
		}
	}
}
