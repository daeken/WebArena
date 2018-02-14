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
	
	// Largely derived from https://github.com/toji/webgl-quake3/blob/master/js/q3movement.js
	class Movement {
		const double Q3StopSpeed = 100.0;
		const double Q3DuckScale = 0.25;
		const double Q3JumpVelocity = 50;

		const double Q3Accelerate = 10.0;
		const double Q3AirAccelerate = 0.1;
		const double Q3FlyAccelerate = 8.0;

		const double Q3Friction = 6.0;
		const double Q3FlightFriction = 3.0;

		const double Q3FrameTime = 0.30;
		const double Q3Overclip = 0.501;
		const double Q3StepSize = 18;

		const double Q3Gravity = 20.0;

		const double Q3PlayerRadius = 10.0;
		const double Q3Scale = 50;

		const double Q3TraceOffset = 0.03125;
		
		public Vec3 Position;
		Vec3 Velocity;
		bool OnGround;
		double FrameTime;
		
		public void Move(Vec3 dir, double frameTime) {
			FrameTime = frameTime * 0.0075;

			OnGround = GroundCheck();
			
			if(OnGround)
				WriteLine($"On ground at {Floor(Position)}");

			if(OnGround)
				WalkMove(dir.Normalized);
			else
				AirMove(dir.Normalized);
		}

		void WalkMove(Vec3 dir) {
		}

		void AirMove(Vec3 dir) {
		}

		bool GroundCheck() {
			var checkPoint = Position - vec3(0, Q3PlayerRadius + 0.25, 0);
			var groundTrace = Trace(Position, checkPoint, Q3PlayerRadius);
			if(groundTrace.Plane != null)
				WriteLine($"Foo {groundTrace.Plane.Normal}");
			if(groundTrace.Fraction == 1 || groundTrace.Plane == null || (Velocity.Y > 0 && Velocity % groundTrace.Plane.Normal > 10))
				return false;

			return groundTrace.Plane.Normal.Y >= 0.7;
		}

		Trace Trace(Vec3 start, Vec3 end, double radius = 0) {
			var trace = new Trace {
				AllSolid = false, 
				StartSolid = false, 
				Fraction = 1, 
				End = end, 
				Plane = null
			};
			TraceNode(0, 1, start, end, radius, CurrentMap.CollisionTree, ref trace);
			if(trace.Fraction != 1)
				trace.End = Lerp(start, end, trace.Fraction);
			return trace;
		}

		void TraceNode(double startFraction, double endFraction, Vec3 start, Vec3 end, double radius, BspCollisionTree node, ref Trace trace) {
			if(node.Leaf) {
				foreach(var brush in node.Brushes)
					TraceBrush(brush, start, end, radius, ref trace);
				return;
			}

			var plane = node.Plane;
			var startDist = plane.Normal % start - plane.Distance;
			var endDist = plane.Normal % end - plane.Distance;

			if(startDist >= radius && endDist >= radius)
				TraceNode(startFraction, endFraction, end, start, radius, node.Left, ref trace);
			else if(startDist < -radius && endDist < -radius)
				TraceNode(startFraction, endFraction, end, start, radius, node.Right, ref trace);
			else {
				var back = startDist < endDist;
				double fraction1 = 1, fraction2 = 0;
				if(startDist < endDist) {
					var iDist = 1 / (startDist - endDist);
					fraction1 = (startDist - radius + Q3TraceOffset) * iDist;
					fraction2 = (startDist + radius + Q3TraceOffset) * iDist;
				} else if(startDist > endDist) {
					var iDist = 1 / (startDist - endDist);
					fraction1 = (startDist + radius + Q3TraceOffset) * iDist;
					fraction2 = (startDist - radius - Q3TraceOffset) * iDist;
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
					ref trace
				);
				
				TraceNode(
					Lerp(startFraction, endFraction, fraction2), 
					endFraction, 
					Lerp(start, end, fraction2), 
					end, 
					radius, 
					back ? node.Left : node.Right, 
					ref trace
				);
			}
		}

		void TraceBrush(BspCollisionBrush brush, Vec3 start, Vec3 end, double radius, ref Trace trace) {
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
					var fraction = (startDist - Q3TraceOffset) / (startDist - endDist);
					if(fraction > startFraction) {
						startFraction = fraction;
						collisionPlane = plane;
					}
				} else {
					var fraction = (startDist + Q3TraceOffset) / (startDist - endDist);
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
				trace.Fraction = startFraction < 0 ? 0 : startFraction;
			}
		}
	}
}
