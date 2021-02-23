using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Curve = System.Func<float, UnityEngine.Vector3>;

public static class Vec
{
	public static Vector3 Lerp2(Vector3 a, Vector3 b, Vector3 c, float t)
	{
		return Vector3.Lerp(
			Vector3.Lerp(a, b, t),
			Vector3.Lerp(b, c, t),
			t);
	}
	public static Vector3 Lerp3(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
	{
		return Vector3.Lerp(
			Lerp2(a, b, c, t),
			Lerp2(b, c, d, t),
			t);
	}
	public static Curve CubicCurve(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
		return t => Lerp3(a, b, c, d, t);
    }

	public static float CurveLength(Curve p, int interpolationSegments)
    {
		var curve = Enumerable.Range(0, interpolationSegments + 1)
			.Select(i => p((float)i / interpolationSegments))
			.ToArray();

		return Enumerable.Range(0, interpolationSegments)
			.Select(i => (curve[i + 1] - curve[i]).magnitude).Sum();

	}

	public static void DrawDebugCurve(Curve p, int interpolationSegments, Color color)
	{
		var curve = Enumerable.Range(0, interpolationSegments + 1)
			.Select(i => p((float)i / interpolationSegments))
			.ToArray();

		DrawDebugCurve(curve, color);
	}

	public static void DrawDebugCurve(Vector3[] curve, Color color)
    {
		for (int i = 0; i + 1 < curve.Length; i++)
			Debug.DrawLine(curve[i], curve[i + 1], color);
    }

}