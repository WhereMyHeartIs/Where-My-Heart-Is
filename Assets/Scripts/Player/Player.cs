﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Handles player movement and player interaction </summary>
[System.Serializable]
public class Player : Singleton<Player>, IStateMachine
{
	/// <summary> Reference to the current state. </summary>
	public PlayerState State;

	/// <summary> Reference to the players last spawn. </summary>
	[HideInInspector] public Transform lastSpawn;
	/// <summary> Reference to player CharacterController. </summary>
	[HideInInspector] public CharacterController characterController;
	/// <summary> Reference to player Camera. </summary>
	[HideInInspector] public Camera cam;
	/// <summary> Reference to FX Controller. </summary>
	[HideInInspector] public Effects VFX;
	/// <summary> Empty GameObject for where to put a Pickupable object. </summary>
	[HideInInspector] public Transform heldObjectLocation;
	/// <summary> Reference to a Pickupable object that has been picked up. </summary>
	[HideInInspector] public InteractableObject heldObject;
	/// <summary> Vector3 to store and calculate vertical velocity. </summary>
	[HideInInspector] public float verticalVelocity;
	/// <summary> Player's height. </summary>
	[HideInInspector] public float playerHeight;
	/// <summary> Whether the player can move or not. </summary>
	[HideInInspector] public bool playerCanMove = true;
	[HideInInspector] public bool playerCanRotate = true;
	/// <summary> Whether the player is crouching or not. </summary>
	[HideInInspector] public bool crouching = false;
	/// <summary> Whether the player is holding something or not. </summary>
	[HideInInspector] public bool holding = false;
	/// <summary> Whether the player is inspecting a Pickupable object or not. </summary>
	[HideInInspector] public bool looking = false;
	/// <summary> Whether the player is still crouching after the crouch key has been let go. </summary>
	private bool stillCrouching = false;
	public bool pickedUpFirst = false;

	/// <summary> Vector3 to store and calculate move direction. </summary>
	private Vector3 moveDirection;

	// [Header("Game Object References")]
	/// <summary> Reference to heart window. </summary>
	[HideInInspector] public GameObject heartWindow;
	/// <summary> Reference to death plane. </summary>
	[HideInInspector] public Transform deathPlane;
	/// <summary> Get Window script from GameObject. </summary>
	[HideInInspector] public Window window;
	[HideInInspector] public ApplyMask mask;
	[HideInInspector] public PlayerAudio audioController;

	[Header("UI")]
	/// <summary> Reference for interactPrompt UI object. </summary>
	[SerializeField] public GameObject interactPrompt;

	[Header("Parametres")]
	/// <summary> Player move speed. </summary>
	[SerializeField] float speed = 5f;
	/// <summary> Player gravity variable. </summary>
	[SerializeField] float gravity = 25f;
	/// <summary> Player jump force. </summary>
	public float jumpForce = 7f;
	/// <summary> Mouse sensitivity for camera rotation. </summary>
	[SerializeField] float mouseSensitivity = 2f;
	/// <summary> How far the player can reach to pick something up. </summary>
	public float playerReach = 4f;

	public bool windowEnabled = true;

	public bool sceneActive;

	public GameObject fadeInObject;
	public float fadeInLength;
	public bool playFade;

	// [Header("Camera Variables")]
	/// <summary> Bounds angle the player can look upward. </summary>
	private(float, float)xRotationBounds = (-90f, 90f);
	/// <summary> Stores the rotation of the player. </summary>
	[HideInInspector] public Vector3 rotation = Vector3.zero;
	public Hands hands;

	int _ViewDirID = Shader.PropertyToID("_ViewDir");

	void Start()
	{
		sceneActive = true;
		characterController = GetComponent<CharacterController>();
		cam = GetComponentInChildren<Camera>();
		VFX = cam.GetComponent<Effects>();
		window = GetComponentInChildren<Window>();
		heartWindow = window.gameObject;
		mask = GetComponentInChildren<ApplyMask>();
		audioController = GetComponent<PlayerAudio>();
		hands = GetComponentInChildren<Hands>();

		// Get reference to the player height using the CharacterController's height.
		playerHeight = characterController.height;
		// Creates an empty game object at the position where a held object should be.
		heldObjectLocation = new GameObject("HeldObjectLocation").transform;
		heldObjectLocation.position = cam.transform.position + cam.transform.forward;
		heldObjectLocation.parent = cam.transform;

		Cursor.lockState = CursorLockMode.Locked; // turn off cursor
		Cursor.visible = false;
		BeginFadeIn();

		Initialize();
	}

	public override void Initialize()
	{
		interactPrompt = GameObject.FindWithTag("InteractPrompt");
		deathPlane = GameObject.FindWithTag("Finish")?.transform;
		lastSpawn = GameObject.FindWithTag("Respawn")?.transform;

		if (lastSpawn)
		{
			transform.position = lastSpawn.position;
			rotation = lastSpawn.eulerAngles;
		}
		playerCanMove = true;
		holding = false;
		looking = false;
		window.world = World.Instance;
		VFX.ToggleMask(false);
		window.Invoke("CreateFoVMesh", 1);
	}

