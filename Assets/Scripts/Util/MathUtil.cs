using System.Runtime.InteropServices;
using UnityEngine;

// Credit to SaffronCR: https://gist.github.com/SaffronCR/b0802d102dd7f262118ac853cd5b4901
public class MathUtil
{
	// Evil floating point bit level hacking.
	[StructLayout(LayoutKind.Explicit)]
	private struct FloatIntUnion
	{
		[FieldOffset(0)]
		public float f;

		[FieldOffset(0)]
		public int tmp;
	}

	public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
	{
		Vector3 dir = point - pivot;
		dir = rotation * dir;
		point = dir + pivot;

		return point;
	}

	public static Vector3 SampleParabola(Vector3 start, Vector3 end, float height, float t)
	{
		if (Mathf.Abs(start.y - end.y) < 0.1f)
		{
			// Start and end are roughly level, pretend they are - simpler solution with less steps
			Vector3 travelDirection = end - start;
			Vector3 result = start + t * travelDirection;
			result.y += Mathf.Sin(t * Mathf.PI) * height;
			return result;
		}
		else
		{
			// Start and end are not level, gets more complicated
			Vector3 travelDirection = end - start;
			Vector3 levelDirecteion = end - new Vector3(start.x, end.y, start.z);
			Vector3 right = Vector3.Cross(travelDirection, levelDirecteion);
			Vector3 up = Vector3.Cross(right, travelDirection);
			if (end.y > start.y) up = -up;
			Vector3 result = start + t * travelDirection;
			result += (Mathf.Sin(t * Mathf.PI) * height) * up.normalized;
			return result;
		}
	}

	public static Vector3 ClosestPointOnLine(Vector3 vA, Vector3 vB, Vector3 vPoint)
	{
		Vector3 vVector1 = vPoint - vA;
		Vector3 vVector2 = (vB - vA).normalized;

		float d = Vector3.Distance(vA, vB);
		float t = Vector3.Dot(vVector2, vVector1);

		if (t <= 0)
			return vA;

		if (t >= d)
			return vB;

		Vector3 vVector3 = vVector2 * t;
		Vector3 vClosestPoint = vA + vVector3;
		return vClosestPoint;
	}

	/// Determine the signed angle between two vectors, with normal 'n'
	/// as the rotation axis.
	public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
	{
		return Mathf.Atan2(
				Vector3.Dot(n, Vector3.Cross(v1, v2)),
				Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
	}

	/// Low accuracy sqrt method for fast calculation.
	public static float FastSqrt(float z)
	{
		if (z == 0) return 0;
		FloatIntUnion u;
		u.tmp = 0;
		u.f = z;
		u.tmp -= 1 << 23; // Subtract 2^m.
		u.tmp >>= 1; // Divide by 2.
		u.tmp += 1 << 29; // Add ((b + 1) / 2) * 2^m.
		return u.f;
	}

	/// Returns the distance between a and b (approximately).
	public static float AproxDistance(Vector3 a, Vector3 b)
	{
		Vector3 vector = new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
		return InvSqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
	}

	/// The Infamous Unusual Fast Inverse Square Root (TM).
	public static float InvSqrt(float z)
	{
		if (z == 0) return 0;
		FloatIntUnion u;
		u.tmp = 0;
		float xhalf = 0.5f * z;
		u.f = z;
		u.tmp = 0x5f375a86 - (u.tmp >> 1);
		u.f = u.f * (1.5f - xhalf * u.f * u.f);
		return u.f * z;
	}

    public static Vector3 FastNormalize(Vector3 v)
    {
        float invMagnitude = InvSqrt(v.x*v.x + v.y*v.y + v.z+v.z);
        // Multiplying the vector by the inverse magnitude is the same as dividing by magnitude, which yields the normal
        return v * invMagnitude;
    }

    public static float FastMagnitude(Vector3 v)
    {
        return 1.0f / MathUtil.InvSqrt(v.x*v.x + v.y*v.y + v.z+v.z);
    }

	/// Returns the distance between a and b (fast but very low accuracy).
	public static float FastDistance(Vector3 a, Vector3 b)
	{
		Vector3 vector = new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
		return FastSqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
	}
}