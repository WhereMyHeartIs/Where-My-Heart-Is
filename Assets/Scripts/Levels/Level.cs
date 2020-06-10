﻿using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "Level", menuName = "Levels/Level", order = 2)]
public class Level : ScriptableObject
{
	// string of dialogue
	public string dialogue;
	// level behaviors
	public List<LevelBehaviour> behaviours;
	[SerializeField, ReadOnly] string levelName = "";

#if UNITY_EDITOR
	// scene
	public UnityEditor.SceneAsset scene = null;

	// void OnValidate() => levelName = scene?.name ?? "";
	void OnValidate()
	{
		if (!scene)
		{
			// Debug.LogError($"Scene missing in {name}");
			return;
		}
		levelName = scene?.name ?? "";
	}
#endif

	public string Name { get => levelName; }

	public void StartBehaviors()
	{
		GameManager.Instance.dialogue.PlayScript(DialogueText.texts[levelName]);
		// GameManager.Instance.GetComponentInChildren<DialogueSystem>().PlayScript(DialogueText.texts[levelName]);
		// Debug.Log(DialogueText.texts[levelName]);

		foreach (LevelBehaviour behavior in behaviours)
			behavior.StartEvent.Invoke();
	}

	public void EndBehaviours()
	{
		foreach (LevelBehaviour behaviour in behaviours)
			behaviour.EndEvent.Invoke();
	}

}
