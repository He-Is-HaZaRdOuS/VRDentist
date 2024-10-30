using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToolController : MonoBehaviour
{
    private ToolInputActions inputActions;
    [SerializeField] private Camera mainCamera;
    private Vector2 mouseInput;
    private Vector2 WSAD;
    private Vector2 rotation;
    private Vector3 movementDirection;


    public float toolMovementSpeed = 1f;
    public float cameraMovementSpeed = 0.5f;
    public float cameraRotationSpeed = 4f;

    void Awake()
    {
        inputActions = new ToolInputActions();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            mouseInput.x = Input.GetAxis("Mouse X");
            mouseInput.y = Input.GetAxis("Mouse Y");
            WSAD.x = Input.GetAxis("Horizontal");
            WSAD.y = Input.GetAxis("Vertical");
        }
    }

    void LateUpdate()
    {
        // === Camera Movement ===
        // Translate the camera based on WSAD input in local coordinates
        Vector3 cameraMoveDirection = new Vector3(WSAD.x, 0, WSAD.y);
        mainCamera.transform.Translate(cameraMoveDirection * cameraMovementSpeed * Time.deltaTime, Space.Self);

        // === Camera Rotation ===
        // Get the camera's current pitch and yaw (up/down and left/right rotation)
        float pitch = -mouseInput.y * cameraRotationSpeed * Time.deltaTime; // invert for intuitive control
        float yaw = mouseInput.x * cameraRotationSpeed * Time.deltaTime;

        // Rotate the camera around its local X-axis for pitch (up/down rotation)
        mainCamera.transform.Rotate(pitch, 0, 0, Space.Self);

        // Rotate the camera around the world's Y-axis for yaw (left/right rotation)
        mainCamera.transform.Rotate(0, yaw, 0, Space.World);

        // === Tool Translation ===
        // Translate the tool in world coordinates to make it independent of its rotation
        transform.Translate(movementDirection * toolMovementSpeed * Time.deltaTime, Space.World);

        // === Tool Rotation ===
        // Rotate the tool around its local axes (X and Z) based on input
        transform.Rotate(rotation.y, 0, 0, Space.Self); // Rotate around X-axis
        transform.Rotate(0, 0, rotation.x, Space.Self); // Rotate around Z-axis
    }

    public void YawPitchRotation(InputAction.CallbackContext context)
    {
        Debug.Log("YawPitch");
        if (true)
        {
            rotation = context.ReadValue<Vector2>();
        }
    }

    public void XZMovement(InputAction.CallbackContext context)
    {
        Debug.Log("XZMovement");
        if (true)
        {
            movementDirection.x = context.ReadValue<Vector2>().x;
            movementDirection.z = context.ReadValue<Vector2>().y;
        }
    }

    public void YMovement(InputAction.CallbackContext context)
    {
        Debug.Log("YMovement");
        if (true)
        {
            movementDirection.y = context.ReadValue<Vector2>().y;
        }
    }

    public void ToolRotationSpeed(InputAction.CallbackContext context)
    {
        Debug.Log("ToolRotationSpeed");
    }

    public void kb(InputAction.CallbackContext context)
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
}
