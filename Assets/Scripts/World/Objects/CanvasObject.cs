﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasObject : CollectableObject
{
	public Texture2D preview;
	[SerializeField] string manualTarget = "";

    public event Action OnInteract;

    public override void Interact()
	{
        OnInteract?.Invoke();
		//prevent move/rotate here
		StartCoroutine(Collect(
			player.transform.position + player.cam.transform.forward,
			new Vector3(player.rotationX - 25f, player.rotationY, 0)));
	}

	protected override void CollectEndAction()
	{
		StartCoroutine(Player.Instance.mask.PreTransition(preview, GameManager.Instance.levels[GameManager.Instance.sceneIndex + 1]));
		// StartCoroutine(Effects.mask.PreTransition(preview, manualTarget == "" ? "Intro" : manualTarget));
	}
}
