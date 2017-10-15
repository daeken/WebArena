using System.Collections.Generic;
using static WebArena.Globals;

namespace WebArena {
	class Model : IDrawable {
		List<Mesh> Meshes;

		public Model() {
			Meshes = new List<Mesh>();
		}

		public void AddMesh(Mesh mesh) {
			Meshes.Add(mesh);
		}

		public virtual void Draw(bool transparent) {
			Meshes.ForEach(x => x.Draw(transparent));
		}

		public virtual void Update() {
		}
	}
}
