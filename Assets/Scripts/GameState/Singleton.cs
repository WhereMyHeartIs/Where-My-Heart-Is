using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary> Generic Singleton implementation </summary>
public class Singleton<T> : MonoBehaviour where T : Component
{
	/// <summary> Local instance reference, to be used to child class </summary>
	protected static T instance;
	/// <summary> Public instance reference, to be used by external classes </summary>
	public static T Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindObjectOfType<T>();
				if (instance == null)
				{
					GameObject obj = new GameObject();
					obj.name = typeof(T).Name;
					instance = obj.AddComponent<T>();
					//spawn prefab
				}
			}
			return instance;
		}
	}

	public virtual void Awake()
	{
		if (instance == null || instance == this)
		{
			instance = this as T;
			DontDestroyOnLoad(this.gameObject);
		}
		else
			Destroy(gameObject);
	}

	public virtual void Initialize() { }
	public virtual void OnExitScene() { }
	public virtual void TransitionUpdate() { }
	public virtual void OnEnterScene() { }
}
