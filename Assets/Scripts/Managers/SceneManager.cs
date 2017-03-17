using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EScene
{
	UI,
	ChakraLongTones
}

public class SceneManager : MonoBehaviour {

	private static SceneManager instance;
	public static SceneManager GetInstance()
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

	public void LoadScene(EScene scene)
	{
		switch(scene)
		{
			case EScene.UI :
				break;

			case EScene.ChakraLongTones :
				break;
		}
	}
}
