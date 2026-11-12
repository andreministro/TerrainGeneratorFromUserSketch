using UnityEngine;
public class FPSCamMovement : MonoBehaviour
{
    [Min(1f)] public float speed = 40f;
    [SerializeField, Min(1f)] private float boostMultiplier = 3f;
    private Vector2 look;
    private Camera view;
    private TerrainManager terrain;

    private void Awake() => view = GetComponent<Camera>();
    internal void SetTerrain(TerrainManager manager) => terrain = manager;

    private void Update()
    {
        if (!view.enabled || terrain == null || terrain.IsBuilding || Cursor.lockState != CursorLockMode.Locked)
            return;

        look.x += Input.GetAxis("Mouse X") * 2f;
        look.y = Mathf.Clamp(look.y - Input.GetAxis("Mouse Y") * 2f, -89f, 89f);
        transform.localRotation = Quaternion.Euler(look.y, look.x, 0f);

        float vertical = Input.GetKey(KeyCode.Space) ? 1f : 0f;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            vertical -= 1f;

        Vector3 direction = transform.forward * Input.GetAxis("Vertical")
            + transform.right * Input.GetAxis("Horizontal") + Vector3.up * vertical;
        float boost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? boostMultiplier : 1f;
        transform.position += Vector3.ClampMagnitude(direction, 1f) * (speed * boost * terrain.VoxelSize * Time.deltaTime);
    }
}
