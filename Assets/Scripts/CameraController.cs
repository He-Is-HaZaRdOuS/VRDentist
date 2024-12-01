using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float sensitivity = 0.1f; // Mouse sensitivity
    public float moveSpeed = 2f; // Camera movement speed
    private Vector2 lookInput; // Store mouse movement input
    private Vector2 movementDirection; // Store WSAD input
    private float xRotation = 0f; // Camera rotation around the x-axis

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    void Start()
    {

    }

    void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float yaw = lookInput.x * Time.deltaTime;
        float pitch = -lookInput.y * Time.deltaTime;

        // Update vertical angle and clamp it
        xRotation += pitch;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply rotations
        // Set the local X rotation explicitly using verticalAngle (clamped pitch)
        transform.localRotation = Quaternion.Euler(xRotation, transform.localRotation.eulerAngles.y, 0);

        // Apply yaw (horizontal rotation)
        transform.Rotate(0, yaw, 0, Space.World);

        // Calculate movement direction
        Vector3 forward = transform.forward * movementDirection.y;
        Vector3 right = transform.right * movementDirection.x;

        // Move the camera
        Vector3 movement = (forward + right) * moveSpeed * Time.deltaTime;
        transform.position += movement;
    }

    public void Look(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>() * sensitivity;
    }

    public void Lock(InputAction.CallbackContext context)
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        movementDirection = context.ReadValue<Vector2>();
    }
}
