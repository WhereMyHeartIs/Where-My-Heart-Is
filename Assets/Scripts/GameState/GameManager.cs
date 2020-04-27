using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary> Constrols app macro and scene manipulations </summary>
public class GameManager : Singleton<GameManager>
{
	public readonly string[] levels = new string[] { "Intro", "BridgeRedo", "SimpleGate", "OneCutRedo", "ComplexGate", "Pushable", "BoxHalfCut", "End" };
	public int sceneIndex = -1;
	public bool duringLoad;
	public DialogueSystem dialogue;
	public Prompt prompt;
	public Effects VFX;
	public PauseMenu pause;

	public override void Awake()
	{
		base.Awake();
		VFX = Player.Instance.GetComponentInChildren<Effects>();
		dialogue = GetComponentInChildren<DialogueSystem>();
		prompt = GetComponentInChildren<Prompt>();
		pause = GetComponentInChildren<PauseMenu>();
	}

	void Start()
	{
		sceneIndex = levels.ToList().FindIndex(name => name == SceneManager.GetActiveScene().name);
		World.Instance.name += $" [{SceneManager.GetActiveScene().name}]";
		SceneManager.sceneLoaded += new UnityEngine.Events.UnityAction<Scene, LoadSceneMode>((scene, _) => OnEnterScene());
		// SceneManager.activeSceneChanged += new UnityEngine.Events.UnityAction<Scene, Scene>((_, __) => this.Print("ActiveSceneChanged", SceneManager.GetActiveScene().name));
	}

	/// <summary> Closes the Application </summary>
	public static void QuitGame()
	{
		// prompt
		Application.Quit();
	}

	public void ChangeLevel(string scene) => Transition(scene); // temp, to be deleted

	/// <summary> Starts Coroutine to load scene async  </summary>
	/// <param name="scene"> Name of scene to load  </param>
	public static void Transition(string scene) => instance.StartCoroutine(LoadScene(scene));

	/// <summary> Loads scene asynchronously, will transition when ready </summary>
	/// <param name="scene"> Name of scene to load  </param>
	static IEnumerator LoadScene(string name, float minDuration = 3)
	{
		instance.duringLoad = true;
		Player.Instance.OnExitScene();

		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(name);
		asyncLoad.allowSceneActivation = false;

		float startTime = Time.time;
		bool inProgress = true;

		Material transitionMat = Player.Instance.mask.transitionMat;
		int _CutoffID = Shader.PropertyToID("_Cutoff");

		while (inProgress)
		{
			yield return null;
			float currentTime = Time.time - startTime;
			float loadProgress = Mathf.Min(asyncLoad.progress / .9f, currentTime / minDuration);
			transitionMat.SetFloat(_CutoffID, loadProgress * 2); // add curve here

			Player.Instance.TransitionUpdate();

			if (asyncLoad.progress >= .9f && currentTime > minDuration)
				inProgress = false;
		}
		asyncLoad.allowSceneActivation = true;
		Player.Instance.mask.transitionMat = null;
		instance.duringLoad = false;
	}

	public override void OnEnterScene()
	{
		World.Instance.name += $"[{levels[++sceneIndex]}]";
		Player.Instance.OnEnterScene();
	}

	/// <summary> Unloads scene asynchronously </summary>
	/// <param name="scene"> Name of scene to unload  </param>
	static IEnumerator UnloadScene(string name)
	{
		AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(name);
		while (!asyncUnload.isDone)
			yield return null;
	}

	public static void ReloadScene()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
}
