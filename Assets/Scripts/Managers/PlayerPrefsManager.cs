using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPrefsManager : MonoBehaviour {

	private static PlayerPrefsManager instance;


	public static PlayerPrefsManager GetInstance()
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
		PlayerPrefs.GetString ("UserName");
	}

	public string PlayerName 
	{
		get{return PlayerPrefs.GetString ("UserName");}
		set{PlayerPrefs.SetString ("UserName", value);}
	}
}
