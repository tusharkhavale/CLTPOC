using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPrefsManager : MonoBehaviour {

	/// <summary>
	/// Gets or sets the name of the player from Prefs.
	/// </summary>
	/// <value>The name of the player.</value>
	public string PlayerName 
	{
		get{return PlayerPrefs.GetString ("UserName");}
		set{PlayerPrefs.SetString ("UserName", value);}
	}
}
