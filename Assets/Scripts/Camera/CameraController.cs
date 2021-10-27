using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject cameraTarget;
    public Camera orthoCam;

    public Camera FPSCam;

    public GameObject Player;

    void Start()
    {
        orthoCam.enabled = true;
        FPSCam.enabled = false;
        Player.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        ChangeSettingsOrtho();

        if (Input.GetKeyDown(KeyCode.K))
        {
            orthoCam.enabled = false;
            FPSCam.enabled = true;

            if(FPSCam.enabled == true)
            {
                ChangeSettingsFPS();
            }
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            orthoCam.enabled = false;
            FPSCam.enabled = false;
            Player.SetActive(true);
        }

        if(Input.GetKeyDown(KeyCode.O))
        {
            orthoCam.enabled = true;
            FPSCam.enabled = false;

            if (orthoCam.enabled == true)
            {
                ChangeSettingsOrtho();
            }
        }


        if (Input.GetKeyDown(KeyCode.P))
        {
            ScreenCapture.CaptureScreenshot("Screenshot.png", 1);
        }
    }

    public void ChangeSettingsOrtho()
    {
        GameObject imgReader = GameObject.Find("ImageReader");
        ReadImage readImage = imgReader.GetComponent<ReadImage>();

        int width = readImage.mapWidth;
        int height = readImage.mapHeight;

        if (width == 32 && height == 32)
        {
            orthoCam.orthographicSize = 27f;
            cameraTarget.transform.position = new Vector3(-25f, 21f, -25f);

        }
        else if (width == 64 && height == 64)
        {
            orthoCam.orthographicSize = 40f;
            cameraTarget.transform.position = new Vector3(-30f, 28f, -30f);
        }
        else if (width == 128 && height == 128)
        {
            orthoCam.orthographicSize = 60f;
            cameraTarget.transform.position = new Vector3(-65f, 55f, -65f);
        }
        else if (width == 256 && height == 256)
        {
            orthoCam.orthographicSize = 100f;
            cameraTarget.transform.position = new Vector3(-120f, 95f, -120f);
        }
        else if (width == 512 && height == 512)
        {
            orthoCam.orthographicSize = 200f;
            cameraTarget.transform.position = new Vector3(-200f, 160f, -200f);
        }
        else if (width == 1024 && height == 1024)
        {
            //orthoCam.orthographicSize = 400f;
            //cameraTarget.transform.position = new Vector3(-100f, 80f, -100f);
        }
    }

    public void ChangeSettingsFPS()
    {
        FPSCam.transform.position = new Vector3(0f, 10f, 0f);
    }

}
