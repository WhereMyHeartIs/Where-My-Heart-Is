using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FMOD;
using UnityEngine;
using UnityEngine.UI;
using System;

using Debug = UnityEngine.Debug;

/// <summary> Handles player behaviors. </summary>
[System.Serializable]
public class Player : Singleton<Player>, IStateMachine
{
	/// <summary> Reference to the current state. </summary>
	public PlayerState State;

	/// <summary> Reference to the players last spawn. </summary>
	[HideInInspector] public Transform lastSpawn;
	/// <summary> Reference to player rigidbody. </summary>
	[HideInInspector] public Rigidbody body;
	/// <summary> Reference to player collider. </summary>
	[HideInInspector] public CapsuleCollider playerCollider;
	/// <summary> Reference to player Camera. </summary>
	[HideInInspector] public Camera cam;
	/// <summary> Reference to FX Controller. </summary>
	[HideInInspector] public Effects VFX;
	/// <summary> Empty GameObject for where to put a Pickupable object. </summary>
	[HideInInspector] public Transform heldObjectLocation;
	/// <summary> Reference to a Pickupable object that has been picked up. </summary>
	[HideInInspector] public Pickupable heldObject;
	/// <summary> Vector3 to store and calculate vertical velocity. </summary>
	[HideInInspector] public float verticalVelocity;
	/// <summary> Player's height. </summary>
	[HideInInspector] public float playerHeight;
	/// <summary> Whether the player can move or not. </summary>
	[HideInInspector] public bool canMove = true;
	[HideInInspector] public bool playerCanRotate = true;
	/// <summary> Whether the player is crouching or not. </summary>
	[HideInInspector] public bool crouching = false;
	/// <summary> Whether the player is holding something or not. </summary>
	[HideInInspector] public bool looking = false;
	/// <summary> Whether the player is still crouching after the crouch key has been let go. </summary>
	private bool stillCrouching = false;
	public bool pickedUpFirst = false;

	// [Header("Game Object References")]
	/// <summary> Reference to heart window. </summary>
	public GameObject heartWindow;
	/// <summary> Reference to death plane. </summary>
	[HideInInspector] public Transform deathPlane;
	/// <summary> Get Window script from GameObject. </summary>
	[HideInInspector] public Prompt prompt;
	[HideInInspector] public Window window;
	[HideInInspector] public ApplyMask mask;
	[HideInInspector] public PlayerAudio audioController;
	[HideInInspector] public Hands hands;

	[Header("Parametres")]
	/// <summary> Player move speed. </summary>
	[SerializeField] float speed = 5f;
	/// <summary> Player gravity variable. </summary>
	[SerializeField] float gravity = 25f;
	/// <summary> Player jump force. </summary>
	[SerializeField] float jumpForce = 7f;
	/// <summary> Mouse sensitivity for camera rotation. </summary>
	public static float mouseSensitivity = 2f;
	/// <summary> How far the player can reach to pick something up. </summary>
	public float playerReach = 4f;
	public bool windowEnabled = true;
	[SerializeField] float fadeDuration;

	// [Header("Camera Variables")]
	/// <summary> Bounds angle the player can look upward. </summary>
	private(float, float) xRotationBounds = (-90f, 90f);
	/// <summary> Stores the rotation of the player. </summary>
	[HideInInspector] public Vector3 rotation = Vector3.zero;
	int _ViewDirID = Shader.PropertyToID("_ViewDir");

    // events
    public event Action OnOpenWindow;
    public event Action OnApplyCut;

    void Start()
	{
		playerCollider = GetComponentInChildren<CapsuleCollider>();
		body = GetComponent<Rigidbody>();
		cam = GetComponentInChildren<Camera>();
		VFX = cam.GetComponent<Effects>();
		window = GetComponent<Window>();
		mask = GetComponentInChildren<ApplyMask>();
		audioController = GetComponent<PlayerAudio>();
		hands = GetComponentInChildren<Hands>();
		prompt = GameManager.Instance.prompt;

		playerHeight = playerCollider.height;

		// Creates an empty game object at the position where a held object should be.
		heldObjectLocation = new GameObject("HeldObjectLocation").transform;
		heldObjectLocation.position = cam.transform.position + cam.transform.forward;
		heldObjectLocation.parent = cam.transform;

		Cursor.lockState = CursorLockMode.Locked; // turn off cursor
		Cursor.visible = false;
		VFX.StartFade(true, fadeDuration);

		Initialize();
	}

	public override void Initialize()
	{
		deathPlane = GameObject.FindWithTag("Finish")?.transform;
		lastSpawn = GameObject.FindWithTag("Respawn")?.transform;

		if (lastSpawn)
		{
			transform.position = lastSpawn.position;
			rotation = lastSpawn.eulerAngles;
		}
		canMove = true;
		looking = false;
		window.world = World.Instance;
		window.cam = cam;
		VFX.ToggleMask(false);
		window.Invoke("CreateFoVMesh", 1);
	}

