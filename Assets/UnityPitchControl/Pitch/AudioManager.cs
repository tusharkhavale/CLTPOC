using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
	private AudioSource audioSource;
	private int audioBufferSize;
	private int spectrumSize;
	private int sampleRate;
	private float[] spectrumData;
	private float[] audioOutputData;
	private float[] rawAudioData;
	private bool getAudioData;
	private bool getSpectrumData;

	// Events and Delegates
	public delegate void SpectrumDataDelegate(float[] spectrumData);
	private event SpectrumDataDelegate spectrumDataEvent;

	public delegate void AudioOutputDataDelegate(float[] outputData);
	private event AudioOutputDataDelegate outputDataEvent;

	public delegate void RawAudioDataDelegate(float[] rawAudioData);
	private event RawAudioDataDelegate rawAudioDataEvent;

	public delegate void RecordingStartedDelegate();
	private event RecordingStartedDelegate recordingStartedEvent;

	public delegate void RecordingEndedDelegate();
	private event RecordingEndedDelegate recordingEndedEvent;

	// Subscribing and unsubscribing to events
	public void AddSpectrumDataDelegate(SpectrumDataDelegate del)
	{
		spectrumDataEvent += del;
	}

	public void RemoveSpectrumDataDelegate(SpectrumDataDelegate del)
	{
		spectrumDataEvent -= del;
	}

	public void AddAudioOutputDataDelegate(AudioOutputDataDelegate del)
	{
		outputDataEvent += del;
	}

	public void RemoveAudioOutputDataDelegate(AudioOutputDataDelegate del)
	{
		outputDataEvent -= del;
	}

	public void AddRawAudioDataDelegate(RawAudioDataDelegate del)
	{
		rawAudioDataEvent += del;
	}

	public void RemoveRawAudioDataDelegate(RawAudioDataDelegate del)
	{
		rawAudioDataEvent -= del;
	}

	public void AddRecordingStartedDelegate(RecordingStartedDelegate del)
	{
		recordingStartedEvent += del;
	}

	public void RemoveRecordingStartedDelegate(RecordingStartedDelegate del)
	{
		recordingStartedEvent -= del;
	}

	public void AddRecordingEndedDelegate(RecordingEndedDelegate del)
	{
		recordingEndedEvent += del;
	}

	public void RemoveRecordingEndedDelegate(RecordingEndedDelegate del)
	{
		recordingEndedEvent -= del;
	}

	/// <summary>
	/// Initialize variables on start.
	/// </summary>
	void Start()
	{
		InitVariables ();		
	}

	/// <summary>
	/// Get AudioSource 
	/// Initialize all variables.
	/// </summary>
	void InitVariables()
	{
		audioBufferSize = AudioConstants.audioBufferSize;
		spectrumSize = AudioConstants.spectrumSize;
		sampleRate = AudioConstants.sampleRate;

		audioSource = GetComponent<AudioSource> ();
		spectrumData = new float[spectrumSize];
		audioOutputData = new float[audioBufferSize];
		rawAudioData = new float[audioBufferSize];
	}

	/// <summary>
	/// Starts the microphone.
	/// </summary>
	public void StartMicrophone()
	{
		audioSource.clip = Microphone.Start (null, true, 1, sampleRate);
		audioSource.Play();
		audioSource.loop = true;
		if(recordingStartedEvent != null)
			recordingStartedEvent ();
	}

	/// <summary>
	/// Stops the microphone.
	/// </summary>
	public void StopMicrophone()
	{
		Microphone.End (null);
		audioSource.Stop (); 
		audioSource.clip = null;
		if(recordingEndedEvent != null)
			recordingEndedEvent ();
	}

	/// <summary>
	/// Toggle the audio data retrieving action
	/// </summary>
	/// <param name="value">If set to <c>true</c> value.</param>
	public void SetAudioDataTrigger(bool value)
	{
		getAudioData = value;
	}

	/// <summary>
	/// Toggle the spectrum data retrieving action
	/// </summary>
	/// <param name="value">If set to <c>true</c> value.</param>
	public void SetSpectrumDataTrigger(bool value)
	{
		getSpectrumData = value;
	}



	/// <summary>
	/// Get Spectrum data and Output data from AudioSource
	/// Fire events
	/// </summary>
	void Update()
	{
		// Get audio output data
		if(getAudioData) 
		{
			audioSource.clip.GetData(audioOutputData,0);
			audioSource.GetOutputData (rawAudioData, 0);

			if (outputDataEvent != null)
				outputDataEvent (audioOutputData);

			if (rawAudioDataEvent != null)
				rawAudioDataEvent (rawAudioData);
		}

		// Get Spectrum data
		if(getSpectrumData) 
		{
			audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);	

			if (spectrumDataEvent != null)
				spectrumDataEvent (spectrumData);
		}
	}
}
