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

	void CheckIfNewUser()
	{
		if (string.IsNullOrEmpty (PlayerPrefsManager.GetInstance ().PlayerName))
			ShowNewUserUI ();
		else
			ShowOldUSerUI ();
	}

	void ShowNewUserUI()
	{
		inputName.gameObject.SetActive (true);
		enterNameText.SetActive (true);
		changeUserButton.SetActive (false);
		welcomeUserText.gameObject.SetActive (false);
		goButton.SetActive (false);
	}

	void ShowOldUSerUI()
	{
		changeUserButton.SetActive (true);
		welcomeUserText.gameObject.SetActive (true);
		welcomeUserText.text = "Hello " + PlayerPrefsManager.GetInstance ().PlayerName;
		goButton.SetActive (true);
		inputName.gameObject.SetActive (false);
		enterNameText.SetActive (false);
	}



	public void OnChangeUser()
	{
		PlayerPrefsManager.GetInstance ().PlayerName = "";
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
			PlayerPrefsManager.GetInstance ().PlayerName = inputName.text;

			GameController.gameController.uiManager.UIScreenTransition (EScreen.SelectGame);
	}



}
