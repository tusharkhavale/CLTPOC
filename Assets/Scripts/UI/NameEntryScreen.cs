using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NameEntryScreen : MonoBehaviour {
	// assign in inspector
	public InputField inputName;
	public GameObject enterNameText;
	public Text welcomeUserText;
	public GameObject changeUserButton;
	public GameObject goButton;



	void Start()
	{
		CheckIfNewUser ();
	}

	// Check if new user
	void CheckIfNewUser()
	{
		string name = GameController.gameController.ppManager.PlayerName;
		if (string.IsNullOrEmpty(name))
			ShowNewUserUI ();
		else
			ShowOldUserUI ();
	}

	void ShowNewUserUI()
	{
		inputName.gameObject.SetActive (true);
		enterNameText.SetActive (true);
		changeUserButton.SetActive (false);
		welcomeUserText.gameObject.SetActive (false);
		goButton.SetActive (false);
	}

	void ShowOldUserUI()
	{
		changeUserButton.SetActive (true);
		welcomeUserText.gameObject.SetActive (true);
		welcomeUserText.text = "Hello " + GameController.gameController.ppManager.PlayerName;
		goButton.SetActive (true);
		inputName.gameObject.SetActive (false);
		enterNameText.SetActive (false);
	}



	public void OnChangeUser()
	{
		GameController.gameController.ppManager.PlayerName = "";
		ShowNewUserUI ();
	}


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
		if(!string.IsNullOrEmpty(inputName.text))
			GameController.gameController.ppManager.PlayerName = inputName.text;

		GameController.gameController.ScreenTransition (EScreen.SelectGame);
	}



}
