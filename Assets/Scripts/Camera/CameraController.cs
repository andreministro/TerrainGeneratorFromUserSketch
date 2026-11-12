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
    private int framedBuildVersion;
    private int framedOverviewVersion;
    private float framedVoxelSize;
    private float viewVoxelSize = 1f;
    private float overviewFarClip;
    private float flyFarClip;
    private float playerFarClip;
    private Camera playerCamera;
    private CharacterController playerController;
    private PlayerMovement playerMovement;
    private bool hasFlown;

    void Start()
    {
        playerCamera = Player.GetComponentInChildren<Camera>(true);
        playerController = Player.GetComponent<CharacterController>();
        playerMovement = Player.GetComponent<PlayerMovement>();
        playerMovement.SetTerrain(terrain);
        FPSCam.GetComponent<FPSCamMovement>().SetTerrain(terrain);
        overviewFarClip = orthoCam.farClipPlane;
        flyFarClip = FPSCam.farClipPlane;
        playerFarClip = playerCamera.farClipPlane;
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorCaptured(false);
            return;
        }

        if (terrain.IsBuilding)
            return;

        if (Input.GetKeyDown(KeyCode.K))
        {
            ToggleFlyCamera();
        }
        else if (Input.GetKeyDown(KeyCode.L))
            ShowPlayer();
        else if (Input.GetKeyDown(KeyCode.O))
            ShowOverview();
        else if (Input.GetKeyDown(KeyCode.R))
        {
            if (FPSCam.enabled)
                ChangeSettingsFPS();
            else if (Player.activeSelf)
                playerMovement.RespawnAt(terrain.transform.position);
        }
        else if (!orthoCam.enabled && Input.GetMouseButtonDown(0))
            SetCursorCaptured(true);
    }

    private void ToggleFlyCamera()
    {
        if (FPSCam.enabled)
        {
            ShowOverview();
            return;
        }

        if (Player.activeSelf)
            FPSCam.transform.position = playerCamera.transform.position + Vector3.up * (4f * terrain.VoxelSize);
        else if (!hasFlown)
            ChangeSettingsFPS();

        hasFlown = true;
        orthoCam.enabled = false;
        Player.SetActive(false);
        FPSCam.enabled = true;
        terrain.SetExplorationTarget(FPSCam.transform);
        SetCursorCaptured(true);
    }

    private void ShowPlayer()
    {
        if (Player.activeSelf)
            return;

        Vector3 start = FPSCam.enabled ? FPSCam.transform.position : Player.transform.position;
        orthoCam.enabled = false;
        FPSCam.enabled = false;
        Player.SetActive(true);
        playerMovement.RespawnAt(start);
        terrain.SetExplorationTarget(Player.transform);
        SetCursorCaptured(true);
    }

    private void ShowOverview()
    {
        FPSCam.enabled = false;
        Player.SetActive(false);
        orthoCam.enabled = true;
        terrain.SetExplorationTarget(null);
        SetCursorCaptured(false);
        ChangeSettingsOrtho();
    }

    private void StartNewSketch()
    {
        SetCursorCaptured(false);
        SceneManager.LoadScene("SketchScene");
    }

    private void OnDisable() => SetCursorCaptured(false);

    private static void SetCursorCaptured(bool captured)
    {
        Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !captured;
    }

    internal string ControlHint
    {
        get
        {
            if (orthoCam.enabled)
                return "K Fly  ·  L Walk  ·  P New sketch";
            if (Cursor.lockState != CursorLockMode.Locked)
                return "Click to resume mouse look  ·  K Fly / Overview  ·  L Walk  ·  O Overview  ·  P New sketch";
            if (FPSCam.enabled)
                return "WASD Fly  ·  Space / Ctrl Up / Down  ·  Shift Boost  ·  R Reset  ·  Esc Release mouse\nK Overview  ·  L Walk  ·  P New sketch";
            return "WASD Walk  ·  Space Jump  ·  Shift Sprint  ·  R Respawn  ·  Esc Release mouse\nK Fly  ·  O Overview  ·  P New sketch";
        }
    }

    private void LateUpdate()
    {
        if (viewVoxelSize != terrain.VoxelSize)
            ResizeViews();

        if (!terrain.IsBuilding && orthoCam.enabled
            && (framedAspect != orthoCam.aspect || framedBuildVersion != terrain.BuildVersion
                || framedOverviewVersion != terrain.OverviewVersion
                || framedVoxelSize != terrain.VoxelSize))
            ChangeSettingsOrtho();
    }

    private void ResizeViews()
    {
        float ratio = terrain.VoxelSize / viewVoxelSize;
        Vector3 origin = terrain.transform.position;
        FPSCam.transform.position = origin + (FPSCam.transform.position - origin) * ratio;

        // Keep the player's feet at the corresponding point on the scaled terrain.
        Vector3 playerCenterToFeet = Player.transform.TransformVector(
            playerController.center - Vector3.up * (playerController.height * 0.5f));
        Vector3 feet = Player.transform.position + playerCenterToFeet;
        Player.transform.position = origin + (feet - origin) * ratio - playerCenterToFeet;

        viewVoxelSize = terrain.VoxelSize;
        float clipScale = Mathf.Max(1f, viewVoxelSize);
        FPSCam.farClipPlane = flyFarClip * clipScale;
        playerCamera.farClipPlane = playerFarClip * clipScale;
    }

    public void ChangeSettingsOrtho()
    {
        Transform view = orthoCam.transform;
        Vector3 minimum = Vector3.one * float.PositiveInfinity;
        Vector3 maximum = Vector3.one * float.NegativeInfinity;
        var overviewBounds = terrain.GetOverviewBounds();
        if (overviewBounds != null)
        {
            foreach (Bounds bounds in overviewBounds)
                Include(bounds);
        }
        else
        {
            foreach (var chunk in terrain.chunks.Values)
            {
                Mesh mesh = chunk.GetComponent<MeshFilter>().sharedMesh;
                if (chunk.update || mesh == null)
                    return;
                if (mesh.vertexCount == 0)
                    continue;

                Include(chunk.GetComponent<MeshRenderer>().bounds);
            }
        }

        framedAspect = orthoCam.aspect;
        framedBuildVersion = terrain.BuildVersion;
        framedOverviewVersion = terrain.OverviewVersion;
        framedVoxelSize = terrain.VoxelSize;
        if (float.IsPositiveInfinity(minimum.x))
            return;

        const float framingPadding = 1.035f;
        Vector3 center = (minimum + maximum) * 0.5f;
        Vector3 size = maximum - minimum;
        float clipPadding = Mathf.Max(orthoCam.nearClipPlane * 2f, size.z * 0.02f);
        Vector3 position = view.right * center.x + view.up * center.y + view.forward * (minimum.z - clipPadding);
        cameraTarget.transform.position += position - view.position;
        orthoCam.orthographicSize = Mathf.Max(size.y, size.x / orthoCam.aspect) * 0.5f * framingPadding;
        orthoCam.farClipPlane = Mathf.Max(overviewFarClip, size.z + clipPadding * 2f);

        void Include(Bounds bounds)
        {
            Vector3 projectedCenter = new Vector3(Vector3.Dot(bounds.center, view.right),
                Vector3.Dot(bounds.center, view.up), Vector3.Dot(bounds.center, view.forward));
            Vector3 projectedExtents = new Vector3(ProjectExtent(bounds.extents, view.right),
                ProjectExtent(bounds.extents, view.up), ProjectExtent(bounds.extents, view.forward));
            minimum = Vector3.Min(minimum, projectedCenter - projectedExtents);
            maximum = Vector3.Max(maximum, projectedCenter + projectedExtents);
        }
    }

    private static float ProjectExtent(Vector3 extents, Vector3 axis)
    {
        return extents.x * Mathf.Abs(axis.x) + extents.y * Mathf.Abs(axis.y) + extents.z * Mathf.Abs(axis.z);
    }

    public void ChangeSettingsFPS()
    {
        if (viewVoxelSize != terrain.VoxelSize)
            ResizeViews();
        FPSCam.transform.position = terrain.GetFlyStart();
    }

}
