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

    public Clouds clouds;

    void Update()
    {
        Move();
        Rotate();
    }

    public void Move()
    {
        clouds.UpdateClouds();
        float horizontalMovement = Input.GetAxis("Horizontal");
        float verticalMovement = Input.GetAxis("Vertical");

        if (charController.isGrounded) verticalSpeed = 0;
        else verticalSpeed -= gravity * Time.deltaTime;

        Vector3 gravityMove = new Vector3(0, verticalSpeed, 0);

        if (Input.GetButtonDown("Jump") && charController.isGrounded)
        {
            verticalSpeed += Mathf.Sqrt(jumpHeight);
        }

        Vector3 move = transform.forward * verticalMovement + transform.right * horizontalMovement;
        charController.Move(speed * Time.deltaTime * move + gravityMove * Time.deltaTime);

       
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
