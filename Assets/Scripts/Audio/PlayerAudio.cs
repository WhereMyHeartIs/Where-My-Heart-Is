using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
	public float landingDistanceThreshold;

	[Header("Movement Event References")]
	[FMODUnity.EventRef]
	public string JumpLiftoffEvent;

	[FMODUnity.EventRef]
	public string JumpLandingEvent;

	/* [FMODUnity.EventRef]
	 public string CrouchDownEvent;

	 [FMODUnity.EventRef]
	 public string CrouchUpEvent;*/

	[FMODUnity.EventRef]
	public string WalkEvent;

	[Header("Window Event References")]
	[FMODUnity.EventRef]
	public string WindowEvent;

	private FMOD.Studio.EventInstance walkInstance;
	private FMOD.Studio.EventInstance windowInstance;

	private WalkingSurface.Surface currentSurface;

	private bool jumping;

	private void Start()
	{
		walkInstance = FMODUnity.RuntimeManager.CreateInstance(WalkEvent);
		walkInstance.start();

		windowInstance = FMODUnity.RuntimeManager.CreateInstance(WindowEvent);
		windowInstance.setParameterByName("Heart World Type", 2);
		windowInstance.start();

		currentSurface = WalkingSurface.Surface.Stone;
	}

	private void Update()
	{
		if (jumping)
		{
			/*if (Player.Instance.characterController.isGrounded)
			{
				jumping = false;
				JumpLanding();
			}*/
		}
		if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, 1 << 9))
		{
			if (hit.transform.TryComponent(out WalkingSurface surface))
			{
				currentSurface = surface.surface;
				walkInstance.setParameterByName("Surface", (float) surface.surface);
			}
			else
			{
				currentSurface = WalkingSurface.Surface.Stone;
				walkInstance.setParameterByName("Surface", (float) WalkingSurface.Surface.Stone);
			}
		}
	}

	public void SetWindowWorld(float worldIndex)
	{
		windowInstance.setParameterByName("Heart World Type", worldIndex);

	}

	public void OpenWindow()
	{
		windowInstance.setParameterByName("WindowState", 1);
	}

	public void PlaceWindow()
	{
		windowInstance.setParameterByName("WindowState", 1);
		CloseWindow();
	}

	public void CloseWindow()
	{
		windowInstance.setParameterByName("WindowState", 0);
	}

	public void JumpLiftoff()
	{
		FMOD.Studio.EventInstance instance = FMODUnity.RuntimeManager.CreateInstance(JumpLiftoffEvent);
		instance.setParameterByName("Surface", (float) currentSurface);
		instance.start();
		windowInstance.setParameterByName("WindowState", 0);
		jumping = true;
	}

	public void JumpLanding()
	{
		FMOD.Studio.EventInstance instance = FMODUnity.RuntimeManager.CreateInstance(JumpLandingEvent);
		instance.setParameterByName("Surface", (float) currentSurface);
		instance.start();
	}

	public void CrouchDown()
	{
		//FMODUnity.RuntimeManager.PlayOneShot(CrouchDownEvent, transform.position);
	}

	public void CrouchUp()
	{
		//FMODUnity.RuntimeManager.PlayOneShot(CrouchUpEvent, transform.position);
	}

	public void SetWalkingVelocity(float value)
	{
		if (jumping)
		{
			walkInstance.setParameterByName("Velocity", 0);
			return;
		}
		walkInstance.setParameterByName("Velocity", value);
	}
}
