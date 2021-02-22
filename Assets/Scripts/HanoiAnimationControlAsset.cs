using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class HanoiAnimationControlAsset : PlayableAsset
{
	[Header("Path")]
	public int SplinePoints;
	public float BaseLiftLength;
	[Range(0, 1)]
	public float UpperBlockPullK;
	[Range(0, 3)]
	public float ExtraMiddlePullK;

	[Header("Animation")]
	//public float StartDelay = 0;
	public float RandomShiftScale;
	public float MovementSpeed;
	public float MovementTimeMultiplier, MovementTimePower;

	[Range(0, 1)]
	public float DelayFraction;

	[Header("Barrier")]
	public bool UseBarrier;
	public float BarrierK;
	public ExposedReference<Transform> Barrier;

	[Header("UI")]
	public string CountPrefix;
	public string CountSuffix;

	[Header("References")]
	//public ExposedReference<Camera> camera;
	public ExposedReference<Manager2> exManager;
	public ExposedReference<Transform> Left, Middle, Right;
	public ExposedReference<Transform> exBlocks;
	public ExposedReference<TextMeshPro> exMoveCounterLabel;

	[ReadOnly]
	public Transform Blocks;
	[ReadOnly]
	public TextMeshPro MoveCounterLabel;
	[ReadOnly]
	public Manager2 manager;

	[Header("Debug")]
	public int MovesToSkip = 0;
	public double MoveI = 0;

	[Header("Computed")]
	private Transform[] cols;

	const int LeftIndex = 0, MiddleIndex = 1, RightIndex = 2;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		Debug.Log("Asset.CreatePlayable Before");

		var playable = ScriptPlayable<HanoiAnimationControlBehaviour>.Create(graph);
		var b = playable.GetBehaviour();
		b.FrameRate = 60;
		b.SetRate = SetRate;

		var resolver = graph.GetResolver();
		Blocks = exBlocks.Resolve(resolver);
		MoveCounterLabel = exMoveCounterLabel.Resolve(resolver);
		manager = exManager.Resolve(resolver);
		cols = new Transform[] { Left.Resolve(resolver), Middle.Resolve(resolver), Right.Resolve(resolver) };
		//SetMove(MoveI);

		Debug.Log("Asset.CreatePlayable After");

		return playable;
	}

	const int tn = 3;

	void SetRate(double t)
    {
		int n = Blocks.childCount;
		int moveCount = MoveCount(n);
		int delayCount = moveCount - 1;

		double animationMoveLen = moveCount - DelayFraction;
		
		MoveI = DelayFraction + animationMoveLen * t; //(int)System.Math.Floor(count * t);

		int baseMoveIndex = (int)System.Math.Floor(MoveI);
		double inPosMove = MoveI % 1;
		if (inPosMove < DelayFraction)
			inPosMove = 0;
		else
			inPosMove = (inPosMove - DelayFraction) / (1 - DelayFraction);

		SetMove(n, baseMoveIndex, inPosMove);
	}

	void SetMove(int n, int baseMoveIndex, double inMovePos)
    {
		// bi - Block Index
		var (towers, bi, from, to) = BuildTowersAtMove(n, baseMoveIndex);

		float Height = manager.Height;
		for (int ti = 0; ti < tn; ti++)
        {
			for (int j = 0; j < towers[ti].Count; j++)
            {
				int blockIndex = towers[ti][j];
				var block = Blocks.Find(blockIndex + "");
				block.localPosition = cols[ti].localPosition + Vector3.up * (j + 0.5f) * Height;
            }
        }

        if (baseMoveIndex < MoveCount(n)) {
			// Animation of the move

			Transform block = Blocks.Find(bi + "");
			BoxCollider2D blockBox = block.GetComponent<BoxCollider2D>();

			var posFrom = block.localPosition;
			var posTo = new Vector3(cols[to].localPosition.x, (towers[to].Count + 0.5f) * Height);
			float maxY = Mathf.Max(posFrom.y, posTo.y);
			float extraMiddleY = 0;
			if (from != MiddleIndex && to != MiddleIndex)
				extraMiddleY = Mathf.Max(0, (towers[MiddleIndex].Count + 0.5f) * Height - maxY);

			System.Func<float, Vector3> p = t => Vec.Lerp3(
				posFrom,
				posFrom + Vector3.up * (BaseLiftLength + UpperBlockPullK * (maxY - posFrom.y) + ExtraMiddlePullK * extraMiddleY),
				posTo + Vector3.up * (BaseLiftLength + UpperBlockPullK * (maxY - posTo.y) + ExtraMiddlePullK * extraMiddleY),
				posTo,
				t
			);

			block.localPosition = p((float)inMovePos);
			UpdateUI(baseMoveIndex);
        }
    }

	void UpdateUI(int moveIndex)
    {
		MoveCounterLabel.text = CountPrefix + moveIndex + CountSuffix;
    }

	int MoveCount(int n)
    {
		int count = 0;
		for (int i = 1; i <= n; i++)
			count = 2 * count + 1;
		return count;
	}

	// Returns (Tower State, nextBlockToMove, nextMoveFrom, nextMoveTo)
	(List<int>[], int, int, int) BuildTowersAtMove(int n, int index)
    {
		List<int>[] towers = new List<int>[]
		{
			Enumerable.Range(0, n).Reverse().ToList(),
			new List<int>(),
			new List<int>(),
		};

		int stepi = 0;
		for (int i = 1; i <= n; i++)
			stepi = 2 * stepi + 1;

		// Moving stacks of blocks of sizes from 'n' down to '1'
		// Each iteration solves top 'stack' blocks of 'stack+1'-block puzzle
		int from = LeftIndex, temp = MiddleIndex, to = RightIndex;
		for (int stack = n; stack >= 0; stack--)
        {
			if (index < stepi)
            {
				(temp, to) = (to, temp);
            }
			else
            {
				towers[to].AddRange(towers[from].Pop(stack));
				index -= stepi;

				if (index == 0)
                {
					return (towers, stack, from, temp);
                }

				towers[temp].Add(towers[from].Pop());
				index--;
				// Initially forgot to update (from, to, temp) here
				// (from, to, temp) = (to, temp, from); - Wrong as well
				(from, to) = (to, from);
            }
			stepi = (stepi - 1) / 2; // Equivalent to just: step / 2
        }

		throw new System.Exception("Something wrong at BuildTowersAtMove");
    }
}
