using System;
using static WebArena.Globals;

namespace WebArena {
	class SpinningItem : Node {
		Vec3 Offset;
		public SpinningItem(Md3 item) {
			Offset = -item.CenterPoint;
			Add(item);
		}
		
		public override void Update() {
			Rotation = Quaternion.FromAxisAngle(vec3(0, 0, 1), Time * 5);
		}
		
		public override void Draw(bool transparent) {
			PushMatrix();
			var temp = ModelMatrix;
			ModelMatrix = Mat4.Translation(Offset);
			ModelMatrix *= Rotation.ToMatrix();
			ModelMatrix *= temp;
			ModelMatrix *= Mat4.Translation(Position + vec3(0, 0, 10) * Math.Sin(Time * 4));
			Children.ForEach(x => x.Draw(transparent));
			PopMatrix();
		}
	}
}