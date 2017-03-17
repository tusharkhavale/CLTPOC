using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
	private AudioSource audioSource;
	private int audioBufferSize = 1024;
	private int spectrumSize = 1024;
	private int sampleRate = 44100;
	private float[] spectrumData;
	private float[] audioOutputData;
	private float[] rawAudioData;
	private bool getAudioData;

	private bool noteWasSounding = false;
	private bool initialized = false;

	// Events and Delegates
	public delegate void SpectrumDataDelegate(float[] spectrumData);
	private event SpectrumDataDelegate spectrumDataEvent;

	public delegate void AudioOutputDataDelegate(float[] outputData);
	private event AudioOutputDataDelegate outputDataEvent;

	public delegate void RawAudioDataDelegate(float[] rawAudioData);
	private event RawAudioDataDelegate rawAudioDataEvent;

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
		audioSource = GetComponent<AudioSource> ();
		spectrumData = new float[spectrumSize];
		audioOutputData = new float[audioBufferSize];
		audioBufferSize = AudioConstants.audioBufferSize;
		spectrumSize = AudioConstants.spectrumSize;
		sampleRate = AudioConstants.sampleRate;
	}

	/// <summary>
	/// Starts the microphone.
	/// </summary>
	public void StartMicrophone()
	{
		audioSource.clip = Microphone.Start (null, true, 1, sampleRate);
		audioSource.Play();
		audioSource.loop = true;
		GetAudioData (true);
	}

	/// <summary>
	/// Stops the microphone.
	/// </summary>
	public void StopMicrophone()
	{
		GetAudioData (false);
		Microphone.End (null);
		audioSource.Stop (); 
		audioSource.clip = null;
	}

	/// <summary>
	/// Trigggers the audio data retriving action
	/// </summary>
	/// <param name="value">If set to <c>true</c> value.</param>
	public void GetAudioData(bool value)
	{
		getAudioData = value;
	}

	/// <summary>
	/// Get Spectrum data and Output data from AudioSource
	/// Fire events
	/// </summary>
	void Update()
	{
		if(getAudioData) 
		{
			audioSource.clip.GetData(audioOutputData,0);
			audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);	
			audioSource.GetOutputData (rawAudioData, 0);

			if (spectrumDataEvent != null)
				spectrumDataEvent (spectrumData);

			if (outputDataEvent != null)
				outputDataEvent (audioOutputData);

			if (rawAudioDataEvent != null)
				rawAudioDataEvent (rawAudioData);
		}
	}
}
