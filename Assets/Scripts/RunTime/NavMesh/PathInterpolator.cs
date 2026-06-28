using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

namespace Ase
{
	/// <summary>
	/// 沿着一系列的坐标点进行插值工具类
	/// </summary>
	public sealed class PathInterpolator
	{
		private List<fp3> path = new List<fp3>();

		private fp distanceToSegmentStart;
		private fp currentDistance;
		private fp currentSegmentLength = (fp)float.PositiveInfinity;
		private fp totalDistance = (fp)float.PositiveInfinity;

		/// <summary>Current position</summary>
		public fp3 position
		{
			get
			{
				fp t = currentSegmentLength > (fp)0.0001f
					? (currentDistance - distanceToSegmentStart) / currentSegmentLength
					: 0;

				return fpmath.lerp(path[segmentIndex], path[segmentIndex + 1], t);
			}
		}

		/// <summary>
		/// 路径结束点坐标
		/// </summary>
		public fp3 endPoint => path[path.Count - 1];

		/// <summary>
		/// 曲线在当前位置的切线
		/// </summary>
		public fp3 tangent => path[segmentIndex + 1] - path[segmentIndex];

		/// <summary>
		/// 到路径终点的剩余距离
		/// </summary>
		public fp remainingDistance
		{
			//get => totalDistance - distance;
			//set => distance = totalDistance - value;
			get => fpmath.max(totalDistance - distance, 0);
			set => distance = fpmath.max(totalDistance - value, 0);
		}

		/// <summary>
		/// 从路径开始的遍历距离
		/// </summary>
		public fp distance
		{
			get => currentDistance;
			set
			{
				currentDistance = value;

				while (currentDistance < distanceToSegmentStart && segmentIndex > 0) PrevSegment();
				while (currentDistance > distanceToSegmentStart + currentSegmentLength &&
				       segmentIndex < path.Count - 2) NextSegment();
			}
		}

		/// <summary>
		/// 当前路段
		/// 线段的起点和终点分别是path[value]和path[value+1].
		/// </summary>
		public int segmentIndex { get; private set; }

		/// <summary>
		/// 是否为有效路径
		/// </summary>
		public bool valid => path != null && path.Count > 0;

		/// <summary>
		/// 将position和endPoint之间的剩余路径附加到buffer
		/// </summary>
		public void GetRemainingPath(List<fp3> buffer)
		{
			if (!valid) throw new System.Exception("PathInterpolator is not valid");
			buffer.Add(position);
			for (int i = segmentIndex + 1; i < path.Count; i++)
			{
				buffer.Add(path[i]);
			}
		}

		public void PathClear()
		{
			if(path!=null)
				path.Clear();
		}

		/// <summary>
		/// 设置路径
		/// 将重置所有插值变量
		/// </summary>
		public void SetPath(fp3[] pathList)
		{
			if (pathList == null)
			{
				return;
			}

			this.path.Clear();
			this.path.AddRange(pathList);
			currentDistance = 0;
			segmentIndex = 0;
			distanceToSegmentStart = 0;

			if (pathList.Length == 0)
			{
				totalDistance = (fp)float.PositiveInfinity;
				currentSegmentLength = (fp)float.PositiveInfinity;
				return;
			}
			
			if (this.path.Count == 1) 
				this.path.Add(path[0]);

			if (this.path.Count < 2) throw new System.ArgumentException("Path must have a length of at least 2");

			currentSegmentLength = fpmath1.magnitude((path[1] - path[0]));
			totalDistance = 0;

			var prev = path[0];
			for (int i = 1; i < path.Count; i++)
			{
				var current = path[i];
				totalDistance += fpmath1.magnitude((current - prev));
				prev = current;
			}
		}

		/// <summary>
		/// 移动到指定的路段并移动一小段路到下一个路段
		/// </summary>
		private void MoveToSegment(int index, fp fractionAlongSegment)
		{
			if (path == null) return;
			if (index < 0 || index >= path.Count - 1) throw new System.ArgumentOutOfRangeException(nameof(index));
			while (segmentIndex > index) PrevSegment();
			while (segmentIndex < index) NextSegment();
			distance = distanceToSegmentStart + fpmath1.Clamp01(fractionAlongSegment) * currentSegmentLength;
		}

