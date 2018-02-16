using System;
using System.Collections.Generic;
using static System.Console;
using static WebArena.Globals;

namespace WebArena {
	class Trace {
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

		Trace GroundTrace;
		
		public void Move(Vec3 dir, double frameTime) {
			FrameTime = frameTime * 0.0075;

			OnGround = GroundCheck();

			if(OnGround)
				WalkMove(dir.Normalized);
			else
				AirMove(dir.Normalized);
		}

		void WalkMove(Vec3 dir) {
			ApplyFriction();

			var speed = dir.Length * Q3Scale;
			
			Accelerate(dir, speed, Q3Accelerate);

			Velocity = ClipVelocity(Velocity, GroundTrace.Plane.Normal);

			if(Velocity.X == 0 && Velocity.Y == 0) return;
			
			StepSlideMove(false);
		}

		void ApplyFriction() {
			if(!OnGround) return;

			var speed = Velocity.Length;
			var drop = Math.Max(Q3StopSpeed, speed);
			var newSpeed = Math.Max(0, speed - drop);
			if(speed != 0)
				Velocity *= newSpeed / speed;
			else
				Velocity = vec3();
		}

		void AirMove(Vec3 dir) {
			var speed = dir.Length * Q3Scale;
			Accelerate(dir, speed, Q3AirAccelerate);

			StepSlideMove(true);
		}

		void Accelerate(Vec3 dir, double speed, double accel) {
			var curspeed = Velocity % dir;
			var add = speed - curspeed;
			if(add > 0)
				Velocity += dir * Math.Min(accel * FrameTime * speed, add);
		}

		void StepSlideMove(bool gravity) {
			var startO = Position;
			var startV = Velocity;

			if(!SlideMove(gravity))
				return;

			var down = startO;
			down.Z -= Q3StepSize;
			var trace = Trace(startO, down, Q3PlayerRadius);

			var up = vec3(0, 0, 1);

			if(Velocity.Z > 0 && (trace.Fraction == 1 || trace.Plane.Normal % up < 0.7)) return;

			var downO = Position;
			var downV = Velocity;

			up = startO;
			up.Z += Q3StepSize;

			trace = Trace(startO, up, Q3PlayerRadius);
			if(trace.AllSolid) return;

			var stepSize = trace.End.Z - startO.Z;
			Position = trace.End;
			Velocity = startV;

			SlideMove(gravity);

			down = Position;
			down.Z -= stepSize;
			trace = Trace(Position, down, Q3PlayerRadius);
			if(!trace.AllSolid)
				Position = trace.End;
			if(trace.Fraction < 1)
				Velocity = ClipVelocity(Velocity, trace.Plane.Normal);
		}

		bool SlideMove(bool gravity) {
			var endVelocity = vec3();
			var planes = new List<Vec3>();
			
			if(gravity) {
				endVelocity = Velocity;
				endVelocity.Z -= Q3Gravity * FrameTime;
				Velocity.Z = (Velocity.Z + endVelocity.Z) / 2;
				if(GroundTrace != null && GroundTrace.Plane != null)
					Velocity = ClipVelocity(Velocity, GroundTrace.Plane.Normal);
			}
			
			if(GroundTrace?.Plane != null)
				planes.Add(GroundTrace.Plane.Normal);
			
			planes.Add(Velocity.Normalized);

			var timeLeft = FrameTime;
			int bumpCount;
			for(bumpCount = 0; bumpCount < 4; ++bumpCount) {
				var end = Position + Velocity * timeLeft;
				
				var trace = Trace(Position, end, Q3PlayerRadius);

				if(trace.AllSolid) {
					Velocity.Z = 0;
					return true;
				}

				if(trace.Fraction > 0)
					Position = trace.End;
				
				if(trace.Fraction == 1)
					break;

				timeLeft -= timeLeft * trace.Fraction;
				
				planes.Add(trace.Plane.Normal);

				for(var i = 0; i < planes.Count; ++i) {
					var plane = planes[i];
					if(Velocity % plane >= 0.1) continue;

					var cvel = ClipVelocity(Velocity, plane);
					var ecvel = ClipVelocity(endVelocity, plane);

					for(var j = 0; j < planes.Count; ++j) {
						if(i == j) continue;
						var splane = planes[j];
						if(cvel % splane >= 0.1) continue;

						cvel = ClipVelocity(cvel, splane);
						ecvel = ClipVelocity(ecvel, splane);

						if(cvel % plane >= 0) continue;

						var dir = (plane ^ splane).Normalized;
						cvel = dir * (dir % Velocity);
						ecvel = dir * (dir % endVelocity);

						for(var k = 0; k < planes.Count; ++k) {
							if(i == k || j == k) continue;
							var tplane = planes[k];
							if(cvel % tplane >= 0.1) continue;

							Velocity = vec3();
							return true;
						}
					}

					Velocity = cvel;
					endVelocity = ecvel;
					break;
				}
			}

			if(gravity)
				Velocity = endVelocity;

			return bumpCount != 0;
		}

		Vec3 ClipVelocity(Vec3 vel, Vec3 normal) {
			var backoff = vel % normal;

			if(backoff < 0)
				backoff *= Q3Overclip;
			else
				backoff /= Q3Overclip;

			return vel - normal * backoff;
		}

		bool GroundCheck() {
			var checkPoint = Position - vec3(0, 0, Q3PlayerRadius + 0.25);
			GroundTrace = Trace(Position, checkPoint, Q3PlayerRadius);
			if(GroundTrace.Fraction == 1 || GroundTrace.Plane == null || (Velocity.Z > 0 && Velocity % GroundTrace.Plane.Normal > 10))
				return false;

			return GroundTrace.Plane.Normal.Z >= 0.7;
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
					if(brush.Collidable)
						TraceBrush(brush, start, end, radius, ref trace);
				return;
			}

			var plane = node.Plane;
			var startDist = plane.Normal % start - plane.Distance;
			var endDist = plane.Normal % end - plane.Distance;

			if(startDist >= radius && endDist >= radius)
				TraceNode(startFraction, endFraction, start, end, radius, node.Left, ref trace);
			else if(startDist < -radius && endDist < -radius)
				TraceNode(startFraction, endFraction, start, end, radius, node.Right, ref trace);
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

			if(brush.Planes.Length == 0)
				return;
			
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
