using UnityEngine;

[ExecuteInEditMode]
public class SpawnPoint : MonoBehaviour
{
#if UNITY_EDITOR

	Transform player, cam;

	void OnEnable()
	{
		player = FindObjectOfType<Player>().transform; // TODO: not use find
		cam = player.GetComponentInChildren<Camera>().transform;
	}
	void Update()
	{
		if (Application.isEditor && !Application.isPlaying)
		{
			transform.position = player.position;
			transform.eulerAngles = new Vector3(cam.eulerAngles.x, player.eulerAngles.y, 0);
		}
		else
			Destroy(this);
	}
#endif
}
