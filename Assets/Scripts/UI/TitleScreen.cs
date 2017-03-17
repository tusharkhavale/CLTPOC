using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScreen : MonoBehaviour {

	void Start()
	{
		StartCoroutine (WaitForTitle ());
	}

	/// <summary>
	/// Show title for 2 secs.
	/// </summary>
	/// <returns>The for title.</returns>
	IEnumerator WaitForTitle()
	{
		yield return new WaitForSeconds (2f);
		GameController.gameController.ScreenTransition(EScreen.NameEntry);
	}

}
