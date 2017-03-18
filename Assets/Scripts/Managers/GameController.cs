using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {

	public static GameController gameController;
	// Assign in inspector
	public UIManager uiManager;
	public PlayerPrefsManager ppManager;
	public AnalyticsManager analyticsManager;
	public AudioManager audioManager;
	public AudioDSP audioDSP;

	void Awake()
	{
		gameController = this;
	}


	// Audio Manager functions
	public void StartMicrophoneRecording()
	{
		audioManager.StartMicrophone ();
	}

	public void EndMicrophoneRecording()
	{
		audioManager.StopMicrophone ();
	}

	public void SetAudioDataTrigger(bool value)
	{
		audioManager.SetAudioDataTrigger (value);
	}

	public void SetSpectrumDataTrigger(bool value)
	{
		audioManager.SetSpectrumDataTrigger (value);
	}


	// AudioDSP functions
	public void StartPictDetection()
	{
		audioDSP.InitializePitchTracker ();
	}

	public void EndPitchDetection()
	{
		audioDSP.EndPitchTracking ();
	}

	public void StartHarmonicsCalculation()
	{
		audioDSP.StartHarmonicsCalculation ();
	}

	public void EndHarmonicsCalculation()
	{
		audioDSP.EndHarmonicsCalculation();
	}

	public void StartNoiseCeilingCalibration()
	{
		audioDSP.StartNoiseCeilingCalibration ();
	}


	// UI related functions
	public void ScreenTransition(EScreen screen)
	{
		uiManager.UIScreenTransition (screen);
	}

}
