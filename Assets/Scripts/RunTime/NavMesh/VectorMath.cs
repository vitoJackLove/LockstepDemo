using Unity.Mathematics.FixedPoint;
using UnityEngine;

namespace Ase
{

	public static class VectorMath
	{
		/// <summary>
		/// Complex number multiplication.
		/// Returns: a * b
		///
		/// Used to rotate vectors in an efficient way.
		///
		/// See: https://en.wikipedia.org/wiki/Complex_number<see cref="Multiplication_and_division"/>
		/// </summary>
		public static fp2 ComplexMultiply(fp2 a, fp2 b)
		{
			return new fp2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
		}

		/// <summary>
		/// Factor along the line which is closest to the point.
		/// Returned value is in the range [0,1] if the point lies on the segment otherwise it just lies on the line.
		/// The closest point can be calculated using (end-start)*factor + start.
		///
		/// See: ClosestPointOnLine
		/// See: ClosestPointOnSegment
		/// </summary>
		public static fp ClosestPointOnLineFactor(fp3 lineStart, fp3 lineEnd, fp3 point)
		{
			var dir = lineEnd - lineStart;
			fp sqrMagn = fpmath1.sqrMagnitude(dir);

			if (sqrMagn <= (fp)0.000001) return 0;

			return fpmath.dot(point - lineStart, dir) / sqrMagn;
		}

		/// <summary>
		/// Intersection of a line and a circle.
		/// Returns the greatest t such that segmentStart+t*(segmentEnd-segmentStart) lies on the circle.
		///
		/// In case the line does not intersect with the circle, the closest point on the line
		/// to the circle will be returned.
		///
		/// Note: Works for line and sphere in 3D space as well.
		///
		/// See: http://mathworld.wolfram.com/Circle-LineIntersection.html
		/// See: https://en.wikipedia.org/wiki/Intersection_(Euclidean_geometry)<see cref="A_line_and_a_circle"/>
		/// </summary>
		public static fp LineCircleIntersectionFactor(fp3 circleCenter, fp3 linePoint1, fp3 linePoint2,
			fp radius)
		{
			fp segmentLength;
			var normalizedDirection = Normalize(linePoint2 - linePoint1, out segmentLength);
			var dirToStart = linePoint1 - circleCenter;

			var dot = fpmath.dot(dirToStart, normalizedDirection);
			var discriminant = dot * dot - (fpmath1.sqrMagnitude(dirToStart) - radius * radius);

			if (discriminant < 0)
			{
				// No intersection, pick closest point on segment
				discriminant = 0;
			}

			var t = -dot + fpmath.sqrt(discriminant);
			// Note: the default value of 1 is important for the PathInterpolator.MoveToCircleIntersection2D
			// method to work properly. Maybe find some better abstraction where this default value is more obvious.
			return segmentLength > (fp)0.00001f ? t / segmentLength : 1;
		}

		/// <summary>
		/// Normalize vector and also return the magnitude.
		/// This is more efficient than calculating the magnitude and normalizing separately
		/// </summary>
		public static fp3 Normalize(fp3 v, out fp magnitude)
		{
			magnitude = fpmath1.magnitude(v);
			// This is the same constant that Unity uses
			if (magnitude > (fp)1E-05f)
			{
				return v / magnitude;
			}
			else
			{
				return fp3.zero;
			}
		}

		/// <summary>
		/// Normalize vector and also return the magnitude.
		/// This is more efficient than calculating the magnitude and normalizing separately
		/// </summary>
		public static fp2 Normalize(fp2 v, out fp magnitude)
		{
			magnitude = fpmath1.magnitude(v);
			// This is the same constant that Unity uses
			if (magnitude > (fp)1E-05f)
			{
				return v / magnitude;
			}
			else
			{
				return fp2.zero;
			}
		}

