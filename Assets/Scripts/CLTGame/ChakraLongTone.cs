﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChakraLongTone : MonoBehaviour {

	private static ChakraLongTone instance;
	private int widthMultiplier = 100;
	private float interpolant = 0.1f;
	private static Vector3 defaultScale = new Vector3 (0.1f, 0.1f, 1f);
	public GameObject startButton;
	public GameObject micWarning;


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
			float lerpX = Mathf.Lerp(chakras[i].localScale.x,intensity,interpolant);

			float lerpY = lerpX < 0.5 ? lerpX : 0.5f;
			Vector3 newScale = new Vector3( lerpX, lerpY, chakras[i].localScale.z);
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

	void Start()
	{
		if (Microphone.devices.Length == 0) 
		{
			startButton.gameObject.SetActive (false);
			micWarning.gameObject.SetActive (true);
			StartCoroutine (CheckForMic ());
		}
		else
			startButton.gameObject.SetActive (true);
	}

	IEnumerator CheckForMic()
	{
		while (Microphone.devices.Length == 0) 
		{
			yield return null;
		}
		startButton.gameObject.SetActive (true);
		micWarning.gameObject.SetActive (false);
	}
}
