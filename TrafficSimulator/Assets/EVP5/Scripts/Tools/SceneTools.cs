//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------


using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;


namespace EVP
{

public class SceneTools : MonoBehaviour
	{
	public bool slowTimeMode = false;
	public float slowTime = 0.3f;

	public KeyCode hotkeyReset = KeyCode.R;
	public KeyCode hotkeyTime = KeyCode.T;


	void Update ()
		{
		if (Input.GetKeyDown(hotkeyReset))
			SceneManager.LoadScene(0, LoadSceneMode.Single);

		if (Input.GetKeyDown(hotkeyTime))
			slowTimeMode = !slowTimeMode;

		Time.timeScale = slowTimeMode? slowTime : 1.0f;
		}
	}
}