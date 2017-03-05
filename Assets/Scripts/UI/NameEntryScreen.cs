using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NameEntryScreen : MonoBehaviour {
	// assign in inspector
	public InputField inputName;
	public GameObject goButton;


	/// <summary>
	/// Callback for Input value change.
	/// Enable go button if name entered
	/// </summary>
	public void OnValueChanged()
	{
		if (string.IsNullOrEmpty (inputName.text))
			goButton.SetActive (false);
		else
			goButton.SetActive (true);
	}

	/// <summary>
	/// Callback for Go button.
	/// Go to Game page
	/// </summary>
	public void OnClickNext()
	{
		GameController.gameController.uiManager.UIScreenTransition (EScreen.SelectGame);
	}



}
