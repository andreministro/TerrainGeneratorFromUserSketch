using UnityEngine;
using System.Collections;
public class FPSCamMovement : MonoBehaviour
{
    Vector2 rot;
    public float speed = 2f;


    void Update()
    {
        rot = new Vector2(
            rot.x + Input.GetAxis("Mouse X") * 2,
            rot.y + Input.GetAxis("Mouse Y") * 2);

        transform.localRotation = Quaternion.AngleAxis(rot.x, Vector3.up);
        transform.localRotation *= Quaternion.AngleAxis(rot.y, Vector3.left);

        transform.position += transform.forward * speed * Input.GetAxis("Vertical");
        transform.position += transform.right * speed * Input.GetAxis("Horizontal");
    }
}
