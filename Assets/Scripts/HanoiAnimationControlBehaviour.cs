using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class HanoiAnimationControlBehaviour : PlayableBehaviour
{
	public System.Action<double> SetRate;
	public float FrameRate;

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		double frameTime = 1 / FrameRate;
		double lastFrameTime = playable.GetDuration() - frameTime;
		double rate = playable.GetTime() / lastFrameTime;

		//Debug.Log($"{info.frameId}");
		//Debug.Log($"Playable. GetPlayState(): {playable.GetPlayState()}, GetSpeed(): {playable.GetSpeed()}, GetTraversalMode(): {playable.GetTraversalMode()}");
		//Debug.Log($"FrameData. EffectivePlayState: {info.effectivePlayState}, effective speed: {info.effectiveSpeed}, evaluation type: {info.evaluationType}");

		//Debug.Log("Process Frame");
		//Debug.Log($"time: {playable.GetTime()}, duration: {playable.GetDuration()}, rate: {rate}");
		SetRate(rate);
	}
}

