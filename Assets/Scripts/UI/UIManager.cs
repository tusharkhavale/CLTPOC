using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// UI Screens
public enum EScreen
{
	Title,
	NameEntry,
	Loading,
	SelectGame,
	Game,
}

public class UIManager : MonoBehaviour {

	private static UIManager instance;
	private EScreen currScreen = EScreen.Title;

	public GameObject analytics;

	public static UIManager GetInstance()
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


	/// <summary>
	/// Gets or sets the curr screen.
	/// </summary>
	public EScreen CurrScreen
	{
		get{return currScreen;}
		set{currScreen = value;}
	}

	/// <summary>
	/// Unload curr sceen
	/// Load curr screen
	/// </summary>
	/// <param name="screen">Screen.</param>
	public void UIScreenTransition(EScreen screen)
	{
		UnloadScreen (CurrScreen);
		loadScreen (screen);
	}

	/// <summary>
	/// Unload Child UI Screen prefabs from scene
	/// </summary>
	/// <param name="screen">Screen.</param>
	private void UnloadScreen(EScreen screen)
	{
		Transform page = null;
		switch (screen) 
		{
			case EScreen.Title:
				page = transform.FindChild ("TitleScreen");
				break;

			case EScreen.NameEntry:
				page = transform.FindChild ("NameEntryScreen");
											 
				break;

			case EScreen.SelectGame:
				page = transform.FindChild ("SelectGameScreen");
				break;

			case EScreen.Game:
				page = transform.FindChild ("Game");
				break;
		}
		if(page != null)
			Destroy(page.gameObject);
			
	}


	/// <summary>
	/// Set current screen
	/// Load UI Screen prefab from Resources/UIScreens.
	/// Instantiate screens and set UI as parent
	/// </summary>
	/// <param name="screen">Screen.</param>
	private void loadScreen(EScreen screen)
	{
		CurrScreen = screen;
		Object go = null;
		GameObject page = null;
		switch (screen) 
		{
			case EScreen.Title:
				// Not required
				break;

			case EScreen.NameEntry:
				go = Resources.Load<GameObject> ("UIScreens/NameEntryScreen");
				break;

			case EScreen.Game:
				go = Resources.Load<GameObject> ("UIScreens/Game");
				break;

			case EScreen.SelectGame:
				go = Resources.Load<GameObject> ("UIScreens/SelectGameScreen");
				break;
		}

		// Set UI(Canvas) as parent 
		if (go != null) 
		{
			page = Instantiate (go) as GameObject;
			page.transform.SetParent (transform,false);	
			page.name = page.name.Replace ("(Clone)", "");
		}

	}



	public void ShowLoading()
	{
		transform.FindChild ("LoadingScreen").gameObject.SetActive(true);
	}


}
