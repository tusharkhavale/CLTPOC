using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {

	public static GameController gameController;
	public UIManager uiManager;
	public PlayerPrefsManager ppManager;
	public AnalyticsManager analyticsManager;
	// Assign in inspector
	public AudioManager audioManager;
	public AudioDSP audioDSP;

	void Awake()
	{
		gameController = this;
	}

	// Audio related functions
	public void StartMicrophoneRecording()
	{
		audioManager.StartMicrophone ();
	}

	public void EndMicrophoneRecording()
	{
		audioManager.StopMicrophone ();
	}

	public void StartPictDetection()
	{
		audioDSP.InitializePitchTracker ();
	}

	public void EndPitchDetection()
	{
		audioDSP.EndPitchTracking ();
	}

	// UI related functions
	public void ScreenTransition(EScreen screen)
	{
		uiManager.UIScreenTransition (screen);
	}

}
