using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pitch;

public class AudioDSP : MonoBehaviour {

	private PitchTracker pitchTracker;
	public List<int> detectedPitches;
	private int samplerate;
	private float dbRefValue; // this represents 0dB
	private int currrentPitch;
	public FrequencyMapping freqMapping;
	private float noiseCeilingValue;
	private bool noiseCeilingCalibration;
	public float dbValue;
	public float rmsValue;
	private float fudgeFactor = 1.21f; // gray area between velocity zones = 11%
	private float noiseSensitivityFloor; 

	// Events and delegates
	public delegate void HarmonicsDataDelegate(float[] data);
	private event HarmonicsDataDelegate harmonicsDataEvent;

	public delegate void PitchDetectedDelegate(int pitch);
	private event PitchDetectedDelegate pitchDetectedEvent;

	public delegate void InputSoundDetectedDelegate();
	private event InputSoundDetectedDelegate inputSoundDetectedEvent;

	public delegate void NoiseCalibratedDelegate();
	private event NoiseCalibratedDelegate noiseCalibratedEvent;


	// Subscribing and Unsubscribing to events
	public void AddHarmonicsDataDelegate(HarmonicsDataDelegate del)
	{
		harmonicsDataEvent += del;
	}

	public void RemoveHarmonicsDataDelegate(HarmonicsDataDelegate del)
	{
		harmonicsDataEvent -= del; 
	}

	public void AddPitchDetectedDelegate(PitchDetectedDelegate del)
	{
		pitchDetectedEvent += del;
	}

	public void RemovePitchDetectedDelegate(PitchDetectedDelegate del)
	{
		pitchDetectedEvent -= del;
	}

	public void AddInputSoundDetectedDelegate(InputSoundDetectedDelegate del)
	{
		inputSoundDetectedEvent += del;
	}

	public void RemoveInputSoundDetectedDelegate(InputSoundDetectedDelegate del)
	{
		inputSoundDetectedEvent -= del;
	}

	public void AddNoiseCalibratedDelegate(NoiseCalibratedDelegate del)
	{
		noiseCalibratedEvent += del;
	}

	public void RemoveNoiseCalibratedDelegate(NoiseCalibratedDelegate del)
	{
		noiseCalibratedEvent -= del;
	}

	/// <summary>
	/// Initialize variables on start.
	/// </summary>
	void Start()
	{
		InitVariables ();
	}

	/// <summary>
	/// Initializes the variables.
	/// </summary>
	void InitVariables()
	{
		samplerate = AudioConstants.sampleRate;
		dbRefValue = AudioConstants.dbRefValue;
	}

	/// <summary>
	/// Initializes the PitchTracker.
	/// Add Pitch Detection delegate
	/// </summary>
	public void InitializePitchTracker()
	{
		pitchTracker = new PitchTracker();
		pitchTracker.SampleRate = samplerate;
		pitchTracker.PitchDetected += new PitchTracker.PitchDetectedHandler(PitchDetectedListener);
		AddAudioDataDelgates ();
	}


	/// <summary>
	/// Ends the pitch tracking.
	/// Remove Pitch Detection delegate
	/// </summary>
	public void EndPitchTracking()
	{
		RemoveAudioDataDelegates ();
		pitchTracker.PitchDetected -= this.PitchDetectedListener;
		pitchTracker = null;
	}

	/// <summary>
	/// Start harmonic data calculation
	/// Subscribe to spectrum data events.
	/// </summary>
	public void StartHarmonicsCalculation()
	{
		GameController.gameController.audioManager.AddSpectrumDataDelegate (this.OnSpectrumDataReceived);
	}

	/// <summary>
	/// End harmonic data calculation
	/// Unsubscribe
	/// </summary>
	public void EndHarmonicsCalculation()
	{
		GameController.gameController.audioManager.RemoveSpectrumDataDelegate (this.OnSpectrumDataReceived);
	}

	/// <summary>
	/// Subscribe to audio raw data events.
	/// </summary>
	private void AddAudioDataDelgates()
	{
		GameController.gameController.audioManager.AddAudioOutputDataDelegate(this.OnAudioOutputDataReceived);
		GameController.gameController.audioManager.AddRawAudioDataDelegate (this.OnRawAudioDataReceived);
	}

	/// <summary>
	/// Unsubscribe 
	/// </summary>
	private void RemoveAudioDataDelegates()
	{
		GameController.gameController.audioManager.RemoveAudioOutputDataDelegate (this.OnAudioOutputDataReceived);
		GameController.gameController.audioManager.RemoveRawAudioDataDelegate (this.OnRawAudioDataReceived);
	}

