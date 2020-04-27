using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class ClippableObject : MonoBehaviour
{
    public enum WorldType {Real, Heart};

    public WorldType worldType;

	[SerializeField] protected bool volumeless = default;
	[HideInInspector] public InteractableObject tiedInteractable;
	[HideInInspector] public bool isClipped;
	[HideInInspector] public GameObject uncutCopy;

    protected GameObject mirroredCopy;

    public CSG.Model CachedModel
	{
		get
		{
			if (model == null || transform.position != previousCutPosition)
			{
				model = new CSG.Model(initialMesh, transform);
				model.ConvertToWorld();
				previousCutPosition = transform.position;
			}

			return model;
		}
	}

	private int oldLayer;
	private Mesh initialMesh;
	private MeshFilter meshFilter;
	private CSG.Model model;
	private Vector3 previousCutPosition;

    private CSG.Model stagedModel;

	protected void Awake()
	{
		isClipped = false;

		if (!this.TryComponent(out meshFilter))
			meshFilter = gameObject.AddComponent<MeshFilter>();
		if (!this.TryComponent<MeshCollider>())
			gameObject.AddComponent<MeshCollider>();
		initialMesh = meshFilter.mesh;

		oldLayer = gameObject.layer;
	}

	public virtual bool IntersectsBound(CSG.Model bound)
	{
		return CachedModel.Intersects(bound, 0.001f);
	}

    public virtual void ClipWith(CSG.Model other)
    {
        isClipped = true;

        if (worldType == WorldType.Heart)
        {
            IntersectWith(other);
        }
        else if (worldType == WorldType.Real)
        {
            Subtract(other);
        }
    }

	public virtual void IntersectWith(CSG.Model other)
	{
		uncutCopy = Instantiate(gameObject, transform.position, transform.rotation, transform);
		uncutCopy.transform.localScale = Vector3.one;// since this will be a child of a duplicate of its transform, don't double apply the

        oldLayer = gameObject.layer;
		gameObject.layer = 9;

		if (!volumeless)
			meshFilter.mesh = CSG.Operations.Intersect(CachedModel, other, false, null);
		else
			meshFilter.mesh = CSG.Operations.ClipAToB(CachedModel, other, true, false, null);

		if (this.TryComponent(out MeshCollider col))
			col.sharedMesh = meshFilter.mesh;
	}

    public void Subtract(CSG.Model other, bool normalOverride = true)
    {
        if (!volumeless)
            meshFilter.mesh = CSG.Operations.Subtract(CachedModel, other, normalOverride);
        else
            meshFilter.mesh = CSG.Operations.ClipAToB(CachedModel, other, false, false, null);

        if (this.TryComponent(out MeshCollider col))
            col.sharedMesh = meshFilter.mesh;
    }

    public virtual void IntersectMirrored(CSG.Model other, Matrix4x4 reflectionMatrix)
    {
        isClipped = true;

        mirroredCopy = Instantiate(gameObject, transform.position, transform.rotation);
        mirroredCopy.transform.position = reflectionMatrix.MultiplyPoint(transform.position);
        mirroredCopy.transform.LookAt(mirroredCopy.transform.position + reflectionMatrix.MultiplyVector(mirroredCopy.transform.forward), reflectionMatrix.MultiplyVector(mirroredCopy.transform.up));

        if (!volumeless)
            mirroredCopy.GetComponent<MeshFilter>().mesh = CSG.Operations.Intersect(CachedModel, other, true, mirroredCopy.transform.worldToLocalMatrix * reflectionMatrix);
        else
            mirroredCopy.GetComponent<MeshFilter>().mesh = CSG.Operations.ClipAToB(CachedModel, other, true, true, mirroredCopy.transform.worldToLocalMatrix * reflectionMatrix);

        if (mirroredCopy.TryComponent(out MeshCollider col))
            col.sharedMesh = meshFilter.mesh;

        mirroredCopy.layer = 9;
    }

    public virtual void StageIntersectMirroredInPlace(CSG.Model other)
    {
        isClipped = true;

        CSG.Model model = CachedModel;
        stagedModel = model;

        if (!volumeless)
            stagedModel = CSG.Operations.Intersect(model, other, true);// * reflectionMatrix
        else
            stagedModel = CSG.Operations.ClipAToB(model, other, true, true);// * reflectionMatrix

        if (this.TryComponent(out MeshCollider col))
            col.sharedMesh = meshFilter.mesh;

        gameObject.layer = 9;
    }

    public virtual void ApplyIntersectMirroredInPlace(Matrix4x4 reflectionMatrix)
    {
        GetComponent<MeshFilter>().mesh = stagedModel.ToMesh(transform.worldToLocalMatrix * reflectionMatrix);
    }

    public virtual void Revert()
	{
        isClipped = false;

        if (worldType == WorldType.Real)
        {
		    meshFilter.mesh = initialMesh;
        }
        else
        {
            meshFilter.mesh = initialMesh;
		    gameObject.layer = oldLayer;

            if (uncutCopy)
            {
                uncutCopy.GetComponent<ClippableObject>().Revert();
                DestroyImmediate(uncutCopy);
            }
            if (mirroredCopy)
            {
                DestroyImmediate(mirroredCopy);
            }
        }

		if (this.TryComponent(out MeshCollider col))
			col.sharedMesh = meshFilter.mesh;
	}
}
