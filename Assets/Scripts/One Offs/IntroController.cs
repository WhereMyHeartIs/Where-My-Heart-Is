using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class IntroController : MonoBehaviour
{
	Player player;
	public PlayerTrigger trigger;
	public BirbAnimTester birdAnim;
	public CanvasObject canvas;

	private bool initialized = false;
	// Start is called before the first frame update
	void Start()
	{
		player = Player.Instance;
		trigger.OnPlayerEnter += birdAnim.StartNextCurve;
	}

	public void SetCanvas(CanvasObject canvas)
	{
		this.canvas = canvas;
		canvas.OnInteract += PlayTheme;
		player.canMove = false;
		player.windowEnabled = true;
		//player.characterController.Move(Vector3.zero);
		player.prompt.SetText("Press and hold Right Click or Left CTRL to Open Lens");
		InputManager.OnAltAimKeyDown += SetPrompt;
		InputManager.OnRightClickDown += SetPrompt;
		InputManager.OnLeftClickDown += EnterPrompt;
	}

	public void PlayTheme()
	{
		AudioMaster.Instance.StartMainTheme();
	}

	public void SetPrompt()
	{
		player.prompt.SetText("Left Click to Materialize");
		InputManager.OnAltAimKeyDown -= SetPrompt;
		InputManager.OnRightClickDown -= SetPrompt;
	}

	void EnterPrompt() => player.prompt.SetText("Press E to Enter Canvas");

	// Update is called once per frame
	void Update()
	{
		if (!initialized)
		{
			AudioMaster.Instance.SetAmbientVariable("Play Song", 0);
		}

	}
}
