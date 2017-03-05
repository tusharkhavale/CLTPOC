using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Loading : MonoBehaviour {

	Image guitarImage;


	/// <summary>
	/// Get the guitar image component
	/// set the guitar image fill amount to zero
	/// start image filling coroutine
	/// </summary>
	void OnEnable()
	{
		Transform guitar = transform.FindChild ("guitar");
		guitarImage = guitar.GetComponent<Image> ();
		guitarImage.fillAmount = 0f;
		StartCoroutine (StartFilling ());
	}

	/// <summary>
	/// Set the image fill amount to zero
	/// </summary>
	void OnDisable()
	{
		guitarImage.fillAmount = 0f;
	}


	/// <summary>
	/// Fills the guitar image with respect to delta time
	/// Disable the loading prefab on completely filling the guitar image
	/// </summary>
	IEnumerator StartFilling()
	{
		while(guitarImage.fillAmount < 1)
		{
			guitarImage.fillAmount += Time.deltaTime;
			yield return null;
		}
		gameObject.SetActive (false);
	}

}
