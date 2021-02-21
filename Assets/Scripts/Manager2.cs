//using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

public class Manager2 : MonoBehaviour
{
	[Header("Tower Dimensions")]
	[Range(2, 20)]
	public int count;
	public float MinWidth, MaxWidth;
	[Range(2, 20)]
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

	[Header("References")]
	public Transform Blocks;
	public Transform Left, Middle, Right;

	public bool ShowSpline = false;
	public bool ShowBarrier = false;
	public float BarrierDebugWidth = 1;
	public bool CreateExtraBottomBlocks = false;

	[Header("Computed")]
	[SerializeField, ReadOnly]
	public float Height;
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

}

[CustomEditor(typeof(Manager2))]
public class Manager2Editor : Editor
{
	public override void OnInspectorGUI()
	{
		var man = (Manager2)target;

		EditorGUI.BeginChangeCheck();
		base.OnInspectorGUI();

		bool pressedRecreate = GUILayout.Button("Recreate");
		if (EditorGUI.EndChangeCheck() || pressedRecreate)
		{
			man.Recreate();
		}
	}
}