	public override void OnBeginTransition()
	{
		characterController.enabled = false;
		sceneActive = false;
		fadeInObject.SetActive(true);
		fadeInObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
	}

	public override void OnCompleteTransition()
	{
		DialoguePacket packet = FindObjectOfType<DialoguePacket>();
		if (packet != null)
		{
			DialogueSystem dialogueSystem = FindObjectOfType<DialogueSystem>();
			StartCoroutine(dialogueSystem.WriteDialogue(packet.text));
			dialogueSystem.TextComplete += EndSceneTransitionHelper;
		}
		else
		{
			EndSceneTransitionHelper();
		}
	}

	private void EndSceneTransitionHelper(DialogueSystem dialogueSystem = null)
	{
		if (dialogueSystem != null)
		{
			dialogueSystem.TextComplete -= EndSceneTransitionHelper;
		}
		Initialize();
		characterController.enabled = true;
		Player.Instance.sceneActive = true;
		BeginFadeIn();
	}

	private void BeginFadeIn()
	{
		sceneActive = true;
		playFade = true;
		fadeInObject.SetActive(true);
		fadeInObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
		CancelInvoke("EndFadeIn");
		Invoke("EndFadeIn", fadeInLength);
	}

	private void EndFadeIn()
	{
		playFade = false;
		fadeInObject.SetActive(false);
		if (Raycast() == null)
			interactPrompt.SetActive(false);
	}

	public void OnEnable()
	{
		// Subscribe input events to player behaviors
		InputManager.OnJumpDown += Jump;
		// InputManager.OnCrouchDown += Crouch;
		// InputManager.OnCrouchUp += UnCrouch;
		// InputManager.OnCrouchUp += EndState;
		InputManager.OnPickUpDown += PickUp;
		InputManager.OnRightClickDown += Aiming;
		InputManager.OnRightClickUp += EndState;
		InputManager.OnAltAimKeyDown += Aiming;
		InputManager.OnAltAimKeyUp += EndState;
		InputManager.OnLeftClickDown += Cut;
	}

	public void OnDisable()
	{
		// Unsubscribe input events to player behaviors
		InputManager.OnJumpDown -= Jump;
		// InputManager.OnCrouchDown -= Crouch;
		// InputManager.OnCrouchUp -= UnCrouch;
		// InputManager.OnCrouchUp -= EndState;
		InputManager.OnPickUpDown -= PickUp;
		InputManager.OnRightClickDown -= Aiming;
		InputManager.OnRightClickUp -= EndState;
		InputManager.OnAltAimKeyDown -= Aiming;
		InputManager.OnAltAimKeyUp -= EndState;
		InputManager.OnLeftClickDown -= Cut;
	}

	public void EndState()
	{
		State?.End();
		State = null;
	}

	public void SetState(PlayerState state)
	{
		EndState();
		State = state;
		State.Start();
	}

	void Update()
	{
		if (playFade == true)
		{
			Color oldColor = fadeInObject.GetComponent<SpriteRenderer>().color;
			fadeInObject.GetComponent<SpriteRenderer>().color = new Color(oldColor.r, oldColor.g, oldColor.b, oldColor.a - (Time.deltaTime / fadeInLength));
		}
	}

	void FixedUpdate()
	{
		if (sceneActive)
		{
			if (playerCanMove)
			{
				Move();
				ApplyGravity();
				Rotate();
				characterController.Move(moveDirection);
			}

			UpdateInteractPrompt();
			StuckCrouching();
			Die();
		}
	}

	/// <summary> Player sudoku function. </summary>
	// private void Die() => SetState(new Die(this));
	private void Die()
	{
		if (!deathPlane)
		{
			Debug.LogWarning("Missing death plane!");
			return;
		}

		if (transform.position.y < deathPlane.position.y)
		{
			if (lastSpawn)
			{
				// Set the position to the spawnpoint
				transform.position = lastSpawn.position;
				verticalVelocity = 0;

				// Set the rotation to the spawnpoint
				rotation = lastSpawn.eulerAngles;
			}
			else
				Debug.LogWarning("Missing spawn point!");
		}
	}

	/// <summary> Moves and applies gravity to the player using Horizonal and Vertical Axes. </summary>
	private void Move()
	{
		moveDirection = Input.GetAxis("Vertical") * transform.forward + Input.GetAxis("Horizontal") * transform.right;
		Vector3 horizontal = characterController.velocity - characterController.velocity.y * Vector3.up;
		audioController.SetWalkingVelocity(Mathf.RoundToInt(horizontal.magnitude) / speed);
		moveDirection *= speed * Time.deltaTime;
	}

	/// <summary> Applies gravity to the player and includes jump. </summary>
	private void ApplyGravity()
	{
		if (!characterController.isGrounded)
		{
			verticalVelocity -= gravity * Time.deltaTime;
		}
		moveDirection.y = verticalVelocity * Time.deltaTime;
	}

