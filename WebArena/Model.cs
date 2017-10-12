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

		public void Draw(bool transparent) {
			foreach(var mesh in Meshes)
				mesh.Draw(transparent);
		}
	}
}