		/// <summary>
		/// 尽可能靠近指定点
		/// </summary>
		/// <param name="point"></param>
		public void MoveToClosestPoint(fp3 point)
		{
			if (path == null) return;

			fp bestDist = (fp)float.PositiveInfinity;
			fp bestFactor = 0;
			int bestIndex = 0;

			for (int i = 0; i < path.Count - 1; i++)
			{
				fp factor = VectorMath.ClosestPointOnLineFactor(path[i], path[i + 1], point);
				fp3 closest = fpmath.lerp(path[i], path[i + 1], factor);
				fp dist = fpmath1.sqrMagnitude((point - closest));

				if (dist < bestDist)
				{
					bestDist = dist;
					bestFactor = factor;
					bestIndex = i;
				}
			}

			MoveToSegment(bestIndex, bestFactor);
		}

		//TODO
		/*public void MoveToLocallyClosestPoint(Vector3 point, bool allowForwards = true, bool allowBackwards = true)
		{
			if (path == null) return;

			while (allowForwards && segmentIndex < path.Count - 2 && (path[segmentIndex + 1] - point).sqrMagnitude <=
			       (path[segmentIndex] - point).sqrMagnitude)
			{
				NextSegment();
			}

			while (allowBackwards && segmentIndex > 0 && (path[segmentIndex - 1] - point).sqrMagnitude <=
			       (path[segmentIndex] - point).sqrMagnitude)
			{
				PrevSegment();
			}

			// Check the distances to the two segments extending from the vertex path[segmentIndex]
			// and pick the position on those segments that is closest to the #point parameter.
			float factor1 = 0, factor2 = 0, d1 = float.PositiveInfinity, d2 = float.PositiveInfinity;
			if (segmentIndex > 0)
			{
				factor1 = VectorMath.ClosestPointOnLineFactor(path[segmentIndex - 1], path[segmentIndex], point);
				d1 = (Vector3.Lerp(path[segmentIndex - 1], path[segmentIndex], factor1) - point).sqrMagnitude;
			}

			if (segmentIndex < path.Count - 1)
			{
				factor2 = VectorMath.ClosestPointOnLineFactor(path[segmentIndex], path[segmentIndex + 1], point);
				d2 = (Vector3.Lerp(path[segmentIndex], path[segmentIndex + 1], factor2) - point).sqrMagnitude;
			}

			if (d1 < d2) MoveToSegment(segmentIndex - 1, factor1);
			else MoveToSegment(segmentIndex, factor2);
		}*/

		//TODO
		/*public void MoveToCircleIntersection2D(Vector3 circleCenter3D, float radius)
		{
			if (path == null || path.Count == 0) return;

			while (segmentIndex < path.Count - 2 &&
			       VectorMath.ClosestPointOnLineFactor(path[segmentIndex], path[segmentIndex + 1], circleCenter3D) > 1)
			{
				NextSegment();
			}

			// Move forwards as long as the current segment endpoint is within the circle
			while (segmentIndex < path.Count - 2 &&
			       (path[segmentIndex + 1] - circleCenter3D).sqrMagnitude <= radius * radius)
			{
				NextSegment();
			}

			// Calculate the intersection with the circle. This involves some math.
			var factor =
				VectorMath.LineCircleIntersectionFactor(circleCenter3D, path[segmentIndex], path[segmentIndex + 1],
					radius);
			// Move to the intersection point
			MoveToSegment(segmentIndex, factor);
		}*/

		private void PrevSegment()
		{
			segmentIndex--;
			currentSegmentLength = fpmath1.magnitude((path[segmentIndex + 1] - path[segmentIndex]));
			distanceToSegmentStart -= currentSegmentLength;
		}

		private void NextSegment()
		{
			segmentIndex++;
			distanceToSegmentStart += currentSegmentLength;
			currentSegmentLength = fpmath1.magnitude((path[segmentIndex + 1] - path[segmentIndex]));
		}
	}
}