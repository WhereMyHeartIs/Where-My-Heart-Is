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

	public List<ClippableObject> heartClippables, realClippables, mirrorClippables;

    [HideInInspector] public List<EntangledClippable> EntangledClippables
    {
        get
        {
            return entangledClippables.OrderBy(clippable => (clippable.transform.position - Player.Instance.transform.position).sqrMagnitude).ToList();
        }
    }

    private List<EntangledClippable> entangledClippables;

	[HideInInspector] public List <ClippableObject> Clippables
	{
        get
        {
            return clippables.OrderBy(clippable => (clippable.transform.position - Player.Instance.transform.position).sqrMagnitude).ToList();
		}
	}

    private List<ClippableObject> clippables;


    public ClippableObject[] GetEntangledObjects()
	{
		return entangledWorldContainer.GetComponentsInChildren<EntangledClippable>();
	}

	public void Awake() => Instance = this;

	public void Start()
	{
		heartWorldContainer = transform.Find("Heart World");
		realWorldContainer = transform.Find("Real World");
		entangledWorldContainer = GetComponentInChildren<EntangledObjectManager>().transform; // TODO: decouple EntangledObjectManager

		ConfigureWorld("Heart", heartWorldContainer);
		ConfigureWorld("Real", realWorldContainer);

		heartClippables = heartWorldContainer.GetComponentsInChildren<ClippableObject>().Where(clippable => !(clippable is Mirror)).ToList();
		realClippables = realWorldContainer.GetComponentsInChildren<ClippableObject>().Where(clippable => !(clippable is Mirror)).ToList();

        // get the mirror. Futureproofed in case we have more than one mirror or mirrors in both worlds
        mirrorClippables = realWorldContainer.GetComponentsInChildren<ClippableObject>().Where(clippable => clippable is Mirror).ToList();
        mirrorClippables.AddRange(heartWorldContainer.GetComponentsInChildren<ClippableObject>().Where(clippable => clippable is Mirror).ToList());

        entangledClippables = entangledWorldContainer.GetComponentsInChildren<EntangledClippable>().ToList();
        foreach (EntangledClippable entangled in entangledClippables)
		{
			heartClippables.AddRange(entangled.heartObject.GetComponentsInChildren<ClippableObject>());
			realClippables.AddRange(entangled.realObject.GetComponentsInChildren<ClippableObject>());

            foreach (ClippableObject clippable in entangled.heartObject.GetComponentsInChildren<ClippableObject>())
            {
                clippable.worldType = ClippableObject.WorldType.Heart;
            }

            foreach (ClippableObject clippable in entangled.realObject.GetComponentsInChildren<ClippableObject>())
            {
                clippable.worldType = ClippableObject.WorldType.Real;
            }
        }

        clippables = new List<ClippableObject>();
        clippables.AddRange(realClippables);
        clippables.AddRange(heartClippables);
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
		foreach (MeshFilter meshFilter in worldContainer.GetComponentsInChildren<MeshFilter>())
		{
			meshFilter.gameObject.layer = LayerMask.NameToLayer(layer);
			if (!meshFilter.TryComponent(out MeshRenderer meshRenderer)) meshRenderer = meshFilter.gameObject.AddComponent<MeshRenderer>();
			if (!meshFilter.TryComponent<MeshCollider>()) meshFilter.gameObject.AddComponent<MeshCollider>();
			if (!meshFilter.TryComponent<ClippableObject>()) meshFilter.gameObject.AddComponent<ClippableObject>();

            if (layer == "Heart") meshFilter.gameObject.GetComponent<ClippableObject>().worldType = ClippableObject.WorldType.Heart;
            else if (layer == "Real") meshFilter.gameObject.GetComponent<ClippableObject>().worldType = ClippableObject.WorldType.Real;
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
		foreach (ClippableObject clippable in clippables)
			if (clippable.isClipped) clippable.Revert();

        foreach (EntangledClippable entangled in GetComponentsInChildren<EntangledClippable>())
            if (entangled.isClipped) entangled.Revert();

        foreach (Mirror mirror in mirrorClippables)
            if (mirror.isClipped) mirror.Revert();

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
