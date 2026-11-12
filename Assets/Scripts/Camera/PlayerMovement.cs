using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController charController;
    public float speed = 5;
    public Transform cameraHolder;
    public float mouseSensitivity = 2f;
    public float upLimit = -50;
    public float downLimit = 50;
    public float jumpHeight = 2f;

    private float gravity = 9.87f;
    private float verticalSpeed = 0;
    [SerializeField, Min(1f)] private float sprintMultiplier = 1.7f;
    private TerrainManager terrain;
    private bool waitingForGround;
    private int terrainVersion;

    public Clouds clouds;

    internal void SetTerrain(TerrainManager manager) => terrain = manager;

    internal void RespawnAt(Vector3 position)
    {
        Teleport(terrain.ClampToSketch(position));
        waitingForGround = true;
        verticalSpeed = 0f;
    }

    void Update()
    {
        if (terrain != null)
        {
            if (terrain.IsBuilding)
                return;

            if (terrainVersion != terrain.BuildVersion)
                waitingForGround = true;

            if (waitingForGround)
            {
                if (!terrain.TryGetGround(transform.position, out Vector3 ground))
                    return;

                float feetOffset = (charController.height * 0.5f - charController.center.y) * transform.lossyScale.y;
                Teleport(ground + Vector3.up * (feetOffset + charController.skinWidth));
                terrainVersion = terrain.BuildVersion;
                waitingForGround = false;
            }
            else if (terrain.IsBelowTerrain(transform.position))
            {
                RespawnAt(terrain.transform.position);
                return;
            }
            else if (!terrain.CanWalkTo(transform.position))
            {
                waitingForGround = true;
                verticalSpeed = 0f;
                return;
            }
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            return;
        Move();
        Rotate();
    }

    private void Teleport(Vector3 position)
    {
        charController.enabled = false;
        transform.position = position;
        charController.enabled = true;
    }

    public void Move()
    {
        clouds.UpdateClouds(transform.position);
        float horizontalMovement = Input.GetAxis("Horizontal");
        float verticalMovement = Input.GetAxis("Vertical");

        if (charController.isGrounded) verticalSpeed = 0;
        else verticalSpeed -= gravity * Time.deltaTime;

        Vector3 gravityMove = new Vector3(0, verticalSpeed, 0);

        if (Input.GetButtonDown("Jump") && charController.isGrounded)
        {
            verticalSpeed += Mathf.Sqrt(jumpHeight);
        }

        float sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? sprintMultiplier : 1f;
        Vector3 move = transform.forward * verticalMovement + transform.right * horizontalMovement;
        move = Vector3.ClampMagnitude(move, 1f) * (speed * sprint * Time.deltaTime);
        if (terrain != null && !terrain.CanWalkTo(transform.position + move))
            move = Vector3.zero;
        charController.Move(move + gravityMove * Time.deltaTime);

       
    }

    public void Rotate()
    {
        float horizontalRotation = Input.GetAxis("Mouse X");
        float verticalRotation = Input.GetAxis("Mouse Y");

        transform.Rotate(0, horizontalRotation * mouseSensitivity, 0);
        cameraHolder.Rotate(-verticalRotation * mouseSensitivity, 0, 0);

        Vector3 currentRotation = cameraHolder.localEulerAngles;

        if (currentRotation.x > 180) currentRotation.x -= 360;

        currentRotation.x = Mathf.Clamp(currentRotation.x, upLimit, downLimit);
        cameraHolder.localRotation = Quaternion.Euler(currentRotation);
    }

   
}
