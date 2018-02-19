using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	class PlayerModel : Node {
		readonly Node HeadNode, UpperNode, LowerNode, WeaponNode;

		readonly List<Tuple<Md3, Node, Func<Md3>, Node, string>> Attachments = new List<Tuple<Md3, Node, Func<Md3>, Node, string>>();
		
		public Md3 Weapon {
			get { return WeaponNode.Children.Count == 0 ? null : (Md3) WeaponNode.Children[0]; }
			set {
				WeaponNode.Children.Clear();
				if(value != null)
					WeaponNode.Add(value);
			}
		}

		public PlayerModel(Md3Data head, Md3Data upper, Md3Data lower) {
			var hm = new Md3(head);
			var um = new Md3(upper);
			um.SetAnimation(90, 90, 152, 15);
			var lm = new Md3(lower);
			lm.SetAnimation(153 - 63, 153 - 63, 193 - 63, 20);

			HeadNode = new Node();
			HeadNode.Add(hm);
			UpperNode = new Node();
			UpperNode.Add(um);
			LowerNode = new Node();
			LowerNode.Add(lm);
			WeaponNode = new Node();

			Add(HeadNode);
			Add(UpperNode);
			Add(LowerNode);
			Add(WeaponNode);

			Attachments.Add(new Tuple<Md3, Node, Func<Md3>, Node, string>(lm, LowerNode, () => um, UpperNode, "tag_torso"));
			Attachments.Add(new Tuple<Md3, Node, Func<Md3>, Node, string>(um, UpperNode, () => hm, HeadNode, "tag_head"));
			Attachments.Add(new Tuple<Md3, Node, Func<Md3>, Node, string>(um, UpperNode, () => Weapon, WeaponNode, "tag_weapon"));
		}

		public override void Update() {
			Children.ForEach(x => x.Update());

			foreach(var t in Attachments) {
				var am = t.Item1;
				var an = t.Item2;
				var bm = t.Item3();
				var bn = t.Item4;
				var tag = t.Item5;
				if(bm == null)
					continue;
				var t1 = GetTag(am, an, tag);
				var t2 = GetTag(bm, bn, tag);
				bn.Position = an.Position + t1.Item1 - t2.Item1;
				bn.Rotation = t1.Item2 * t2.Item2;
			}
		}

		Tuple<Vec3, Quaternion> GetTag(Md3 model, Node node, string tag) {
			var frames = model.Tags[tag];
			if(model.CurA == model.CurB)
				return frames[model.CurA];

			var t1 = frames[model.CurA];
			var t2 = frames[model.CurB];

			return new Tuple<Vec3, Quaternion>(Lerp(t1.Item1, t2.Item1, model.CurrentLerp), Slerp(t1.Item2, t2.Item2, model.CurrentLerp));
		}
	}
}
