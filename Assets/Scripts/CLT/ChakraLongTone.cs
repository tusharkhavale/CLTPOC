using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChakraLongTone : MonoBehaviour {

	private static ChakraLongTone instance;
	private int widthMultiplier = 100;
	private float interpolant = 0.1f;
	private static Vector3 defaultScale = new Vector3 (0f, 1f, 1f);

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

//			Debug.Log ((i+1)+ " harmonic Amplitude : " + harmonics [i]);

			float intensity = harmonics [i] * widthMultiplier;
			float lerpX = Mathf.Lerp(chakras[i].localScale.x,intensity,interpolant);
			Vector3 newScale = new Vector3( lerpX, chakras[i].localScale.y, chakras[i].localScale.z);
			chakras [i].localScale = newScale;
		}
	}

	public void NormalizeChakras()
	{
		for (int i = 0; i < chakras.Length; i++) 
		{
			Vector3 scale = Vector3.Lerp(chakras[i].localScale,defaultScale,(0.05f));
			chakras [i].localScale = scale;
		}
	}

}
