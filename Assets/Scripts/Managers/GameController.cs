using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {

	public static GameController gameController;

	[HideInInspector]
	public UIManager uiManager;
	[HideInInspector]
	public PlayerPrefsManager ppManager;
	[HideInInspector]
	public SceneManager sceneManager;

	void Awake()
	{
		gameController = this;
	}

	/// <summary>
	/// Get all manager Instances.
	/// </summary>
	void Start()
	{
		uiManager = UIManager.GetInstance ();
		ppManager = PlayerPrefsManager.GetInstance ();
		sceneManager = SceneManager.GetInstance ();
	}



}
