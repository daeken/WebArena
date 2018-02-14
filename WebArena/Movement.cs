using System;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	struct Trace {
		public bool AllSolid, StartSolid;
		public double Fraction;
		public Vec3 End;
		public BspCollisionPlane Plane;
	}
	
	class Movement {
		public void Move() {

		}

		public Trace Trace(Vec3 start, Vec3 end, double radius = 0) {
			var trace = new Trace {
				AllSolid = false, 
				StartSolid = false, 
				End = end, 
				Plane = null
			};
			TraceNode(0, 1, start, end, radius, CurrentMap.CollisionTree, trace);
			if(trace.Fraction != 1)
				trace.End = Lerp(start, end, trace.Fraction);
			return trace;
		}

		const double q3bsptree_trace_offset = 0.03125;

		void TraceNode(double startFraction, double endFraction, Vec3 start, Vec3 end, double radius, BspCollisionTree node, Trace trace) {
			if(node.Leaf) {
				node.Brushes.ForEach(brush => TraceBrush(brush, start, end, radius, trace));
				return;
			}

			var plane = node.Plane;
			var startDist = plane.Normal % start - plane.Distance;
			var endDist = plane.Normal % end - plane.Distance;

			if(startDist >= radius && endDist >= radius)
				TraceNode(startFraction, endFraction, end, start, radius, node.Left, trace);
			else if(startDist < -radius && endDist < -radius)
				TraceNode(startFraction, endFraction, end, start, radius, node.Right, trace);
			else {
				var back = startDist < endDist;
				double fraction1 = 1, fraction2 = 0;
				if(startDist < endDist) {
					var iDist = 1 / (startDist - endDist);
					fraction1 = (startDist - radius + q3bsptree_trace_offset) * iDist;
					fraction2 = (startDist + radius + q3bsptree_trace_offset) * iDist;
				} else if(startDist > endDist) {
					var iDist = 1 / (startDist - endDist);
					fraction1 = (startDist + radius + q3bsptree_trace_offset) * iDist;
					fraction2 = (startDist - radius + q3bsptree_trace_offset) * iDist;
				}

				fraction1 = Clamp(fraction1, 0, 1);
				fraction2 = Clamp(fraction2, 0, 1);

				TraceNode(
					startFraction, 
					Lerp(startFraction, endFraction, fraction1), 
					start, 
					Lerp(start, end, fraction1), 
					radius, 
					back ? node.Right : node.Left, 
					trace
				);
				
				TraceNode(
					Lerp(startFraction, endFraction, fraction2), 
					endFraction, 
					Lerp(start, end, fraction2), 
					end, 
					radius, 
					back ? node.Left : node.Right, 
					trace
				);
			}
		}

		void TraceBrush(BspCollisionBrush brush, Vec3 start, Vec3 end, double radius, Trace trace) {
			var startFraction = -1.0;
			var endFraction = 1.0;
			var startsOut = false;
			var endsOut = false;
			BspCollisionPlane collisionPlane = null;

			foreach(var plane in brush.Planes) {
				var startDist = start % plane.Normal - (plane.Distance + radius);
				var endDist = end % plane.Normal - (plane.Distance + radius);
				
				if(startDist > 0) startsOut = true;
				if(endDist > 0) endsOut = true;

				if(startDist > 0 && endDist > 0) return;
				if(startDist <= 0 && endDist <= 0) continue;

				if(startDist > endDist) {
					var fraction = (startDist - q3bsptree_trace_offset) / (startDist - endDist);
					if(fraction > startFraction) {
						startFraction = fraction;
						collisionPlane = plane;
					}
				} else {
					var fraction = (startDist + q3bsptree_trace_offset) / (startDist - endDist);
					if(fraction < endFraction)
						endFraction = fraction;
				}
			}

			if(!startsOut) {
				trace.StartSolid = true;
				if(!endsOut)
					trace.AllSolid = true;
			} else if(startFraction < endFraction && startFraction > -1 && startFraction < trace.Fraction) {
				trace.Plane = collisionPlane;
				if(startFraction < 0)
					startFraction = 0;
				trace.Fraction = startFraction;
			}
		}
	}
}
