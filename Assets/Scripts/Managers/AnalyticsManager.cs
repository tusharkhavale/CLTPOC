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


	private static AnalyticsManager instance;
	public static AnalyticsManager GetInstance()
	{
		if (instance != null) 
		{
			return instance;
		}
		return null;
	}

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		GetAudioInputLog ();
	}

	void OnApplicationQuit()
	{
		SaveSessionLog ();
	}


	void SaveSessionLog()
	{
		cummulativeTime = PlayerPrefs.GetFloat ("cummulativeTime") + Time.time;
		PlayerPrefs.SetFloat ("cummulativeTime", cummulativeTime);

		totalSessions = PlayerPrefs.GetInt ("totalSessions") + 1;
		PlayerPrefs.SetInt ("totalSessions", totalSessions);

		PlayerPrefs.SetFloat ("cummulativeAudioInputTime",cummulativeAudioInputTime);
		PlayerPrefs.SetInt("totalInputs",totalInputs);
	}

	void GetAudioInputLog()
	{
		cummulativeAudioInputTime = PlayerPrefs.GetFloat ("cummulativeAudioInputTime");
		totalInputs = PlayerPrefs.GetInt("totalInputs");
	}

	public string GetSessionLog()
	{
		float avgSessionTime = PlayerPrefs.GetFloat ("cummulativeTime") / PlayerPrefs.GetInt ("totalSessions");
		float avgInputDelay = cummulativeAudioInputTime/ totalInputs;

		string log = "Average Session Time : " + avgSessionTime + "s\n"
		             + " Total Sessions : " + PlayerPrefs.GetInt ("totalSessions") + "\n"
		             + " Average Input Delay : " + avgInputDelay;
					 
		
		return log;
	}

	public void SetStartRecordingTime()
	{
		recordingstartedTime = Time.time;
		silenceTime = Time.time;
	}

	public void EndRecordingTime()
	{
		recordingTime = Time.time - recordingstartedTime;
	}

	public void AudioInputDetected()
	{
		if (audioInputAvalable)
			return;
		
		cummulativeAudioInputTime = Time.time - silenceTime;
		totalInputs++;

		StartCoroutine (StartAudioInputTimer ());
		audioInputAvalable = true;
	}

	IEnumerator StartAudioInputTimer()
	{
		while (audioInputAvalable) 
		{
			yield return new WaitForSeconds (2.0f);	
		}

		audioInputAvalable = false;
		silenceTime = Time.time;
	}













}
