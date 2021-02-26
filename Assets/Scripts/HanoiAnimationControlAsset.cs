using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using Curve = System.Func<float, UnityEngine.Vector3>;


public class HanoiAnimationControlAsset : PlayableAsset
{
	[Header("Path")]
	[Range(3, 50)]
	public int InterpolationSegments;
	public float BaseLiftLength;
	[Range(0, 1)]
	public float UpperBlockPullK;
	[Range(0, 3)]
	public float ExtraMiddlePullK, ExtraAverageMiddlePullK;

	[Header("Animation")]
	public int Seed = 0;
	public float RandomShiftScale;
	[Range(0.2f, 2f)]
	public float LengthPower; // Equivalent to TimePower

	[Range(0, 1)]
	public float DelayFraction;

	public float FinalMoveTolerance;

	[Header("Barrier")]
	public bool UseBarrier;
	public float BarrierK;

	[Header("UI")]
	public string CountPrefix;
	public string CountSuffix;

	[Header("Sounds")]
	public bool useLandingSound = false;
	public AudioClip landingSound;
	public ExposedReference<AudioSource> exLandingAudioSource;
	public float landingSoundVolume = 1;

	[Header("References")]
	public ExposedReference<Manager2> manager;

	[Header("Debug")]
	public bool ShowSpline = false;
	public bool ShowBarrier = false;
	public float BarrierDebugWidth = 1;

	[Header("Debug Output")]
	[ReadOnly]
	public double Rate;
	[ReadOnly]
	public int BaseMove;
	[ReadOnly]
	public double InMove;
	[ReadOnly]
	public float CurveTimeRatio;

	[ReadOnly]
	private double MoveI = 0;


	[Header("Computed")]
	private AudioSource landingAudioSource;
	private Manager2 trueManager;
	private Transform Barrier;
	private Transform Blocks;
	private TextMeshPro MoveCounterLabel;
	private Transform Anchors;
	private Transform Left, Middle, Right;
	private Transform[] cols;

	const int LeftIndex = 0, MiddleIndex = 1, RightIndex = 2;
	const int IndexSum = LeftIndex + MiddleIndex + RightIndex;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		var playable = ScriptPlayable<HanoiAnimationControlBehaviour>.Create(graph);

		var resolver = graph.GetResolver();
		landingAudioSource = exLandingAudioSource.Resolve(resolver);
		trueManager = manager.Resolve(resolver);

		Barrier = trueManager.transform.Find("Barrier");
		Blocks = trueManager.transform.Find("Blocks");
		MoveCounterLabel = trueManager.transform.Find("Move Counter").GetComponent<TextMeshPro>();

		Anchors = trueManager.transform.Find("Anchors");
		Left = Anchors.Find("Left");
		Middle = Anchors.Find("Middle");
		Right = Anchors.Find("Right");
		cols = new Transform[] { Left, Middle, Right };

		int n = Blocks.childCount;
		extend2s(n);
		int moveCount = MoveCount(n);
		int delayCount = moveCount - 1;

		float[] times = new float[moveCount];
		for (int i = 0; i < moveCount; i++)
        {
			// Get the curve from move=i to move=i+1
			var (_, p) = PlaceTowersAndGetCurve(n, i, false);

			// Get it's length
			float length = Curves.Length(p, InterpolationSegments); // Test length: Mathf.Log(i+2, 2);

			// Apply LengthPower to length to get appropriate ratio
			times[i] = Mathf.Pow(length, LengthPower);
        }
		float movementFraction = 1 - DelayFraction;
		float timesSum = times.Sum();
		float timesScaleK = movementFraction / timesSum;
		times = times.Select(time => time * timesScaleK).ToArray();
		Debug.Log("Recreating with timesSum: " + timesSum);

		float[] timesSums = new float[moveCount + 1];
		timesSums[0] = 0;
		for (int i = 0; i < moveCount; i++)
			timesSums[i + 1] = timesSums[i] + times[i];

		float oneDelayFraction = DelayFraction / delayCount;
		int lastBaseMoveIndex = 0;
		void setRate(double t)
        {
			Rate = t; // Debug output

			int l = 0;
			int r = moveCount + 1;
			while (l + 1 < r)
            {
				int m = (l + r) / 2;
				float progress = timesSums[m] // time spent in movement
					+ (m - 1) * oneDelayFraction; // time spent in delays,
					//'m' is never =0 during binary search

				if (progress <= t)
					l = m;
				else
					r = m;
            }

			int baseMoveIndex;
			double inMovePos;
			if (l == 0)
            {
				baseMoveIndex = 0;
				inMovePos = t / times[0];
            }
            else
            {
				baseMoveIndex = l;
				double rem = t - (timesSums[baseMoveIndex] + (baseMoveIndex - 1) * oneDelayFraction);
				if (rem < oneDelayFraction)
					inMovePos = 0;
				else
					inMovePos = (rem - oneDelayFraction) / times[baseMoveIndex];

				// The following condition is necessary because of precision errors
				// The error is small so the animation looks fine anyway,
				// but without this condition, the final Move Count is one less than it should be
				if (baseMoveIndex + 1 == moveCount && inMovePos + FinalMoveTolerance >= 1)
                {
					baseMoveIndex = moveCount;
					inMovePos = 0;
                }
            }
			CurveTimeRatio = // Debug output
				baseMoveIndex < moveCount
					? times[baseMoveIndex] / timesScaleK
					: -1;

			SetMove(n, baseMoveIndex, inMovePos);
			if (useLandingSound && baseMoveIndex == lastBaseMoveIndex + 1)
            {
				landingAudioSource.Play();
				//AudioSource.PlayClipAtPoint(landingSound, Camera.main.transform.position, landingSoundVolume);
            }
			lastBaseMoveIndex = baseMoveIndex;
        }

