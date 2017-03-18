using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Temporary class
public class AnalyticsManager : MonoBehaviour {

	static float cummulativeTime;
	static float currSessionTime;
	static int totalSessions;

	static float cummulativeAudioInputTime;
	static int totalInputs;

	static float recordingTime;
	static float silenceTime;
	static float audioInputTime;
	public static bool audioInputAvalable;
	static float recordingstartedTime;



	/// <summary>
	/// Load logs from prefs.
	/// Subscribe to events
	/// </summary>
	void Start()
	{
		LoadAudioInputLog ();
		AddDelegates ();
	}

	//Subscribe to events
	private void AddDelegates()
	{
		GameController.gameController.audioManager.AddRecordingStartedDelegate (this.OnRecordingStarted);
		GameController.gameController.audioManager.AddRecordingEndedDelegate(this.OnRecordingEnded);
		GameController.gameController.audioDSP.AddInputSoundDetectedDelegate (this.OnInputSoundDetected);
	}

	/// <summary>
	/// Raises the application quit event.
	/// </summary>
	void OnApplicationQuit()
	{
		SaveSessionLog ();
	}

	/// <summary>
	/// Saves the session log on exit.
	/// </summary>
	private void SaveSessionLog()
	{
		cummulativeTime = PlayerPrefs.GetFloat ("cummulativeTime") + Time.time;
		PlayerPrefs.SetFloat ("cummulativeTime", cummulativeTime);

		totalSessions = PlayerPrefs.GetInt ("totalSessions") + 1;
		PlayerPrefs.SetInt ("totalSessions", totalSessions);

		PlayerPrefs.SetFloat ("cummulativeAudioInputTime",cummulativeAudioInputTime);
		PlayerPrefs.SetInt("totalInputs",totalInputs);
	}

	/// <summary>
	/// Loads the audio input log from prefs.
	/// </summary>
	private void LoadAudioInputLog()
	{
		cummulativeAudioInputTime = PlayerPrefs.GetFloat ("cummulativeAudioInputTime");
		totalInputs = PlayerPrefs.GetInt("totalInputs");
	}

	/// <summary>
	/// Gets the session log.
	/// </summary>
	/// <returns>The session log.</returns>
	public string GetSessionLog()
	{
		float avgSessionTime = PlayerPrefs.GetFloat ("cummulativeTime") / PlayerPrefs.GetInt ("totalSessions");
		float avgInputDelay = (recordingTime-cummulativeAudioInputTime)/ totalInputs;

		string log = "Average Session Time : " + avgSessionTime + "s\n"
		             + " Total Sessions : " + PlayerPrefs.GetInt ("totalSessions") + "\n"
		             + " Average Input Delay : " + avgInputDelay;
					 
		
		return log;
	}

	/// <summary>
	/// Raises the recording started event.
	/// Set recording started time
	/// </summary>
	private void OnRecordingStarted()
	{
		recordingstartedTime = Time.time;
		silenceTime = Time.time;
	}

	/// <summary>
	/// Raises the recording ended event.
	/// Calculates recording time
	/// </summary>
	private void OnRecordingEnded()
	{
		recordingTime += Time.time - recordingstartedTime;
	}

	/// <summary>
	/// Raises the input sound detected event.
	/// </summary>
	private void OnInputSoundDetected()
	{
		cummulativeAudioInputTime += Time.deltaTime;
		if (silenceTime < Time.time - 0.1f) 
		{
			totalInputs++;
		}
		silenceTime = Time.time;
	}


}