		/// <summary>
		/// Complex number multiplication.
		/// Returns: a * conjugate(b)
		///
		/// Used to rotate vectors in an efficient way.
		///
		/// See: https://en.wikipedia.org/wiki/Complex_number<see cref="Multiplication_and_division"/>
		/// See: https://en.wikipedia.org/wiki/Complex_conjugate
		/// </summary>
		public static fp2 ComplexMultiplyConjugate(fp2 a, fp2 b)
		{
			return new fp2(a.x * b.x + a.y * b.y, a.y * b.x - a.x * b.y);
		}

		/// <summary>Calculate an acceleration to move deltaPosition units and get there with approximately a velocity of targetVelocity</summary>
		public static fp2 CalculateAccelerationToReachPoint(fp2 deltaPosition, fp2 targetVelocity,
			fp2 currentVelocity, fp forwardsAcceleration, fp rotationSpeed, fp maxSpeed,
			fp2 forwardsVector)
		{
			// Guard against div by zero
			if (forwardsAcceleration <= 0) return fp2.zero;

			fp currentSpeed = fpmath1.magnitude(currentVelocity);

			// Convert rotation speed to an acceleration
			// See https://en.wikipedia.org/wiki/Centripetal_force
			var sidewaysAcceleration = currentSpeed * rotationSpeed * (fp)Mathf.Deg2Rad;

			// To avoid weird behaviour when the rotation speed is very low we allow the agent to accelerate sideways without rotating much
			// if the rotation speed is very small. Also guards against division by zero.
			sidewaysAcceleration = fpmath.max(sidewaysAcceleration, forwardsAcceleration);

			// Transform coordinates to local space where +X is the forwards direction
			// This is essentially equivalent to Transform.InverseTransformDirection.
			deltaPosition = VectorMath.ComplexMultiplyConjugate(deltaPosition, forwardsVector);
			targetVelocity = VectorMath.ComplexMultiplyConjugate(targetVelocity, forwardsVector);
			currentVelocity = VectorMath.ComplexMultiplyConjugate(currentVelocity, forwardsVector);
			fp ellipseSqrFactorX = 1 / (forwardsAcceleration * forwardsAcceleration);
			fp ellipseSqrFactorY = 1 / (sidewaysAcceleration * sidewaysAcceleration);

			// If the target velocity is zero we can use a more fancy approach
			// and calculate a nicer path.
			// In particular, this is the case at the end of the path.
			if ((targetVelocity == fp2.zero).Bool2ToBool())
			{
				// Run a binary search over the time to get to the target point.
				fp mn = (fp)0.01f;
				fp mx = 10;
				while (mx - mn > (fp)0.01f)
				{
					var time = (mx + mn) * (fp)0.5f;

					// Given that we want to move deltaPosition units from out current position, that our current velocity is given
					// and that when we reach the target we want our velocity to be zero. Also assume that our acceleration will
					// vary linearly during the slowdown. Then we can calculate what our acceleration should be during this frame.

					//{ t = time
					//{ deltaPosition = vt + at^2/2 + qt^3/6
					//{ 0 = v + at + qt^2/2
					//{ solve for a
					// a = acceleration vector
					// q = derivative of the acceleration vector
					var a = (6 * deltaPosition - 4 * time * currentVelocity) / (time * time);
					var q = 6 * (time * currentVelocity - 2 * deltaPosition) / (time * time * time);

					// Make sure the acceleration is not greater than our maximum allowed acceleration.
					// If it is we increase the time we want to use to get to the target
					// and if it is not, we decrease the time to get there faster.
					// Since the acceleration is described by acceleration = a + q*t
					// we only need to check at t=0 and t=time.
					// Note that the acceleration limit is described by an ellipse, not a circle.
					var nextA = a + q * time;
					if (a.x * a.x * ellipseSqrFactorX + a.y * a.y * ellipseSqrFactorY > 1 ||
					    nextA.x * nextA.x * ellipseSqrFactorX + nextA.y * nextA.y * ellipseSqrFactorY > 1)
					{
						mn = time;
					}
					else
					{
						mx = time;
					}
				}

				var finalAcceleration = (6 * deltaPosition - 4 * mx * currentVelocity) / (mx * mx);

				// Boosting
				{
					// The trajectory calculated above has a tendency to use very wide arcs
					// and that does unfortunately not look particularly good in some cases.
					// Here we amplify the component of the acceleration that is perpendicular
					// to our current velocity. This will make the agent turn towards the
					// target quicker.
					// How much amplification to use. Value is unitless.
					 fp Boost = 1;
					finalAcceleration.y *= 1 + Boost;

					// Clamp the velocity to the maximum acceleration.
					// Note that the maximum acceleration constraint is shaped like an ellipse, not like a circle.
					fp ellipseMagnitude = finalAcceleration.x * finalAcceleration.x * ellipseSqrFactorX +
					                      finalAcceleration.y * finalAcceleration.y * ellipseSqrFactorY;
					if (ellipseMagnitude > 1) finalAcceleration /= fpmath.sqrt(ellipseMagnitude);
				}

				return VectorMath.ComplexMultiply(finalAcceleration, forwardsVector);
			}
			else
			{
				// Here we try to move towards the next waypoint which has been modified slightly using our
				// desired velocity at that point so that the agent will more smoothly round the corner.

				// How much to strive for making sure we reach the target point with the target velocity. Unitless.
				fp TargetVelocityWeight = (fp)0.5f;

				// Limit to how much to care about the target velocity. Value is in seconds.
				// This prevents the character from moving away from the path too much when the target point is far away
				fp TargetVelocityWeightLimit = (fp)1.5f;
				
				fp targetSpeed;
				
				var normalizedTargetVelocity = VectorMath.Normalize(targetVelocity, out targetSpeed);

				var distance = fpmath1.magnitude(deltaPosition);
				var targetPoint = deltaPosition - normalizedTargetVelocity *
					System.Math.Min(TargetVelocityWeight * distance * targetSpeed / (currentSpeed + targetSpeed),
						maxSpeed * TargetVelocityWeightLimit);

				// How quickly the agent will try to reach the velocity that we want it to have.
				// We need this to prevent oscillations and jitter which is what happens if
				// we let the constant go towards zero. Value is in seconds.
				 fp TimeToReachDesiredVelocity = (fp)0.1f;
				// TODO: Clamp to ellipse using more accurate acceleration (use rotation speed as well)
				var finalAcceleration = (fpmath.normalize(targetPoint) * maxSpeed - currentVelocity) *
				                        (1 / TimeToReachDesiredVelocity);

				// Clamp the velocity to the maximum acceleration.
				// Note that the maximum acceleration constraint is shaped like an ellipse, not like a circle.
				fp ellipseMagnitude = finalAcceleration.x * finalAcceleration.x * ellipseSqrFactorX +
				                      finalAcceleration.y * finalAcceleration.y * ellipseSqrFactorY;
				if (ellipseMagnitude > 1) finalAcceleration /= fpmath.sqrt(ellipseMagnitude);

				return VectorMath.ComplexMultiply(finalAcceleration, forwardsVector);
			}
		}

