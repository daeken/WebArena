using System.Collections.Generic;
using static WebArena.Globals;

namespace WebArena {
	interface IDrawable {
		void Update();
		void Draw(bool transparent);
	}

	class Node : IDrawable {
		public Vec3 Position;
		public Quaternion Rotation;
		public List<IDrawable> Children = new List<IDrawable>();

		public void Add(IDrawable drawable) {
			Children.Add(drawable);
		}

		public virtual void Update() {
			Children.ForEach(x => x.Update());
		}

		public virtual void Draw(bool transparent) {
			PushMatrix();
			var temp = ModelMatrix;
			ModelMatrix = Rotation.ToMatrix();
			ModelMatrix *= temp;
			ModelMatrix *= Mat4.Translation(Position);
			Children.ForEach(x => x.Draw(transparent));
			PopMatrix();
		}
	}

	class SceneGraph : Node {
	}
}
