using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowShard : MonoBehaviour
{
    private Vector2 direction;
    private AnimationCurve curve;
    private float distance;
    private float duration;
    private float startTime;
    private Vector3 initialPosition;

    private void Update()
    {
        //move
        Debug.Log(curve.Evaluate((Time.time - startTime) / duration));
        Debug.Log(direction);
        transform.position = initialPosition + Player.Instance.transform.localToWorldMatrix.MultiplyPoint(direction * curve.Evaluate((Time.time - startTime) / duration));
        if(Time.time - startTime > duration)
        {
            Debug.Log(gameObject + " done");
            Destroy(gameObject);
        }
    }

    public void Init(Color color, AnimationCurve _curve, Shader shader, float distance, float duration, Texture2D texture)
    {
        Debug.Log(gameObject + " :: " + curve);
        this.curve = _curve;
        //direction = new Vector2(color.r - 0.5f, color.g - 0.5f);
        direction = new Vector2(color.r, color.g);
        Material myMaterial = new Material(shader);
        GetComponent<MeshRenderer>().material = myMaterial;
        myMaterial.SetColor("_ColorMask", color);
        myMaterial.SetTexture("_MainTex", texture);
        this.distance = distance;
        this.duration = duration;
        startTime = Time.time;
        // get direction
        // create material
    }
}
