using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectGameScreen : MonoBehaviour {

	/// <summary>
	/// Callback for Game selection click.
	/// </summary>
	public void OnClickGame()
	{
		GameController.gameController.uiManager.UIScreenTransition (EScreen.Game);
	}

	/// <summary>
	/// Callback for Analytics click.
	/// </summary>
	public void OnClickAnalytics()
	{
		transform.FindChild ("Analytics").gameObject.SetActive (true);
		transform.FindChild ("Analytics").GetComponentInChildren<Text> ().text = AnalyticsManager.GetInstance ().GetSessionLog ();
	}


}