	/// <summary> Player jump function. </summary>
	private void Jump()
	{
		if (characterController.isGrounded)
		{
			var tempState = State;
			SetState(new Jump(this));
			State = tempState;
		}
	}

	/// <summary> Rotates the player and camera based on mouse movement. </summary>
	private void Rotate()
	{
		if (playerCanRotate)
		{ // Get the rotation from the Mouse X and Mouse Y Axes and scale them by mouseSensitivity.
			rotation.y += Input.GetAxis("Mouse X") * mouseSensitivity;
			rotation.x += Input.GetAxis("Mouse Y") * mouseSensitivity;

			// Limit the rotation along the x axis.
			rotation.x = Mathf.Clamp(rotation.x, xRotationBounds.Item1, xRotationBounds.Item2);

			// Rotate the player along the y axis.
			transform.localEulerAngles = new Vector3(0, rotation.y, 0);

			// Rotate the player camera along the x axis.
			// Done exclusively on camera rotation so that movement is not hindered by looking up or down.
			cam.transform.localEulerAngles = new Vector3(-rotation.x, 0, 0);

			Shader.SetGlobalVector(_ViewDirID, cam.transform.forward.normalized);

			// Allow the player to get out of the mouse lock.
			if (Input.GetKey(KeyCode.Escape))
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}
	}

	/// <summary> Player crouch function. </summary>
	private void Crouch() => SetState(new Crouch(this));

	/// <summary> Player uncrouch function. </summary>
	/// <remarks> If the player is unable to uncrouch, it sets a bool to enable a check in update. </remarks>
	private void UnCrouch()
	{
		// Ray looking straight up from the player's position.
		Ray crouchRay = new Ray(transform.position, Vector3.up);
		if (!Physics.Raycast(crouchRay, out RaycastHit hit, playerHeight * 3 / 4) && crouching) { SetState(new UnCrouch(this)); }
		else { stillCrouching = true; } // The player did not uncrouch
	}

	/// <summary> If player was unable to uncrouch, perform this check until they can uncrouch. </summary>
	private void StuckCrouching()
	{
		if (stillCrouching)
		{
			// Ray looking straight up from the player's position.
			Ray crouchRay = new Ray(transform.position, Vector3.up);
			if (!Physics.Raycast(crouchRay, out RaycastHit hit, playerHeight * 3 / 4))
			{
				SetState(new UnCrouch(this));
				stillCrouching = false;
			}
		}
	}

	/// <summary> Handles player behavior when interacting with objects. </summary>
	private void PickUp()
	{
		if (!GameManager.Instance.duringLoad)
		{
			if (!holding && !looking) { SetState(new PickUp(this)); }
			else if (looking) { SetState(new Inspect(this)); } //unused for now
			else if (holding)
			{
				if (heldObject.GetComponent<GateKey>() && !heldObject.GetComponent<GateKey>().GateCheck())
					StartCoroutine(Effects.DissolveOnDrop(heldObject as GateKey, 1));
				else
					SetState(new Drop(this));
			}
		}
	}

	/// <summary> Player aiming function. </summary>
	private void Aiming()
	{
		if (windowEnabled && !holding && sceneActive)
		{
			SetState(new Aiming(this));
			StartCoroutine(hands.WaitAndAim());
		}
	}

	/// <summary> The player cut function. </summary>
	private void Cut()
	{
		if (State is Aiming && windowEnabled && !holding)SetState(new Cut(this));
	}

	/// <summary> Interact prompt handling. </summary>
	private void UpdateInteractPrompt()
	{
		if (!(State is Aiming) && interactPrompt)
		{
			// Raycast for what the player is looking at.
			Transform hit = Raycast();

			if (heldObject is Placeable && (heldObject as Placeable).PlaceConditionsMet())
			{
				interactPrompt.GetComponent<Text>().text = "Press E to Place Canvas";
				interactPrompt.SetActive(true);
			}
			else if (hit != null && hit.GetComponent<InteractableObject>() || !pickedUpFirst)
			{
				if (!holding && playerCanMove)
				{
					if (hit != null && (bool)hit.GetComponent<BirbAnimTester>())
						interactPrompt.GetComponent<Text>().text = "Press E to Interact with Bird";
					else if (hit != null && (bool)hit.GetComponent<CanvasObject>())
						interactPrompt.GetComponent<Text>().text = "Press E to Enter Canvas";
					else
						interactPrompt.GetComponent<Text>().text = "Press E to Pick Up";

					interactPrompt.SetActive(true);
					if (hit != null && hit.GetComponent<Placeable>() && hit.transform.GetComponent<Placeable>().PlaceConditionsMet())
					{
						interactPrompt.SetActive(false);
						return;
					}
				}
			}
			else
			{
				interactPrompt.SetActive(false);
			}
		}
	}

	public void GateInteractPrompt()
	{
		interactPrompt.SetActive(true);
		interactPrompt.GetComponent<Text>().text = "Press E to Unlock";
	}

	Transform Raycast()
	{
		RaycastHit hit;
		if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, playerReach, 1 << 9))
			return hit.transform;
		return null;
	}
}
