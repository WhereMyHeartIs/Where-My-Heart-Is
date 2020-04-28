using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowMaskAnimation : MonoBehaviour
{
    [Header("Open Window Material")]
    public Material openMat;

    [Header("Open Window Behavior")]
    public float rampLength;
    public float rampTimeOffset;
    public float rampTarget;
    public AnimationCurve rampCurve;

    [Header("Breath Effect Behavior")]
    public float breathMax;
    public float breathMin;
    public float breathRate;
    private bool breathIn;

    [Header("Misc")]
    public float scrollRate;

    private bool openingWindow;
    private float currentBreath;
    private float rampStartTime;

    private ApplyMask applyMask;
    [HideInInspector] public RenderTexture rampResult;

    // Start is called before the first frame update
    void Start()
    {
        rampResult = new RenderTexture(Screen.width, Screen.height, 8, RenderTextureFormat.Default);
        breathIn = false;
        applyMask = GetComponent<ApplyMask>();
        Player.Instance.OnOpenWindow += BeginAnimation;
    }

    private void BeginAnimation()
    {
        openingWindow = true;
        rampStartTime = Time.time + rampTimeOffset;
        openMat.SetTextureOffset("_RampTex", new Vector2(Random.value, Random.value));
    }


    private void OnPreRender()
    {
        if (openingWindow)
        {
            if (Time.time - rampStartTime < rampLength)
            {
                if (Time.time - rampStartTime > 0)
                {
                    
                    //openMat.SetFloat("_Cutoff", ConcreteEaseMethods.QuadEaseOut(Time.time - rampStartTime, 0, rampTarget, rampLength));
                    openMat.SetFloat("_Cutoff", rampCurve.Evaluate(Time.time - rampStartTime/rampLength) * rampTarget);
                    Graphics.Blit(applyMask.mask, rampResult, openMat);
                    applyMask.SetMask(rampResult);
                }
            }
            else
            {
                openingWindow = false;
                //currentBreath = ConcreteEaseMethods.QuadEaseOut(Time.time - rampStartTime, 0, rampTarget, rampLength);
                currentBreath = rampCurve.Evaluate(Time.time - rampStartTime / rampLength) * rampTarget;
            }
        }
        else
        {
            currentBreath = Mathf.Lerp(currentBreath, (breathIn ? breathMax : breathMin), breathRate * Time.deltaTime);
            if (Mathf.Abs(currentBreath - (breathIn ? breathMax : breathMin)) < 0.1)
            {
                breathIn = !breathIn;
            }

            openMat.SetTextureOffset("_RampTex", openMat.GetTextureOffset("_RampTex") + Vector2.right * scrollRate * Time.deltaTime);

            openMat.SetFloat("_Cutoff", currentBreath);
            Graphics.Blit(applyMask.mask, rampResult, openMat);
            applyMask.SetMask(rampResult);
        }
    }
}
