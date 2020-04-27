using UnityEngine;

[ExecuteInEditMode]
public class LevelInABox : MonoBehaviour
{
	void OnTransformChildrenChanged()
	{
		if (transform.childCount == 0)
			DestroyImmediate(gameObject);
	}
}
