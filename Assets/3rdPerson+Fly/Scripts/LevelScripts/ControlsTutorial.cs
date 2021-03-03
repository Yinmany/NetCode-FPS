using UnityEngine;

// This class is created for the example scene. There is no support for this script.
public class ControlsTutorial : MonoBehaviour
{
	private string message = "";
	private bool showMsg = false;

	private int w = 550;
	private int h = 100;
	private Rect textArea;
	private GUIStyle style;
	private Color textColor;

	private GameObject KeyboardCommands;
	private GameObject gamepadCommands;

	void Awake()
	{
		style = new GUIStyle();
		style.alignment = TextAnchor.MiddleCenter;
		style.fontSize = 36;
		style.wordWrap = true;
		textColor = Color.white;
		textColor.a = 0;
		textArea = new Rect((Screen.width-w)/2, 0, w, h);

		KeyboardCommands = this.transform.Find("ScreenHUD/Keyboard").gameObject;
		gamepadCommands = this.transform.Find("ScreenHUD/Gamepad").gameObject;
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		if (Input.GetKeyDown("escape"))
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = true;
		}
		KeyboardCommands.SetActive(Input.GetKey(KeyCode.F2));
		gamepadCommands.SetActive(Input.GetKey(KeyCode.F3) || Input.GetKey(KeyCode.Joystick1Button7));
	}

	void OnGUI()
	{
		if(showMsg)
		{
			if(textColor.a <= 1)
				textColor.a += 0.5f * Time.deltaTime;
		}
		// no hint to show
		else
		{
			if(textColor.a > 0)
				textColor.a -= 0.5f * Time.deltaTime;
		}

		style.normal.textColor = textColor;

		GUI.Label(textArea, message, style);
	}

	public void SetShowMsg(bool show)
	{
		showMsg = show;
	}

	public void SetMessage(string msg)
	{
		message = msg;
	}
}
