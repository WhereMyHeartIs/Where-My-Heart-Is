using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[System.Serializable]
public class ApplyMask : MonoBehaviour
{
    ///<summary> External RenderTexture for Mask TODO: to be consumed </summary>
    //public static RenderTexture mask;
    [Header("Image Effect Materials")]
    public Material openWindowMat;
    public Material rippleMat;
	public Material screenMat;

    ///<summary> Reference to Heart World Cam, temp Mask Cam </summary>
    [HideInInspector] public Camera heartCam, maskCam, mainCam;

	[SerializeField] Shader transition = default;

	[HideInInspector] public Material transitionMat;

	///<summary> Generated RenderTexture for Heart World </summary>
	[HideInInspector]public RenderTexture heart;
    
    [Header("Image Effect Source Textures")]
	[SerializeField] Texture2D dissolveTexture = default;
	[SerializeField] Texture2D birdBackground = default;


	Texture2D curSave;
	int _HeartID;


    [Header("Ripple Behavior")]
    public float rippleLength;
    public float rippleTarget;
    public AnimationCurve rippleCurve;

    [HideInInspector] public RenderTexture mask;
    private Texture2D mask2D;

    private bool rippleInProgress;
    private float rippleStartTime;


    void Start()
	{
		_HeartID = Shader.PropertyToID("_Heart");

		// get ref to heart world cam and assign generated RenderTexture
		mainCam = GetComponent<Camera>();
		mainCam.depthTextureMode = mainCam.depthTextureMode | DepthTextureMode.DepthNormals | DepthTextureMode.Depth;
		heartCam = this.GetComponentOnlyInChildren<Camera>();
		heart = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.Default);
		heartCam.depthTextureMode = heartCam.depthTextureMode | DepthTextureMode.DepthNormals | DepthTextureMode.Depth;
		heart.name = "Heart World";
		heartCam.targetTexture = heart;

        Player.Instance.OnApplyCut += StartRipple;

        CreateMask();
	}

    public void CopyInto(ApplyMask target)
    {
        target.transition = this.transition;
        target.screenMat = this.screenMat;
        target.transitionMat = this.transitionMat;
        target.dissolveTexture = this.dissolveTexture;
    }

    public void StartRipple()
    {
        rippleInProgress = true;
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

            if (rippleInProgress == true)
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
        if (rippleInProgress)
        {
            //float t = () / rippleLength;
            if (Time.time - rippleStartTime < rippleLength)
            {
                if (Time.time - rippleStartTime > 0)
                {
                    rippleMat.SetFloat("_Offset", rippleCurve.Evaluate((Time.time - rippleStartTime)/rippleLength) * rippleTarget);
                }
            }
            else
            {
                rippleInProgress = false;
            }
        }
    }


    public void SetMask(RenderTexture nextMask)
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
