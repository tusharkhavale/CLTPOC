using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChakraLongTone : MonoBehaviour {

	private static ChakraLongTone instance;
	private int widthMultiplier = 100;
	private float interpolant = 1.0f;

	public Transform [] chakras;

	public static ChakraLongTone GetInstance()
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


	public void UpdateChakras(float [] harmonics)
	{
		for (int i = 0; i < harmonics.Length; i++) 
		{
			float intensity = harmonics [i] * widthMultiplier;
			float lerpY = Mathf.Lerp(chakras[i].localScale.y,intensity,interpolant);
			Vector3 newScale = new Vector3( chakras[i].localScale.x, lerpY, chakras[i].localScale.z);
			chakras [i].localScale = newScale;
		}
	}

}
