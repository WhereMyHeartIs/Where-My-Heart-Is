using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowShatter : MonoBehaviour
{
    public Shader shardShader;
    public bool breaking;
    public AnimationCurve curve;
    public GameObject window;

    private List<Transform> shards;

    private List<Color> uniqueColors;

    // Start is called before the first frame update
    void Start()
    {
        breaking = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(breaking)
        {
            // dispurse //actrually unneeded
        }

        if(Input.GetKeyDown(KeyCode.R))
        {
            Shatter();
        }
    }

    public void Shatter()
    {
        breaking = true;

        // count # of different colors
        //RenderTexture texture = new RenderTexture(Screen.width, Screen.height, 16);
        Texture2D texture = (Texture2D)window.GetComponent<MeshRenderer>().material.mainTexture;
        //Graphics.Blit(GetComponent<Material>().GetTexture("_MainTex"), texture);

        uniqueColors = new List<Color>();
        Color currentPixel;
        for(int i = 0; i < texture.width; i++)
        {
            for(int k = 0; k < texture.height; k++)
            {
                currentPixel = texture.GetPixel(i, k);

                if (!uniqueColors.Exists(color => color == currentPixel))
                {
                    uniqueColors.Add(currentPixel);
                }
            }
        }

        Debug.Log(uniqueColors.Count);
        uniqueColors.ForEach(color => Debug.Log(color));

        
        shards = new List<Transform>();
        GameObject createdObject;
        foreach(Color color in uniqueColors)
        {
            createdObject = Instantiate(window);
            createdObject.AddComponent<WindowShard>().Init(color, curve, shardShader, 2, 4, texture);
        }
        // create copies of window
        // set limited color shader on window, shattered peices

        // give each one a direction and velocity modifier
    }
}