		var b = playable.GetBehaviour();
		b.FrameRate = 60;
		b.SetRate = setRate;

		// Necessary to make sure animation starts from the initial state
		// and not from the SetMove(n, MoveCount(n)-1, 0) state
		// (which is the last state evaluated in the times-calculation loop)
		SetMove(n, 0, 0);

		return playable;
	}

	const int tn = 3;

	List<int> p2 = new List<int> { 1 };
	//List<int> l2 = new List<int>();

	void extend2s(int n)
    {
		while (p2.Count <= n)
			p2.Add(p2.Last() * 2);
    }

	void SetRate(double t)
    {
		Debug.LogWarning("Old setRate!");
		int n = Blocks.childCount;
		extend2s(n);
		int moveCount = MoveCount(n);

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
		BaseMove = baseMoveIndex; // Debug output
		InMove = inMovePos; // Debug output

		UpdateUI(baseMoveIndex);

		var (block, p) = PlaceTowersAndGetCurve(n, baseMoveIndex, true);

		if (baseMoveIndex < MoveCount(n))
		{
			// Animation of the move
			//Transform block = Blocks.Find(bi + "");
			block.localPosition = p((float)inMovePos);
		}
	}

	(Transform, Curve) PlaceTowersAndGetCurve(int n, int baseMoveIndex, bool drawDebug)
    {
		var previousRandomState = Random.state;

		// bi - Block Index
		var (towers, bi, from, to) = BuildTowersAtMove(n, baseMoveIndex);
		int temp = LeftIndex + MiddleIndex + RightIndex - from - to;

		float Height = trueManager.Height;

		// Function to get block position
		// Mostly necessary because of random shifts
		Vector3 getBlockPosition(Transform block, int blockIndex, int towerX, int towerY, int moveIndex)
        {
			// moveIndex parameter - is total number of moves made by all blocks = position identificator
			// Next three lines are characteristics of the specific block
			int movesEvery = p2[blockIndex + 1];
			int movesMade = (moveIndex + p2[blockIndex]) / movesEvery;
			int movesTotal = p2[n - 1 - blockIndex];

			float blockX;
			if (towerY == 0 || movesMade == 0 || movesMade == movesTotal)
            {
				blockX = cols[towerX].localPosition.x;
            }
            else
            {
				var under = Blocks.Find(towers[towerX][towerY - 1] + "");
				var underBox = under.GetComponent<BoxCollider2D>();
				var blockBox = block.GetComponent<BoxCollider2D>();
				float dw = underBox.size.x - blockBox.size.x;

				Random.InitState(hash(blockIndex, movesMade));
				blockX = under.localPosition.x
					+ RandomShiftScale * Random.Range(-1f, 1f) * dw / 2;
            }

			return new Vector3(blockX, (towerY + 0.5f) * Height);
        }

		for (int ti = 0; ti < tn; ti++)
        {
			for (int j = 0; j < towers[ti].Count; j++)
            {
				int blockIndex = towers[ti][j];
				var block = Blocks.Find(blockIndex + "");
				block.localPosition = getBlockPosition(block, blockIndex, ti, j, baseMoveIndex);
            }
        }


		if (baseMoveIndex >= MoveCount(n))
        {
			Random.state = previousRandomState;
			return (null, null);
        }
		else
		{
			Transform block = Blocks.Find(bi + "");
			BoxCollider2D blockBox = block.GetComponent<BoxCollider2D>();

			var posFrom = block.localPosition;
			var posTo = getBlockPosition(block, bi, to, towers[to].Count, baseMoveIndex + 1);
			// var posTo = new Vector3(cols[to].localPosition.x, (towers[to].Count + 0.5f) * Height);

			float maxY = Mathf.Max(posFrom.y, posTo.y);
			float averageY = (posFrom.y + posTo.y) / 2;
			float extraMiddleY = 0;
			// extraMiddleY is not enough when block moves high-to-low (or low-to-high)
			// and the middle tower is approximately as tall as the "high" one
			// that's why extraAboveAverageY was introduced
			float extraAboveAveragyY = 0;
			if (temp == MiddleIndex)
			{
				float middlePassY = (towers[MiddleIndex].Count + 0.5f) * Height;
				extraMiddleY = Mathf.Max(0, middlePassY - maxY);
				extraAboveAveragyY = Mathf.Max(0, middlePassY - averageY) - extraMiddleY;
			}

			float sharedExtraY = BaseLiftLength + ExtraMiddlePullK * extraMiddleY + ExtraAverageMiddlePullK * extraAboveAveragyY;
			Curve baseCurve = Curves.Cubic(
				posFrom,
				posFrom + Vector3.up * (sharedExtraY + UpperBlockPullK * (maxY - posFrom.y)),
				posTo + Vector3.up * (sharedExtraY + UpperBlockPullK * (maxY - posTo.y)),
				posTo
			);

            Curve p;

			bool barrierActivated = temp == MiddleIndex && extraMiddleY > 0;
			if (UseBarrier && barrierActivated)
            {
				System.Func<float, float> barrierF = y =>
					y >= 0
						? y
						: (Mathf.Exp(y / BarrierK) - 1) * BarrierK;
				System.Func<Vector3, Vector3> barrierTransform = vec =>
					new Vector3(
						vec.x,
						Barrier.localPosition.y - barrierF(Barrier.localPosition.y - vec.y)
					);

				//p = t => barrierTransform(p(t)); Doesn't capture 'p', executes recursively - not what we want
				p = t => barrierTransform(baseCurve(t));

				if (drawDebug && ShowSpline)
					Curves.DrawDebug(
						Curves.Translate(baseCurve, trueManager.transform.position),
						InterpolationSegments, Color.cyan);
            }
            else
            {
				p = baseCurve;
            }

			if (drawDebug && ShowSpline)
				Curves.DrawDebug(
					Curves.Translate(p, trueManager.transform.position),
					InterpolationSegments, Color.red);

			if (drawDebug && ShowBarrier)
			{
				Debug.DrawLine(
					Barrier.position - Vector3.right * BarrierDebugWidth,
					Barrier.position + Vector3.right * BarrierDebugWidth,
					!UseBarrier ? Color.red :
					barrierActivated ? Color.magenta :
					Color.green);
			}

			//Transform block = Blocks.Find(bi + "");
			//block.localPosition = p((float)inMovePos);
			//UpdateUI(baseMoveIndex);

			Random.state = previousRandomState;
			return (block, p);
		}
	}

	void UpdateUI(int moveIndex)
    {
		MoveCounterLabel.text = CountPrefix + moveIndex + CountSuffix;
    }

	int MoveCount(int n)
    {
		return p2[n] - 1;
	}

	int hash(int blockIndex, int movesMade)
	{
		return Seed
			+ blockIndex * 1000003 // Primes (at least: co-primes) - to minimize intersections
			+ movesMade * 1009;
	}

	// Returns (Tower State, nextBlockToMove, nextMoveFrom, nextMoveTo)
	(List<int>[], int, int, int) BuildTowersAtMove(int n, int moveIndex)
    {
		List<int>[] towers = new List<int>[]
		{
			new List<int>(),
			new List<int>(),
			new List<int>(),
		};

		int stepi = MoveCount(n);
		bool completed = moveIndex == stepi;

		// Moving stacks of blocks of sizes from 'n' down to '1'
		// Each iteration solves top 'stack'-block puzzle
		int index = moveIndex;
		int from = LeftIndex, temp = MiddleIndex, to = RightIndex;
		for (int stack = n; stack >= 1; stack--)
        {
			int nextStepi = (stepi - 1) / 2; // Equivalent to just: step / 2

			if (index < nextStepi + 1)
            {
				towers[from].Add(stack - 1);
				(temp, to) = (to, temp);
            }
            else
            {
				towers[to].Add(stack - 1);
				(temp, from) = (from, temp);
				index -= nextStepi + 1;
            }

			stepi = nextStepi;
        }

		if (completed)
			return (towers, -1, -1, -1);

		int oneIndex =
			towers[from].Count > 0 && towers[from].Last() == 0
				? from
				: to;
		bool oneToMove = moveIndex % 2 == 0;

		if (oneToMove)
        {
			int moveDirection =
				n % 2 == 0
					? 1
					: 2;
			return (towers, 0, oneIndex, (oneIndex + moveDirection) % tn);
        }
		else
        {
			int i1 = oneIndex == LeftIndex ? MiddleIndex : LeftIndex;
			int i2 = IndexSum - oneIndex - i1;
			int indexToMove =
				towers[i1].Count == 0 ? i2 :
				towers[i2].Count == 0 ? i1 :
				towers[i1].Last() < towers[i2].Last() ? i1
				: i2;

			return (towers, towers[indexToMove].Last(), indexToMove, i1 + i2 - indexToMove);
        }
    }
}
