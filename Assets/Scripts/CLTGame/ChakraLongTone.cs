using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChakraLongTone : MonoBehaviour {

	private int widthMultiplier = 10;
	private float interpolant = 0.1f;
	private int maxWidth = 7;
	private static Vector3 defaultScale = new Vector3 (0.1f, 0.1f, 1f);
	public GameObject startButton;
	public GameObject micWarning;
	public Transform [] chakras;
	public Text pitchText;
	public Text noteText;

	/// <summary>
	/// Subscribe to Harmonics data event and Pitch + Note detected events.
	/// Start Microphone and initialise PitchDetection
	/// </summary>
	void Start()
	{
		AddhHamronicsDataDelegate ();
		AddPitchNoteDetectedDelegate ();
	}

	// Subscribing and unsubscribing to events
	private void AddhHamronicsDataDelegate()
	{
		GameController.gameController.audioDSP.AddHarmonicsDataDelegate (this.OnHarmonicsDataReceived);
	}

	private void RemoveHarmonicDataDelegate()
	{
		GameController.gameController.audioDSP.RemoveHarmonicsDataDelegate(this.OnHarmonicsDataReceived);
	}

	private void AddPitchNoteDetectedDelegate()
	{
		GameController.gameController.audioDSP.AddPitchDetectedDelegate (this.OnPitchDetected);
		GameController.gameController.audioDSP.freqMapping.AddNoteDetectedDelegate(this.OnNoteDetected);
	}

	private void RemovePitchNoteDetectedDelegate()
	{
		GameController.gameController.audioDSP.RemovePitchDetectedDelegate (this.OnPitchDetected);
		GameController.gameController.audioDSP.freqMapping.RemoveNoteDetectedDelegate(this.OnNoteDetected);
	}

	/// <summary>
	/// Callback for Start button.
	/// Start microphone recording
	/// start pitch detection
	/// </summary>
	public void OnClickStart()
	{
		GameController.gameController.StartMicrophoneRecording ();
		GameController.gameController.StartPictDetection();
	}

	/// <summary>
	/// Callback for Back button
	/// Stop Microphone recording
	/// Stop Pitch detection
	/// Unsubscribe delegates
	/// Transit to Game Selection page
	/// </summary>
	public void OnClickBack()
	{
		RemoveHarmonicDataDelegate ();
		RemovePitchNoteDetectedDelegate ();
		GameController.gameController.EndMicrophoneRecording();
		GameController.gameController.EndPitchDetection();
		GameController.gameController.ScreenTransition(EScreen.SelectGame);
	}

	// Update display- Pitch
	// Normalize Chakras if pitch is 0
	void OnPitchDetected(int pitch)
	{
		if (pitch == 0)
			NormalizeChakras ();
		
		pitchText.text = "" + pitch + "Hz";
	}

	//update display- Note
	void OnNoteDetected(string note)
	{
		noteText.text = note;
	}

	/// <summary>
	/// Harmonics data delegate
	/// Updates the chakras width based on harmoic data.
	/// </summary>
	/// <param name="harmonics">Harmonics.</param>
	public void OnHarmonicsDataReceived(float [] harmonics)
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
	public void NormalizeChakras()
	{
		for (int i = 0; i < chakras.Length; i++) 
		{
			Vector3 scale = Vector3.Lerp(chakras[i].localScale,defaultScale,(0.03f));
			chakras [i].localScale = scale;
		}
	}

}