		/// <summary>
		/// Clamps the velocity to the max speed and optionally the forwards direction.
		///
		/// Note that all vectors are 2D vectors, not 3D vectors.
		///
		/// Returns: The clamped velocity in world units per second.
		/// </summary>
		/// <param name="velocity">Desired velocity of the character. In world units per second.</param>
		/// <param name="maxSpeed">Max speed of the character. In world units per second.</param>
		/// <param name="slowdownFactor">Value between 0 and 1 which determines how much slower the character should move than normal.
		///      Normally 1 but should go to 0 when the character approaches the end of the path.</param>
		/// <param name="slowWhenNotFacingTarget">Prevent the velocity from being too far away from the forward direction of the character
		///      and slow the character down if the desired velocity is not in the same direction as the forward vector.</param>
		/// <param name="forward">Forward direction of the character. Used together with the slowWhenNotFacingTarget parameter.</param>
		/*public static Vector2 ClampVelocity(Vector2 velocity, float maxSpeed, float slowdownFactor,
			bool slowWhenNotFacingTarget, Vector2 forward)
		{
			// Max speed to use for this frame
			var currentMaxSpeed = maxSpeed * slowdownFactor;

			// Check if the agent should slow down in case it is not facing the direction it wants to move in
			if (slowWhenNotFacingTarget && (forward.x != 0 || forward.y != 0))
			{
				float currentSpeed;
				var normalizedVelocity = VectorMath.Normalize(velocity, out currentSpeed);
				float dot = Vector2.Dot(normalizedVelocity, forward);

				// Lower the speed when the character's forward direction is not pointing towards the desired velocity
				// 1 when velocity is in the same direction as forward
				// 0.2 when they point in the opposite directions
				float directionSpeedFactor = Mathf.Clamp(dot + 0.707f, 0.2f, 1.0f);
				currentMaxSpeed *= directionSpeedFactor;
				currentSpeed = Mathf.Min(currentSpeed, currentMaxSpeed);

				// Angle between the forwards direction of the character and our desired velocity
				float angle = Mathf.Acos(Mathf.Clamp(dot, -1, 1));

				// Clamp the angle to 20 degrees
				// We cannot keep the velocity exactly in the forwards direction of the character
				// because we use the rotation to determine in which direction to rotate and if
				// the velocity would always be in the forwards direction of the character then
				// the character would never rotate.
				// Allow larger angles when near the end of the path to prevent oscillations.
				angle = Mathf.Min(angle, (20f + 180f * (1 - slowdownFactor * slowdownFactor)) * Mathf.Deg2Rad);

				float sin = Mathf.Sin(angle);
				float cos = Mathf.Cos(angle);

				// Determine if we should rotate clockwise or counter-clockwise to move towards the current velocity
				sin *= Mathf.Sign(normalizedVelocity.x * forward.y - normalizedVelocity.y * forward.x);
				// Rotate the #forward vector by #angle radians
				// The rotation is done using an inlined rotation matrix.
				// See https://en.wikipedia.org/wiki/Rotation_matrix
				return new Vector2(forward.x * cos + forward.y * sin, forward.y * cos - forward.x * sin) * currentSpeed;
			}
			else
			{
				return Vector2.ClampMagnitude(velocity, currentMaxSpeed);
			}
		}*/

