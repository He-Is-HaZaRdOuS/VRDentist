using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToolController : MonoBehaviour
{
    private Vector2 rotation;
    private Vector3 movementDirection;


    public float toolMovementSpeed = 1f;
    public float cameraMovementSpeed = 0.5f;
    public float cameraRotationSpeed = 4f;

    void Awake()
    {
    }

    void Update()
    {

    }

    void LateUpdate()
    {
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
        // Debug.Log("YawPitch");
        if (true)
        {
            rotation = context.ReadValue<Vector2>();
        }
    }

    public void XZMovement(InputAction.CallbackContext context)
    {
        // Debug.Log("XZMovement");
        if (true)
        {
            movementDirection.x = context.ReadValue<Vector2>().x;
            movementDirection.z = context.ReadValue<Vector2>().y;
        }
    }

    public void YMovement(InputAction.CallbackContext context)
    {
        // Debug.Log("YMovement");
        if (true)
        {
            movementDirection.y = context.ReadValue<Vector2>().y;
        }
    }

    public void ToolRotationSpeed(InputAction.CallbackContext context)
    {
        // Debug.Log("ToolRotationSpeed");
    }
}
