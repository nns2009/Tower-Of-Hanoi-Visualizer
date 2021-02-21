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
	public float TopPullK, UpperBlockPullK;
	[Range(0, 3)]
	public float ExtraMiddlePullK;

	[Header("Animation")]
	public float StartDelay = 0;
	public float RandomShiftScale;
	public float MovementSpeed;
	public float MovementTimeMultiplier, MovementTimePower;
	public float MoveDelay;

	[Header("Barrier")]
	public bool UseBarrier;
	public float BarrierK;
	public ExposedReference<Transform> Barrier;

	[Header("UI")]
	public string CountPrefix;
	public string CountSuffix;

	[Header("References")]
	public ExposedReference<Camera> camera;
	public ExposedReference<Transform> Left, Middle, Right;
	public ExposedReference<Transform> exBlocks;
	public ExposedReference<TextMeshPro> MoveCounterLabel;
	public ExposedReference<Manager2> exManager;

	[ReadOnly]
	public Transform Blocks;
	[ReadOnly]
	public Manager2 manager;

	[Header("Debug")]
	public int MovesToSkip = 0;
	public int MoveI = 0;

	[Header("Computed")]
	private Transform[] cols;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		var resolver = graph.GetResolver();

		Debug.Log("Asset.CreatePlayable Before");

		var playable = ScriptPlayable<HanoiAnimationControlBehaviour>.Create(graph);

		var b = playable.GetBehaviour();
		b.FrameRate = 60;
		b.SetRate = SetRate;
		//lightControlBehaviour.light = light.Resolve(graph.GetResolver());
		//lightControlBehaviour.color = color;
		//lightControlBehaviour.intensity = intensity;

		Blocks = exBlocks.Resolve(resolver);
		manager = exManager.Resolve(resolver);
		cols = new Transform[] { Left.Resolve(resolver), Middle.Resolve(resolver), Right.Resolve(resolver) };
		SetMove(MoveI);

		Debug.Log("Asset.CreatePlayable After");

		return playable;
	}

	const int tn = 3;

	void SetRate(double t)
    {
		int n = Blocks.childCount;
		int count = MoveCount(n);
		MoveI = (int)System.Math.Floor(count * t);
		SetMove(MoveI);
	}

	void SetMove(int index)
    {
		int n = Blocks.childCount;
		var towers = BuildTowersAtMove(n, index);

		for (int ti = 0; ti < tn; ti++)
        {
			for (int j = 0; j < towers[ti].Count; j++)
            {
				int blockIndex = towers[ti][j];
				var block = Blocks.Find(blockIndex + "");
				block.localPosition = cols[ti].localPosition + Vector3.up * (j + 0.5f) * manager.Height;
            }
        }
    }

	int MoveCount(int n)
    {
		int count = 0;
		for (int i = 1; i <= n; i++)
			count = 2 * count + 1;
		return count;
	}

	List<int>[] BuildTowersAtMove(int n, int index)
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
		int from = 0, temp = 1, to = 2;
		for (int stack = n; stack >= 1; stack--)
        {
			if (index < stepi)
            {
				(temp, to) = (to, temp);
            }
			else
            {
				towers[to].AddRange(towers[from].Pop(stack));
				index -= stepi;
				if (index > 0)
                {
					towers[temp].Add(towers[from].Pop());
					index--;
                }
				// Initially forgot to update (from, to, temp) here
				// (from, to, temp) = (to, temp, from); - Wrong as well
				(from, to) = (to, from);
            }
			int nextStepi = (stepi - 1) / 2; // Equivalent to just: step / 2
			stepi = nextStepi;
        }

		return towers;
    }
}
