using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary> Crouch action state. </summary>
public class Crouch : PlayerState
{
	/// <summary> Constructor. </summary>
	/// <param name="_player"> Reference to player. </param>
	public Crouch(Player _player) : base(_player) { }

	public override void Start()
	{
		//player.characterController.height = player.playerHeight / 2; // Make the player crouch.
		player.audioController.CrouchDown();
		player.crouching = true;
	}

	public override void End()
	{
		player.audioController.CrouchUp();
		//player.characterController.height = player.playerHeight; // Make the player stand.
		player.crouching = false;
	}
}
