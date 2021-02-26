using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Curve = System.Func<float, UnityEngine.Vector3>;

public static class Curves
{
	public static Curve Cubic(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		return t => Vec.Lerp3(a, b, c, d, t);
	}

	public static Curve Translate(Curve p, Vector3 vec)
    {
		return t => p(t) + vec;
    }

	public static float Length(Curve p, int interpolationSegments)
	{
		var curve = Enumerable.Range(0, interpolationSegments + 1)
			.Select(i => p((float)i / interpolationSegments))
			.ToArray();

		return Enumerable.Range(0, interpolationSegments)
			.Select(i => (curve[i + 1] - curve[i]).magnitude).Sum();
	}

	public static void DrawDebug(Curve p, int interpolationSegments, Color color)
	{
		var curve = Enumerable.Range(0, interpolationSegments + 1)
			.Select(i => p((float)i / interpolationSegments))
			.ToArray();

		DrawDebug(curve, color);
	}

	public static void DrawDebug(Vector3[] curve, Color color)
	{
		for (int i = 0; i + 1 < curve.Length; i++)
			Debug.DrawLine(curve[i], curve[i + 1], color);
	}
}