	public override void OnExitScene()
	{ }

	public override void OnEnterScene()
	{
		// window.CreateFoVMesh();

		DialoguePacket packet = FindObjectOfType<DialoguePacket>();
		if (packet != null)
		{
			// DialogueSystem dialogueSystem = FindObjectOfType<DialogueSystem>();
			StartCoroutine(GameManager.Instance.dialogue.WriteDialogue(packet.text));
		}

		Initialize();
		VFX.StartFade(true, fadeDuration);
	}

	public void OnEnable()
	{
		// Subscribe input events to player behaviors
		InputManager.OnJumpDown += Jump;
		// InputManager.OnCrouchDown += Crouch;
		// InputManager.OnCrouchUp += EndState;
		// InputManager.OnCrouchUp += EndState;
		InputManager.OnInteractDown += Interact;
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
		// InputManager.OnCrouchUp -= EndState;
		// InputManager.OnCrouchUp -= EndState;
		InputManager.OnInteractDown -= Interact;
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

	public void SetState(PlayerState state) => (State = state).Start();
	// {
	// 	// EndState();
	// 	State = state;
	// 	State.Start();
	// }

	void FixedUpdate()
	{
		if (!GameManager.Instance.duringLoad)
		{
			if (canMove)
			{
				Move();
				Rotate();
				AdjustGravity();
			}

			prompt.UpdateText(); // non physics
			Die();
		}
	}

	/// <summary> Player sudoku function. </summary>
	private void Die()
	{
		if (!deathPlane)
		{
			Debug.LogWarning("Missing death plane!");
			return;
		}

		if (transform.position.y < deathPlane.position.y && lastSpawn)
		{
			// Set the position to the spawnpoint
			transform.position = lastSpawn.position;
			verticalVelocity = 0;

			// Set the rotation to the spawnpoint
			rotation = lastSpawn.eulerAngles;
		}
		else if (!lastSpawn)
			Debug.LogWarning("Missing spawn point!");
	}

	/// <summary> Moves and applies gravity to the player using Horizonal and Vertical Axes. </summary>
	private void Move()
	{
		Vector3 targetVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		targetVelocity = transform.TransformDirection(targetVelocity);
		targetVelocity *= speed;

		Vector3 velocity = body.velocity;
		Vector3 velocityChange = (targetVelocity - velocity);
		velocityChange.x = Mathf.Clamp(velocityChange.x, -10f, 10f);
		velocityChange.z = Mathf.Clamp(velocityChange.z, -10f, 10f);
		velocityChange.y = 0;
		body.AddForce(velocityChange, ForceMode.VelocityChange);
	}

	/// <summary> Player jump function. </summary>
	private void Jump()
	{
		if (IsGrounded()) body.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
	}

	/// <summary> Increases gravity while falling. </summary>
	/// <remarks> Used to create better feeling jump arc. </remarks>
	private void AdjustGravity()
	{
		if (!IsGrounded() && body.velocity.y < 0) body.AddForce(new Vector3(0, -10f, 0));
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
		if (!Physics.Raycast(crouchRay, out RaycastHit hit, playerHeight * 3 / 4) && crouching) { EndState(); }
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
				EndState();
				stillCrouching = false;
			}
		}
	}

	/// <summary> Handles player behavior when interacting with objects. </summary>
	void Interact()
	{
		var hit = RaycastInteractable();
		if (heldObject || hit is Pickupable)
		{
			PickUp(!heldObject, hit as Pickupable);
		}
		else if (hit)
			hit.Interact();
	}

	private void PickUp(bool pickingUp, Pickupable obj)
	{
		if (pickingUp)
		{
			SetState(new PickUp(this, obj));
		}
		// else if (looking) { SetState(new Inspect(this)); } //unused for now
		else
		{
			if (heldObject.dissolves)
				StartCoroutine(heldObject.DissolveOnDrop(1));
			else
				EndState();
		}
	}

	/// <summary> Player aiming function. </summary>
	private void Aiming()
	{
		if (windowEnabled && !heldObject && !GameManager.Instance.duringLoad)
		{
			SetState(new Aiming(this));
			StartCoroutine(hands.WaitAndAim());
            OnOpenWindow?.Invoke();
		}
	}

	/// <summary> The player cut function. </summary>
	private void Cut()
	{
		if (State is Aiming && windowEnabled && !heldObject)
		{
            // SetState(new Cut(this));
            window.ApplyCut();
			hands.RevertAim();
			audioController.PlaceWindow();
			heartWindow.SetActive(false);
			VFX.ToggleMask(false);
			EndState();
            OnApplyCut?.Invoke();
		}
	}
	public bool IsGrounded()
	{
		RaycastHit ray;
		return Physics.SphereCast(playerCollider.transform.position, 0.2f, Vector3.down, out ray, playerHeight / 2 - 0.1f);
	}

	public InteractableObject RaycastInteractable() => Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, playerReach, 1 << 9) ? hit.transform.GetComponent<InteractableObject>() : null;
}
