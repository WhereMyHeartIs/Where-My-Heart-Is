using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[System.Serializable]
public class ApplyMask : MonoBehaviour
{
    ///<summary> External RenderTexture for Mask TODO: to be consumed </summary>
    //public static RenderTexture mask;
    public Material openWindowMat;
    public Material rippleMat;

    ///<summary> Reference to Heart World Cam, temp Mask Cam </summary>
    [HideInInspector] public Camera heartCam, maskCam, mainCam;
	///<summary> Shader that combines views </summary>
	[SerializeField] Shader merge = default, transition = default;
	///<summary> Generated material for screen shader </summary>
	public Material screenMat;
	[HideInInspector] public Material transitionMat;
	///<summary> Gene()rated RenderTexture for Heart World </summary>
	public RenderTexture heart;
    
	[SerializeField] Texture2D dissolveTexture = default;
	[SerializeField] Texture2D hatchTexture = default;
	[SerializeField] Texture2D birdBackground = default;

    //Texture2D persistentMask;
	Texture2D curSave;
	int _HeartID;

    private bool rampMask;
    private bool ripple;
    public float rippleLength;
    private float rampValue;
    public RenderTexture rampResult;
    public RenderTexture mask;
    private float rampStartTime;
    private float rippleStartTime;
    public float rampLength;
    public float rampTimeOffset;
    public float breathMax;
    public float breathMin;
    private float currentBreath;
    public float breathRate;
    public bool breathIn;
    public float rampTarget;
    public float scrollRate;



    void Start()
	{
		screenMat = new Material(merge);
		_HeartID = Shader.PropertyToID("_Heart");

		// get ref to heart world cam and assign generated RenderTexture
		mainCam = GetComponent<Camera>();
		mainCam.depthTextureMode = mainCam.depthTextureMode | DepthTextureMode.DepthNormals | DepthTextureMode.Depth;
		heartCam = this.GetComponentOnlyInChildren<Camera>();
		heart = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.Default);
		heartCam.depthTextureMode = heartCam.depthTextureMode | DepthTextureMode.DepthNormals | DepthTextureMode.Depth;
		heart.name = "Heart World";
		heartCam.targetTexture = heart;

        rampMask = false;
        rampResult = new RenderTexture(Screen.width, Screen.height, 8, RenderTextureFormat.Default);

        breathIn = false;

