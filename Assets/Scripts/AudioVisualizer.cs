using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioVisualizer : MonoBehaviour {

	private static AudioVisualizer instance;
	Transform[] spectrumObjects;
	[Range(1, 100)] public float barMagnitude;
	public float interpolant  = 1;

	public static AudioVisualizer GetInstance()
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
		spectrumObjects = new Transform[transform.childCount];
		for (int i = 0; i < transform.childCount; i++)
			spectrumObjects[i] = transform.GetChild (i);
	}

	public void UpdateVisualizer(float[] spectrum)
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

}