		/// <summary>
		/// Transforms from world space to the 'ground' plane of the graph.
		/// The transformation is purely a rotation so no scale or offset is used.
		///
		/// For a graph rotated with the rotation (-90, 0, 0) this will transform
		/// a coordinate (x,y,z) to (x,y). For a graph with the rotation (0,0,0)
		/// this will tranform a coordinate (x,y,z) to (x,z). More generally for
		/// a graph with a quaternion rotation R this will transform a vector V
		/// to R * V (i.e rotate the vector V using the rotation R).
		/// </summary>
		public static Vector2 ToPlane(Vector3 point)
		{
			return new Vector2(point.x, point.z);
		}

		/// <summary>
		/// Transforms from world space to the 'ground' plane of the graph.
		/// The transformation is purely a rotation so no scale or offset is used.
		/// </summary>
		public static Vector2 ToPlane(Vector3 point, out float elevation)
		{
			elevation = point.y;
			return new Vector2(point.x, point.z);
		}

		/// <summary>
		/// Transforms from the 'ground' plane of the graph to world space.
		/// The transformation is purely a rotation so no scale or offset is used.
		/// </summary>
		public static Vector3 ToWorld(Vector2 point, float elevation)
		{
			return new Vector3(point.x, elevation, point.y);
		}
	}
}