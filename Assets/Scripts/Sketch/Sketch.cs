using System.Collections;
using System.Collections.Generic;
using UnityEngine.Windows;
using UnityEngine;

public class Sketch : MonoBehaviour
{
    public GameObject brushPFblue;
    public GameObject brushPFyellow;
    public GameObject brushPFmagenta;
    public GameObject brushPFgreen;
    public GameObject brushPFcyan;
    public GameObject brushPFred;
    public GameObject brushPFwhite;
    public GameObject brushPFblack;

    private GameObject brushPF;
    private float brushSize = 0.1f;
    public RenderTexture RTexture;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButton(0))
        {
            var Ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            if(Physics.Raycast(Ray, out hit))
            {
                if (brushPF == null)
                {
                    Debug.Log("CHOOSE COLOUR");
                }
                else
                {
                    var go = Instantiate(brushPF, hit.point + Vector3.up * 0.1f, Quaternion.identity, transform);
                    go.transform.localScale = Vector3.one * brushSize;
                }
            }
        }
        
    }

    public void ChangeBrushSizeBig()
    {
        brushSize = 0.9f;
    }

    public void ChangeBrushSizeMedium()
    {
        brushSize = 0.4f;
    }

    public void ChangeBrushSizeSmall()
    {
        brushSize = 0.1f;
    }

    public void ChangeBrushSizeTiny()
    {
        brushSize = 0.01f;
    }

    public void ChangeBrushtoWater()
    {
        brushPF = brushPFblue;
    }

    public void ChangeBrushtoTemperateForest()
    {
        brushPF = brushPFgreen;
    }

    public void ChangeBrushtoHotDesert()
    {
        brushPF = brushPFyellow;
    }

    public void ChangeBrushtoAlpineTundra()
    {
        brushPF = brushPFmagenta;
    }

    public void ChangeBrushtoTaiga()
    {
        brushPF = brushPFblack;
    }

    public void ChangeBrushtoColdDesert()
    {
        brushPF = brushPFwhite;
    }

    public void ChangeBrushtoSavannah()
    {
        brushPF = brushPFred;
    }

    public void ChangeBrushtoTropicalForest()
    {
        brushPF = brushPFcyan;
    }

    public void Save()
    {
        StartCoroutine(CoSave());
    }

    private IEnumerator CoSave()
    {
        yield return new WaitForEndOfFrame();
        Debug.Log(Application.dataPath);
        RenderTexture.active = RTexture;

        var texture2d = new Texture2D(RTexture.width, RTexture.height);
        texture2d.ReadPixels(new Rect(0, 0, RTexture.width, RTexture.height), 0, 0);
        texture2d.Apply();

        var data = texture2d.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/savedSketch.png", data);
    }
}
