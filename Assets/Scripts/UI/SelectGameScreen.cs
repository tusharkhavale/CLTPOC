using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectGameScreen : MonoBehaviour {

	/// <summary>
	/// Callback for Game selection click.
	/// </summary>
	public void OnClickGame()
	{
		GameController.gameController.uiManager.UIScreenTransition (EScreen.Game);
	}
}
