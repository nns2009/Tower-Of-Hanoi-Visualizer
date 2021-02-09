//using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public class Manager : MonoBehaviour
{
	[Header("Tower Dimensions")]
	[Range(2, 20)]
	public int count;
	public float MinWidth, MaxWidth;
	[Range(2, 12)]
	public float TotalHeight;
	[Range(0, 8)]
	public float TowerSpacing;

	[Header("Tower Visuals")]
	public Sprite sprite;
	public Material material;
	public SpriteDrawMode spriteDrawMode;
	[Range(0, 1)]
	public float Saturation, OuterBrightness, MinHue, MaxHue;

	public bool ShowInnerSprite;
	public Vector2 InnerSpriteScaling = Vector2.one * 0.8f;
	public Vector2 InnerSpriteMargin = Vector2.zero;
	[Range(0, 1)]
	public float InnerBrightness = 0.5f;

	[Header("Path")]
	public int SplinePoints;
	public float BaseLiftLength;
	[Range(0, 1)]
	public float TopPullK, UpperBlockPullK;
	[Range(0, 3)]
	public float ExtraMiddlePullK;

	[Header("Animation")]
	public float RandomShiftScale;
	public float MovementSpeed;
	public float MovementTimeMultiplier, MovementTimePower;
	public float MoveDelay;

	[Header("Barrier")]
	public bool UseBarrier;
	public float BarrierK;
	public Transform Barrier;

	[Header("UI")]
	public string CountPrefix;
	public string CountSuffix;

	[Header("References")]
	public new Camera camera;
	public Transform Left, Middle, Right;
	public Transform Blocks;
	public TextMeshPro MoveCounterLabel;

	[Header("Debug")]
	public int MovesToSkip = 0;
	public bool ShowSpline = false;
	public bool ShowBarrier = false;
	public float BarrierDebugWidth = 1;
	public bool CreateExtraBottomBlocks = false;

	[Header("Computed")]
	[SerializeField, ReadOnly]
	private float Height;
	[SerializeField, ReadOnly]
	private float dWidth;
	[SerializeField, ReadOnly]
	int moveIndex;

	private Transform[] cols;
	[SerializeField, ReadOnly]
	private int[] counts;
	private Stack<Vector3>[] posStacks;
	private Stack<BoxCollider2D>[] boxStacks;
	[SerializeField, ReadOnly]
	private int[] blockMovesRemaining;

	const int LeftIndex = 0, MiddleIndex = 1, RightIndex = 2;

	public void Recreate()
	{
		while (Blocks.childCount > 0)
			DestroyImmediate(Blocks.GetChild(0).gameObject);

		Left.localPosition = Vector3.left * TowerSpacing;
		Middle.localPosition = Vector3.zero;
		Right.localPosition = Vector3.right * TowerSpacing;

		//cols = Enumerable.Range(0, 3).Select(i => new List<Transform>()).ToArray();
		cols = new Transform[] { Left, Middle, Right };
		counts = new int[] { count, 0, 0 };
		posStacks = Enumerable.Range(0, 3)
			.Select(i => new Stack<Vector3>( new[] { cols[i].localPosition } ))
			.ToArray();
		boxStacks = Enumerable.Range(0, 3)
			.Select(i => new Stack<BoxCollider2D>())
			.ToArray();
		blockMovesRemaining = new int[count];

		Height = TotalHeight / count;
		dWidth = (MaxWidth - MinWidth) / (count - 1);

		for (int i = count - 1; i >= 0; i--)
		{
			float k = (float)i / (count - 1);

			var go = new GameObject(i + "");
			go.transform.parent = Blocks;
			go.transform.localPosition = new Vector3(Left.localPosition.x, (count - 1 - i + 0.5f) * Height);
			//go.transform.localScale = new Vector3(Mathf.Lerp(MinWidth, MaxWidth, k), Height);

			var box = go.AddComponent<BoxCollider2D>();
			box.size = new Vector2(Mathf.Lerp(MinWidth, MaxWidth, k), Height);

            {
				var goOuter = new GameObject("Outer");
				goOuter.transform.parent = go.transform;
				goOuter.transform.localPosition = Vector3.zero;

				var sr = goOuter.AddComponent<SpriteRenderer>();
				sr.sprite = sprite;
				if (material != null)
					sr.material = material;
				sr.drawMode = spriteDrawMode;
				sr.size = box.size;
				sr.color = Color.HSVToRGB(Mathf.Lerp(MinHue, MaxHue, k), Saturation, OuterBrightness);
				sr.sortingOrder = i * 3;
            }

			if (ShowInnerSprite)
            {
				var goInner = new GameObject("Inner");
				goInner.transform.parent = go.transform;
				goInner.transform.localPosition = Vector3.zero;

				//goInner.transform.localScale = new Vector3(
				//	(box.size.x - 2 * InnerSpriteMargin.x) * InnerSpriteScaling.x,
				//	(box.size.y - 2 * InnerSpriteMargin.y) * InnerSpriteScaling.y,
				//	1);

				var sr2 = goInner.AddComponent<SpriteRenderer>();
				sr2.sprite = sprite;
				if (material != null)
					sr2.material = material;
				sr2.drawMode = spriteDrawMode;
				sr2.size = new Vector2(
					(box.size.x - 2 * InnerSpriteMargin.x) * InnerSpriteScaling.x,
					(box.size.y - 2 * InnerSpriteMargin.y) * InnerSpriteScaling.y
				);
				sr2.color = Color.HSVToRGB(Mathf.Lerp(MinHue, MaxHue, k), Saturation, InnerBrightness);
				sr2.sortingOrder = i * 3 + 1;
            }


			posStacks[LeftIndex].Push(go.transform.localPosition);
			boxStacks[LeftIndex].Push(box);
			blockMovesRemaining[i] = 1 << (count - 1 - i);
		}

		if (CreateExtraBottomBlocks)
		{
			var go = Blocks.Find(count - 1 + "");
			var copyMiddle = Instantiate(go, Blocks);
			copyMiddle.transform.localPosition = new Vector3(Middle.localPosition.x, 0.5f * Height);
			var copyRight = Instantiate(go, Blocks);
			copyRight.transform.localPosition = new Vector3(Right.localPosition.x, 0.5f * Height);
		}
	}

	void Start()
	{
		Recreate();

		moveIndex = 0;
		UpdateUI();
		StartCoroutine(Animate(count, LeftIndex, RightIndex, MiddleIndex));
	}

	void Update()
	{
		if (ShowBarrier)
		{
			Debug.DrawLine(
				Barrier.position - Vector3.right * BarrierDebugWidth,
				Barrier.position + Vector3.right * BarrierDebugWidth,
				!UseBarrier ? Color.red :
				BarrierActivated ? Color.green :
				Color.yellow);
		}
	}
	void UpdateUI()
    {
		MoveCounterLabel.text = CountPrefix + moveIndex + CountSuffix;
	}
	public bool BarrierActivated
    {
		// Don't use barrier unless Middle tower is high enough
		get { return counts[MiddleIndex] * 2 >= count; }
    }

	IEnumerator Animate(int n, int from, int to, int temp)
	{
		if (n == 0)
			yield break;

		IEnumerator before = Animate(n - 1, from, temp, to);
		while (before.MoveNext())
			yield return before.Current;

		Transform block = Blocks.Find(n - 1 + "");
		BoxCollider2D blockBox = boxStacks[from].Peek();

		var cur = block.localPosition; // posFrom
		float posToX;
		if (boxStacks[to].Count == 0 || blockMovesRemaining[n - 1] <= 1) // No random shifts for the last move
			posToX = cols[to].localPosition.x;
		else
		{
			var under = boxStacks[to].Peek();
			float dw = under.size.x - blockBox.size.x;
			posToX = under.transform.localPosition.x
				     + RandomShiftScale * Random.Range(-1f, 1f) * dw / 2;
		}
		var posTo = new Vector3(
			posToX,
			// posStacks[to].Peek().x + RandomShiftScale * Random.Range(-1f, 1f) * dWidth / 2,
			// cols[to].localPosition.x, - simple method without random shifts
			(counts[to] + 0.5f) * Height);

		if (moveIndex < MovesToSkip)
		{
			block.localPosition = posTo;
		}
		else
		{
			//var intermediate = (cur + posTo) / 2 + Vector3.up * 4; // new Vector3((cur.x + posTo.x) / 2, 6);

			float topVisibleY = camera.orthographicSize + camera.transform.position.y - Height / 2;

			float maxY = Mathf.Max(cur.y, posTo.y);
			float extraMiddleY = 0;
			if (temp == MiddleIndex)
				extraMiddleY = Mathf.Max(0, (counts[temp] + 0.5f) * Height - maxY);

			//Func<float, Vector3> p = t => Lerp2(cur, intermediate, posTo, t);
			/*
			Func<float, Vector3> p = t => Lerp3(
				cur,
				cur + Vector3.up * (BaseLiftLength + LiftLengthK * (maxY - cur.y)),
				posTo + Vector3.up * (BaseLiftLength + LiftLengthK * (maxY - posTo.y)),
				posTo, t
			);
			*/
			System.Func<float, Vector3> p = t => Lerp3(
				cur,
				cur + Vector3.up * (BaseLiftLength + TopPullK * (topVisibleY - cur.y) + UpperBlockPullK * (maxY - cur.y) + ExtraMiddlePullK * extraMiddleY),
				posTo + Vector3.up * (BaseLiftLength + TopPullK * (topVisibleY - posTo.y) + UpperBlockPullK * (maxY - posTo.y) + ExtraMiddlePullK * extraMiddleY),
				posTo, t
			);
			System.Func<float, float> barrierF = y =>
				y >= 0
					? y
					: (Mathf.Exp(y / BarrierK) - 1) * BarrierK;
			System.Func<Vector3, Vector3> barrierTransform = vec =>
				new Vector3(
					vec.x,
					Barrier.localPosition.y - barrierF(Barrier.localPosition.y - vec.y)
				);

			/*
			var dir = (posTo - cur).normalized;

			while (cur != posTo)
			{
				float maxMove = MovementSpeed * Time.deltaTime;
				if ((posTo - cur).magnitude <= maxMove)
					cur = posTo;
				else
					cur += dir * MovementSpeed * Time.deltaTime;

				block.localPosition = cur;
				yield return null;
			}
			*/

			var spline = Enumerable.Range(0, SplinePoints + 1).Select(i => p((float)i / SplinePoints)).ToArray();
			float splineLength = Enumerable.Range(0, SplinePoints).Select(i => (spline[i + 1] - spline[i]).magnitude).Sum();

			var splineBarriered = spline.Select(barrierTransform).ToArray();

			/*
			Vector3 previousPoint = p(0);
			for (int i = 1; i <= SplinePoints; i++)
			{
				Vector3 curPoint = p((float)i / SplinePoints);
				splineLength += (curPoint - previousPoint).magnitude;
				previousPoint = curPoint;
			}
			*/

			float movementTime = splineLength / MovementSpeed;
			movementTime = MovementTimeMultiplier * Mathf.Pow(movementTime, MovementTimePower);

			float rate = 0;
			while (rate < 1)
			{
				rate = Mathf.Min(rate + Time.deltaTime / movementTime, 1);
				block.localPosition = p(rate);
				if (UseBarrier && BarrierActivated)
					block.localPosition = barrierTransform(block.localPosition);
			
				if (ShowSpline)
				{
					DrawDebugCurve(splineBarriered.Select(p => p + transform.position).ToArray(), Color.cyan);
					DrawDebugCurve(spline.Select(p => p + transform.position).ToArray(), Color.red);
				}
				yield return null;
			}
			yield return new WaitForSeconds(MoveDelay);

			/*
			for (int i = 1; i <= SplinePoints; i++)
			{
				block.localPosition = p((float)i / SplinePoints);
				yield return null;
			}
			*/
		}

		counts[from]--;
		counts[to]++;
		posStacks[from].Pop();
		posStacks[to].Push(block.localPosition);
		boxStacks[from].Pop();
		boxStacks[to].Push(blockBox);
		blockMovesRemaining[n - 1]--;
		moveIndex++;

		UpdateUI();

		IEnumerator after = Animate(n - 1, temp, to, from);
		while (after.MoveNext())
			yield return after.Current;

		//foreach (var o in Animate(n - 1, from, temp, to))
		//    yield return o;
	}

	Vector3 Lerp2(Vector3 a, Vector3 b, Vector3 c, float t)
	{
		return Vector3.Lerp(
			Vector3.Lerp(a, b, t),
			Vector3.Lerp(b, c, t),
			t);
	}
	Vector3 Lerp3(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
	{
		return Vector3.Lerp(
			Lerp2(a, b, c, t),
			Lerp2(b, c, d, t),
			t);
	}

	void DrawDebugCurve(Vector3[] curve, Color color)
	{
		for (int i = 0; i + 1 < curve.Length; i++)
			Debug.DrawLine(curve[i], curve[i + 1], color);
	}
}

[CustomEditor(typeof(Manager))]
public class ManagerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var man = (Manager)target;

		EditorGUI.BeginChangeCheck();
		base.OnInspectorGUI();

		EditorGUILayout.BeginHorizontal();
		bool pressedRecreate = GUILayout.Button("Recreate");
		if (GUILayout.Button("-"))
		{
			//man.StackSize--;
			man.Recreate();
		}
		if (GUILayout.Button("+"))
		{
			//man.StackSize++;
			man.Recreate();
		}
		EditorGUILayout.EndHorizontal();

		if (EditorGUI.EndChangeCheck() || pressedRecreate)
		{
			man.Recreate();
		}
	}
}

[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true)]
public class ReadOnlyAttribute : PropertyAttribute { }

[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyAttributeDrawer : UnityEditor.PropertyDrawer
{
	public override void OnGUI(Rect rect, UnityEditor.SerializedProperty prop, GUIContent label)
	{
		bool wasEnabled = GUI.enabled;
		GUI.enabled = false;
		UnityEditor.EditorGUI.PropertyField(rect, prop);
		GUI.enabled = wasEnabled;
	}
}
