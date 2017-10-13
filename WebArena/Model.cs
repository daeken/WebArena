using System.Collections.Generic;
using static WebArena.Globals;

namespace WebArena {
	class Model : IDrawable {
		List<Mesh> Meshes;
		public Vec3 Position;

		public Model() {
			Meshes = new List<Mesh>();
		}

		public void AddMesh(Mesh mesh) {
			Meshes.Add(mesh);
		}

		public void Draw(bool transparent) {
			PushMatrix();
			if(Position.Length != 0)
				TranslateModel(Position);
			foreach(var mesh in Meshes)
				mesh.Draw(transparent);
			PopMatrix();
		}

		public virtual void Update() {
		}
	}
}
