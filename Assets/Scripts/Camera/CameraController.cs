using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    public GameObject cameraTarget;
    public Camera orthoCam;

    public Camera FPSCam;

    public GameObject Player;

    [SerializeField] private TerrainManager terrain;
    private float framedAspect;

    void Start()
    {
        orthoCam.enabled = true;
        FPSCam.enabled = false;
        Player.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            StartNewSketch();
            return;
        }

        if (terrain.IsBuilding)
            return;

        if (Input.GetKeyDown(KeyCode.K))
        {
            ToggleFlyCamera();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            orthoCam.enabled = false;
            FPSCam.enabled = false;
            Player.SetActive(true);
        }
        else if (Input.GetKeyDown(KeyCode.O))
        {
            ShowOverview();
        }
    }

    private void ToggleFlyCamera()
    {
        if (FPSCam.enabled)
        {
            ShowOverview();
            return;
        }

        orthoCam.enabled = false;
        Player.SetActive(false);
        FPSCam.enabled = true;
        ChangeSettingsFPS();
    }

    private void ShowOverview()
    {
        FPSCam.enabled = false;
        Player.SetActive(false);
        orthoCam.enabled = true;
        ChangeSettingsOrtho();
    }

    private void StartNewSketch()
    {
        SceneManager.LoadScene("SketchScene");
    }

    private void LateUpdate()
    {
        if (!terrain.IsBuilding && orthoCam.enabled && framedAspect != orthoCam.aspect)
            ChangeSettingsOrtho();
    }

    public void ChangeSettingsOrtho()
    {
        Bounds bounds = default;
        bool hasTerrain = false;
        foreach (var chunk in terrain.chunks.Values)
        {
            Mesh mesh = chunk.GetComponent<MeshFilter>().sharedMesh;
            if (chunk.update || mesh == null)
                return;
            if (mesh.vertexCount == 0)
                continue;

            Bounds chunkBounds = chunk.GetComponent<MeshRenderer>().bounds;
            if (hasTerrain)
                bounds.Encapsulate(chunkBounds);
            else
            {
                bounds = chunkBounds;
                hasTerrain = true;
            }
        }

        if (!hasTerrain)
            return;

        Transform view = orthoCam.transform;
        float halfWidth = ProjectExtent(bounds.extents, view.right);
        float halfHeight = ProjectExtent(bounds.extents, view.up);
        float halfDepth = ProjectExtent(bounds.extents, view.forward);
        Vector3 position = bounds.center - view.forward * (halfDepth + 10f);
        cameraTarget.transform.position += position - view.position;
        orthoCam.orthographicSize = Mathf.Max(halfHeight, halfWidth / orthoCam.aspect) * 1.1f + 0.5f;
        framedAspect = orthoCam.aspect;
    }

    private static float ProjectExtent(Vector3 extents, Vector3 axis)
    {
        return extents.x * Mathf.Abs(axis.x) + extents.y * Mathf.Abs(axis.y) + extents.z * Mathf.Abs(axis.z);
    }

    public void ChangeSettingsFPS()
    {
        FPSCam.transform.position = new Vector3(0f, 10f, 0f);
    }

}
