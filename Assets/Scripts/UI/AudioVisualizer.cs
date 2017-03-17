using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioVisualizer : MonoBehaviour {

	private Transform[] spectrumObjects;
	[Range(1, 100)] public float barMagnitude;
	public float interpolant  = 0.1f;

	/// <summary>
	/// Get transform of all child objects.
	/// Subscribe to spectrum data event
	/// </summary>
	void Start()
	{
		spectrumObjects = new Transform[transform.childCount];
		for (int i = 0; i < transform.childCount; i++)
			spectrumObjects[i] = transform.GetChild (i);

		AddSpectrumDataDelegate ();
	}

	// Subscribing and Unsubscribing to events
	void AddSpectrumDataDelegate()
	{
		GameController.gameController.audioManager.AddSpectrumDataDelegate (this.OnSpectrumDataReceived);
	}

	void RemoveSpectrumDataDelegate()
	{
		GameController.gameController.audioManager.RemoveSpectrumDataDelegate (this.OnSpectrumDataReceived);
	}

	/// <summary>
	/// Raises the spectrum data received event.
	/// Updates the spectrum object's Y scale
	/// </summary>
	/// <param name="spectrum">Spectrum.</param>
	public void OnSpectrumDataReceived(float[] spectrum)
	{
		for(int i = 0; i < spectrumObjects.Length; i++)
		{
			// apply height multiplier to intensity
			float intensity = spectrum[i] * barMagnitude;

			// calculate object's scale
			float lerpY = Mathf.Lerp(spectrumObjects[i].localScale.y,intensity,interpolant);
			Vector3 newScale = new Vector3( spectrumObjects[i].localScale.x, lerpY, spectrumObjects[i].localScale.z);

			// appply new scale to object
			spectrumObjects[i].localScale = newScale;
		}
	}

	// Remove delegate on destroy
	void OnDestroy()
	{
		RemoveSpectrumDataDelegate ();
	}

}