	/// <summary>
	/// Raises the audio output data received event.
	/// Process the AudioOutputData
	/// </summary>
	/// <param name="audioOutputData">Audio output data.</param>
	void OnAudioOutputDataReceived(float[] audioOutputData)
	{
		detectedPitches.Clear();
		pitchTracker.ProcessBuffer(audioOutputData);
	}

	/// <summary>
	/// Raises the spectrum data received event.
	/// Ge the first 7 harmonics  from the spectrum data
	/// </summary>
	/// <param name="audioOutputData">Audio output data.</param>
	void OnSpectrumDataReceived(float[] spectrumData)
	{
		if(currrentPitch != 0)
			CalculateHarmonicsAmplitude (spectrumData,0,7);
	}

	/// <summary>
	/// Calculates the harmonics amplitude in db.
	/// </summary>
	/// <param name="spectrum">Spectrum.</param>
	/// <param name="peakBin">Peak bin.</param>
	/// <param name="windowHalfLen">Window half length.</param>
	/// <param name="harmonic">Harmonic.</param>
	private void CalculateHarmonicsAmplitude(float[] spectrum, int windowHalfLen, int harmonic)
	{

		float freqN = currrentPitch * spectrum.Length *2f/samplerate;
		int peakBin = (int)freqN;

		float[] harmonicsData = new float[harmonic];
		for (int i = 1; i <= harmonicsData.Length; i++) 
		{
			double sumOfSquares = 0.0;
			int binValue = peakBin * i; 

			for (int bin = binValue-windowHalfLen; bin <= binValue+windowHalfLen; bin++)
			{
				if (bin < spectrum.Length && bin > 0) 
				{
					sumOfSquares += spectrum[bin] * spectrum[bin];
				}
			}

			float rmsValue = Mathf.Sqrt ((float)sumOfSquares);
			harmonicsData [i - 1] = 20 * Mathf.Log10 (rmsValue/dbRefValue);
		}

		// Fire harmonics event
		if (harmonicsDataEvent != null)
			harmonicsDataEvent (harmonicsData);
	}


	/// <summary>
	/// Pitchs detected delegate.
	/// </summary>
	/// <param name="sender">Sender.</param>
	/// <param name="pitchRecord">Pitch record.</param>
	private void PitchDetectedListener(PitchTracker sender, PitchTracker.PitchRecord pitchRecord) 
	{
		int pitch = (int)Mathf.Round(pitchRecord.Pitch);
		if (!detectedPitches.Contains(pitch)) detectedPitches.Add(pitch);

		int lowestPitch = pitch;
		foreach(int p in detectedPitches)
		{
			if (p > 0 && p < lowestPitch)
				lowestPitch = p;
		}

		currrentPitch = lowestPitch;

		// Fire pitch detected event
		if (pitchDetectedEvent != null) 
			pitchDetectedEvent (currrentPitch);
	}


	/// <summary>
	/// Raises the raw audio data received event.
	/// Calculates rms and db values
	/// </summary>
	/// <param name="rawAudioData">Raw audio data.</param>
	private void OnRawAudioDataReceived(float[] rawAudioData)
	{
		float sumOfSquares = 0f;
		for (int i = 0; i < rawAudioData.Length; i++) 
		{
			sumOfSquares += rawAudioData[i] * rawAudioData[i];
		}

		rmsValue = Mathf.Sqrt (sumOfSquares / rawAudioData.Length);
		dbValue = 20 * Mathf.Log10 (rmsValue / dbRefValue);

		if (rmsValue > noiseSensitivityFloor && inputSoundDetectedEvent != null)
			inputSoundDetectedEvent ();
	}

	/// <summary>
	/// Starts the noise ceiling calibration.
	/// </summary>
	public void StartNoiseCeilingCalibration()
	{
		noiseCeilingCalibration = true;
		StartCoroutine (CalibrateNoiseCeiling ());
		StartCoroutine (EndNoiseCeilingCalibration ());
	}

	/// <summary>
	/// Calibrates the noise ceiling value.
	/// </summary>
	/// <returns>The noise ceiling.</returns>
	private IEnumerator CalibrateNoiseCeiling()
	{
		noiseCeilingValue = float.NegativeInfinity;
		while (noiseCeilingCalibration) 
		{
			if (rmsValue > noiseCeilingValue)
				noiseCeilingValue = rmsValue;
			
			yield return null;
		}
	}

	/// <summary>
	/// Ends the noise ceiling calibration.
	/// </summary>
	/// <returns>The noise ceiling calibration.</returns>
	private IEnumerator EndNoiseCeilingCalibration()
	{
		yield return new WaitForSeconds (2.0f);
		noiseCeilingCalibration = false;
		noiseSensitivityFloor = noiseCeilingValue * fudgeFactor;
		if(noiseCalibratedEvent != null)
			noiseCalibratedEvent ();
	}
}
