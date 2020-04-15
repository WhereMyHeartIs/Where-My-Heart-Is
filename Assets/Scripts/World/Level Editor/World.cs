using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class World : MonoBehaviour
{
	public static World Instance;

	public Transform heartWorldContainer;
	public Transform realWorldContainer;
	public Transform entangledWorldContainer;

	public enum WorldType { Real, Heart, Entangled }

	List<ClippableObject> heartClippables, realClippables;

	[HideInInspector] public List < (ClippableObject, WorldType) > clippables
	{
		get
		{
			List < (ClippableObject, WorldType) > objs = new List < (ClippableObject, WorldType) > ();
			foreach (ClippableObject c in heartClippables)
				objs.Add((c, WorldType.Heart));
			foreach (ClippableObject c in realClippables)
				objs.Add((c, WorldType.Real));

			// foreach (ClippableObject c in heartWorldContainer.GetComponentsInChildren<ClippableObject>())
			// 	objs.Add((c, WorldType.Heart));
			// foreach (ClippableObject c in realWorldContainer.GetComponentsInChildren<ClippableObject>())
			// 	objs.Add((c, WorldType.Real));
			// foreach (EntangledClippable e in entangledWorldContainer.GetComponentsInChildren<EntangledClippable>())
			// {
			// 	foreach (ClippableObject c in e.heartObject.GetComponentsInChildren<ClippableObject>())
			// 		objs.Add((c, WorldType.Heart));
			// 	foreach (ClippableObject c in e.realObject.GetComponentsInChildren<ClippableObject>())
			// 		objs.Add((c, WorldType.Real));
			// }
			return objs.OrderBy(pair => (pair.Item1.transform.position - Player.Instance.transform.position).sqrMagnitude).ToList();
		}
	}

	public ClippableObject[] GetHeartObjects()
	{
		return heartWorldContainer.GetComponentsInChildren<ClippableObject>(); // TODO: do these ever change?
	}

	public ClippableObject[] GetRealObjects()
	{
		return realWorldContainer.GetComponentsInChildren<ClippableObject>();
	}

	public ClippableObject[] GetEntangledObjects()
	{
		return entangledWorldContainer.GetComponentsInChildren<EntangledClippable>();
	}

	public void Awake() => Instance = this;

	public void Start()
	{
		heartWorldContainer = transform.Find("Heart World");
		realWorldContainer = transform.Find("Real World");
		entangledWorldContainer = GetComponentInChildren<EntangledObjectManager>().transform;

		ConfigureWorld("Heart", heartWorldContainer);
		ConfigureWorld("Real", realWorldContainer);

		heartClippables = heartWorldContainer.GetComponentsInChildren<ClippableObject>().ToList();
		realClippables = realWorldContainer.GetComponentsInChildren<ClippableObject>().ToList();
		foreach (EntangledClippable e in entangledWorldContainer.GetComponentsInChildren<EntangledClippable>())
		{
			heartClippables.AddRange(e.heartObject.GetComponentsInChildren<ClippableObject>());
			realClippables.AddRange(e.realObject.GetComponentsInChildren<ClippableObject>());
		}
	}

	/*public void Initialize()
	{
	    heartWorldContainer = transform.Find("Heart World");
	    realWorldContainer = transform.Find("Real World");
	    entangledWorldContainer = GetComponentInChildren<EntangledObjectManager>().transform;

	    ConfigureWorld("Heart", heartWorldContainer);
	    ConfigureWorld("Real", realWorldContainer);
	}*/

	/*/// <summary> configures children and related clippables, interactables </summary>
	public void OnExitScene()
	{
        Initialize();
	}*/

	/*/// <summary> removes refs and deletes current to pass singleton to next world </summary>
	public void OnEnterScene()
	{
		heartWorldContainer = null;
		realWorldContainer = null;
		entangledWorldContainer = null;
		instance = null;
		Destroy(gameObject);
	}*/

	private void ConfigureWorld(string layer, Transform worldContainer)
	{
		foreach (MeshFilter mf in worldContainer.GetComponentsInChildren<MeshFilter>())
		{
			mf.gameObject.layer = LayerMask.NameToLayer(layer);
			if (!mf.TryComponent(out MeshRenderer mr)) mr = mf.gameObject.AddComponent<MeshRenderer>();
			if (!mf.TryComponent<MeshCollider>()) mf.gameObject.AddComponent<MeshCollider>();
			if (!mf.TryComponent<ClippableObject>()) mf.gameObject.AddComponent<ClippableObject>();

			if (layer == "Heart")
				mr.material.SetInt("_Dissolve", 1);
		}

		// foreach (Transform child in worldContainer.transform)
		// {
		// 	// Debug.Log(child);
		// 	if (child.TryComponent<MeshFilter>())
		// 	{
		// 		child.gameObject.layer = LayerMask.NameToLayer(layer);
		// 		if (!child.TryComponent(out MeshRenderer mr)) mr = child.gameObject.AddComponent<MeshRenderer>();
		// 		if (!child.TryComponent<MeshCollider>()) child.gameObject.AddComponent<MeshCollider>();
		// 		if (!child.TryComponent<ClippableObject>()) child.gameObject.AddComponent<ClippableObject>();

		// 		if (layer == "Heart")
		// 			mr.material.SetInt("_Dissolve", 1);
		// 	}

		// 	ConfigureWorld(layer, child); // do this recursively to hit everything in the given world
		// }
	}

	public void ResetCut()
	{
		foreach (ClippableObject obj in GetComponentsInChildren<ClippableObject>())
			if (obj.isClipped) obj.Revert();

		// foreach (Transform child in heartWorldContainer)
		// 	foreach (ClippableObject obj in child.GetComponentsInChildren<ClippableObject>())
		// 		if (obj.isClipped) obj.Revert();

		// foreach (Transform child in realWorldContainer)
		// 	foreach (ClippableObject obj in child.GetComponentsInChildren<ClippableObject>())
		// 		if (obj.isClipped) obj.Revert();

		// foreach (Transform child in entangledWorldContainer)
		// 	foreach (ClippableObject obj in child.GetComponentsInChildren<ClippableObject>())
		// 		if (obj.isClipped) obj.Revert();
	}
}
