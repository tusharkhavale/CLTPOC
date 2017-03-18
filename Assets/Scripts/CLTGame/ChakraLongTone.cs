using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChakraLongTone : MonoBehaviour {

	private int widthMultiplier = 10;
	private float interpolant = 0.1f;
	private static Vector3 defaultScale = new Vector3 (0.1f, 0.1f, 1f);
	public GameObject noiseCeilingMessage;
	public Transform [] chakras;
	public Text pitchText;
	public Text noteText;


	/// <summary>
	/// Subscribe to Harmonics data event and Pitch + Note detected events.
	/// </summary>
	void Start()
	{
		AddDelegates ();
	}

	// Subscribing and unsubscribing to events
	private void AddDelegates()
	{
		GameController.gameController.audioDSP.AddHarmonicsDataDelegate (this.OnHarmonicsDataReceived);
		GameController.gameController.audioDSP.AddPitchDetectedDelegate (this.OnPitchDetected);
		GameController.gameController.audioDSP.freqMapping.AddNoteDetectedDelegate(this.OnNoteDetected);
		GameController.gameController.audioDSP.AddNoiseCalibratedDelegate (this.OnNoiseCalibrated);
	}

	private void RemoveDelegates()
	{
		GameController.gameController.audioDSP.RemoveHarmonicsDataDelegate(this.OnHarmonicsDataReceived);
		GameController.gameController.audioDSP.RemovePitchDetectedDelegate (this.OnPitchDetected);
		GameController.gameController.audioDSP.freqMapping.RemoveNoteDetectedDelegate(this.OnNoteDetected);
		GameController.gameController.audioDSP.RemoveNoiseCalibratedDelegate (this.OnNoiseCalibrated);
	}


	/// <summary>
	/// Callback for Start button.
	/// Start microphone recording
	/// Trigger raw audio data and spectrum data
	/// start pitch detection
	/// </summary>
	public void OnClickStart()
	{
		GameController.gameController.StartMicrophoneRecording ();
		GameController.gameController.SetAudioDataTrigger(true);
		GameController.gameController.SetSpectrumDataTrigger(true);
		GameController.gameController.StartNoiseCeilingCalibration ();
	}

	/// <summary>
	/// Callback for Back button
	/// Stop raw audio data and spectrum data
	/// Stop Microphone recording
	/// Stop Pitch detection
	/// Unsubscribe delegates
	/// Transit to Game Selection page
	/// </summary>
	public void OnClickBack()
	{
		RemoveDelegates ();
		GameController.gameController.SetAudioDataTrigger(false);
		GameController.gameController.SetSpectrumDataTrigger(false);
		GameController.gameController.EndMicrophoneRecording();
		GameController.gameController.EndPitchDetection();
		GameController.gameController.EndHarmonicsCalculation ();
		GameController.gameController.ScreenTransition(EScreen.SelectGame);
	}

	// Update display- Pitch
	// Normalize Chakras if pitch is 0
	private void OnPitchDetected(int pitch)
	{
		string txt = "" + pitch + "Hz";
		if (pitch == 0) 
		{
			txt = "";
			NormalizeChakras ();
		}
		
		pitchText.text = txt;
	}

	//update display- Note
	private void OnNoteDetected(string note)
	{
		noteText.text = note;
	}

	 
	/// <summary>
	/// Raises the harmonics data received event.
	/// Updates the chakras width based on harmoic data.
	/// </summary>
	/// <param name="harmonics">Harmonics.</param>
	private void OnHarmonicsDataReceived(float [] harmonics)
	{
		for (int i = 0; i < harmonics.Length; i++) 
		{
			// Boost the values
			harmonics [i] += 80;
			// Normalize between 0 to 1 from 0 - 80)
			harmonics [i] = harmonics [i] / 80 * widthMultiplier;


			if (harmonics [i] < 0)
				continue;

			float intensity = harmonics [i];

			float lerpX = Mathf.Lerp(chakras[i].localScale.x,intensity,interpolant);

			float lerpY = lerpX < 0.5 ? lerpX : 0.5f;

			Vector3 newScale = new Vector3( lerpX, lerpY, chakras[i].localScale.z);
			chakras [i].localScale = newScale;
		}
	}

	/// <summary>
	/// Normalizes width of Chakras to minimum level.
	/// </summary>
	private void NormalizeChakras()
	{
		for (int i = 0; i < chakras.Length; i++) 
		{
			Vector3 scale = Vector3.Lerp(chakras[i].localScale,defaultScale,(0.03f));
			chakras [i].localScale = scale;
		}
	}

	/// <summary>
	/// Raises the noise calibrated event.
	/// Start pitch detection
	/// start harmonics amplitude calculation
	/// Hide noise calibration message
	/// </summary>
	private void OnNoiseCalibrated()
	{
		GameController.gameController.StartPictDetection();
		GameController.gameController.StartHarmonicsCalculation();
		noiseCeilingMessage.gameObject.SetActive (false);
	}
}