        CreateMask();
		screenMat.SetTexture("_HatchTex", hatchTexture);
		screenMat.SetTexture("_Background", birdBackground);
		// screenMat.SetColor("_DepthOutlineColour", Color.white);
	}

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse1))
        {
            rampMask = true;
            rampValue = 0;
            rampStartTime = Time.time + rampTimeOffset;

            RenderTexture temp = Player.Instance.cam.targetTexture;

            Player.Instance.cam.targetTexture = rampResult;
            Player.Instance.cam.Render();
            Player.Instance.cam.targetTexture = temp;

            //ripple = true;
        }
    }

    public void CopyInto(ApplyMask target)
    {
        target.merge = this.merge;
        target.transition = this.transition;
        target.screenMat = this.screenMat;
        target.transitionMat = this.transitionMat;
        target.dissolveTexture = this.dissolveTexture;
    }

    public void StartRipple()
    {
        ripple = true;
        rippleStartTime = Time.time;
    }

	public void CreateMask()
	{
		mask = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.Default);//RenderTexture.GetTemporary(Screen.width, Screen.height, 16);
        mask.name = "Internal Mask";

		// spawn temp mask cam and configure transform
		maskCam = new GameObject("Mask Cam").AddComponent<Camera>();
		//(maskCam.transform.position, maskCam.transform.eulerAngles) = (Vector3.zero, Vector3.zero);
		maskCam.transform.parent = transform;
		(maskCam.transform.localPosition, maskCam.transform.localEulerAngles) = (Vector3.zero, Vector3.zero);
        maskCam.enabled = false;

		// configure mask Camera
		maskCam.cullingMask = 1 << LayerMask.NameToLayer("Mask");
		maskCam.clearFlags = CameraClearFlags.SolidColor;
		maskCam.backgroundColor = Color.clear;
        maskCam.targetTexture = mask;

        maskCam.Render();


        mask2D = new Texture2D(Screen.width, Screen.height);

        SetMask(mask);

        /*RenderTexture screen = RenderTexture.active;
		RenderTexture.active = mask;

        // copy to Texture2D and pass to shader
        Texture2D mask2D = new Texture2D(mask.width, mask.height);
		mask2D.ReadPixels(new Rect(0, 0, mask.width, mask.height), 0, 0);
		mask2D.Apply();
		Shader.SetGlobalTexture("_Mask", mask2D);//
		RenderTexture.active = screen;*/

        // remove temp cam
        maskCam.targetTexture = null;
        Destroy(maskCam.gameObject);
		//RenderTexture.ReleaseTemporary(mask);
	}

	void OnRenderImage(RenderTexture source, RenderTexture dest)
	{
		if (transitionMat == null)
		{ // pass both cameras to screen per render
			screenMat.SetTexture(_HeartID, heart);

            if (ripple == true)
            {
                RenderTexture temp = RenderTexture.GetTemporary(Screen.width, Screen.height, 16);
			    Graphics.Blit(source, temp, screenMat);
                Graphics.Blit(temp, dest, rippleMat);
                RenderTexture.ReleaseTemporary(temp);
            }
            else
            {
                Graphics.Blit(source, dest, screenMat);
            }
            // source.DiscardContents();
            // heart.DiscardContents();
            // source.Release();
            // heart.Release();
            // ClearRT(heart, heartCam);
            // ClearRT(source, mainCam);
        }
		else
		{
			Graphics.Blit(curSave, dest, transitionMat);
		}
	}

	void OnPreRender()
	{
        // ramp here
        if(rampMask)
        {
            float t = (Time.time - rampStartTime) / rampLength;
            if(t < 1.1)
            {
                if(t > 0)
                {
                    openWindowMat.SetFloat("_Cutoff", Mathf.Lerp(0, 2, t));
                    Graphics.Blit(mask, rampResult, openWindowMat);
                    SetMask(rampResult);
                }
            }
            else
            {
                rampMask = false;
                currentBreath = Mathf.Lerp(0, 2, t);
            }
        }
        else
        {
            currentBreath = Mathf.Lerp(currentBreath, (breathIn ? breathMax : breathMin), breathRate * Time.deltaTime);
            if (Mathf.Abs(currentBreath - (breathIn ? breathMax : breathMin)) < 0.1)
            {
                breathIn = !breathIn;
            }

            openWindowMat.SetTextureOffset("_RampTex", openWindowMat.GetTextureOffset("_RampTex") + Vector2.right * scrollRate * Time.deltaTime);

            openWindowMat.SetFloat("_Cutoff", currentBreath);
            Graphics.Blit(mask, rampResult, openWindowMat);
            SetMask(rampResult);
        }

        if (ripple)
        {
            float t = (Time.time - rippleStartTime) / rippleLength;
            if (t < 1.1)
            {
                if (t > 0)
                {
                    rippleMat.SetFloat("_Offset", Mathf.Lerp(0, 10, t));
                }
            }
            else
            {
                ripple = false;
            }
        }
        

        // GL.ClearWithSkybox(true, heartCam);
        // GL.ClearWithSkybox(true, mainCam);
    }

    private Texture2D mask2D;

    private void SetMask(RenderTexture nextMask)
    {
        RenderTexture screen = RenderTexture.active;
        RenderTexture.active = nextMask;

        // copy to Texture2D and pass to shader
        mask2D.ReadPixels(new Rect(0, 0, nextMask.width, nextMask.height), 0, 0);
        mask2D.Apply();
        Shader.SetGlobalTexture("_Mask", mask2D);
		RenderTexture.active = screen;
    }

	void ClearRT(RenderTexture r, Camera cam)
	{
		RenderTexture rt = UnityEngine.RenderTexture.active;
		UnityEngine.RenderTexture.active = r;
		GL.ClearWithSkybox(true, cam);
		// GL.Clear(true, true, Color.clear);
		UnityEngine.RenderTexture.active = rt;
	}

	public IEnumerator PreTransition(Texture2D preview, string scene)
	{
		yield return new WaitForEndOfFrame();
		curSave = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
		curSave.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
		curSave.Apply();
		transitionMat = new Material(transition);
		transitionMat.SetTexture("_BackgroundTex", preview);
		transitionMat.SetTexture("_TransitionTex", dissolveTexture);
		GameManager.Instance.ChangeLevel(scene);
	}
}
