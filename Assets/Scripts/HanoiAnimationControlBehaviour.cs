using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class HanoiAnimationControlBehaviour : PlayableBehaviour
{
	public System.Action<double> SetRate;
	public float FrameRate;

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
	public Transform Barrier;

	[Header("UI")]
	public string CountPrefix;
	public string CountSuffix;

	[Header("References")]
	public Camera camera;
	public Transform Left, Middle, Right;
	public Transform Blocks;
	public TextMeshPro MoveCounterLabel;

	[Header("Debug")]
	public int MovesToSkip = 0;

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		double frameTime = 1 / FrameRate;
		double lastFrameTime = playable.GetDuration() - frameTime;
		double rate = playable.GetTime() / lastFrameTime;

		Debug.Log("Process Frame");
		Debug.Log($"time: {playable.GetTime()}, duration: {playable.GetDuration()}, rate: {rate}");
		SetRate(rate);
	}

    public override void PrepareData(Playable playable, FrameData info)
    {
		//Debug.Log("Prepare data");
        //base.PrepareData(playable, info);
    }

    public override void PrepareFrame(Playable playable, FrameData info)
    {
		//Debug.Log("Prepare Frame");
        //base.PrepareFrame(playable, info);
    }
    public override void OnPlayableCreate(Playable playable)
    {
		Debug.Log("Behaviour.OnPlayableCreate");
        //base.OnPlayableCreate(playable);
    }
}

