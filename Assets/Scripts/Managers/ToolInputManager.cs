using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ToolInputManager : MonoBehaviour
{
    public static ToolInputManager instance;

    [Header("Tool Input")]
    [SerializeField] private List<Aerator> aerators;

    [Header("Materials")]
    [SerializeField] private Material activeMaterial; // Material for active aerator (green)
    [SerializeField] private Material defaultMaterial; // Material for inactive aerators (black)

    [Header("Tool Movement")]
    [SerializeField] private float toolMovementSpeed = 1f;

    private int maxAeratorIndex = 0;
    private int activeAeratorIndex = 0;
    private Vector2 rotation;
    private Vector3 movementDirection;
    public float RightTriggerValue = 0.0f;
    public float LeftTriggerValue = 0.0f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        if (aerators.Count <= 0)
        {
            Debug.LogError("No aerators found in the scene!");
            return;
        }
    }

    void Update()
    {

    }

    void LateUpdate()
    {
        if (aerators.Count <= 0)
        {
            return;
        }

        // === Tool Activation ===
        // Change the material of the active aerator to green and others to black
        for (int i = 0; i < maxAeratorIndex; i++)
        {
            Renderer renderer = aerators[i].GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = (i == activeAeratorIndex) ? activeMaterial : defaultMaterial;
            }
        }

        // === Tool Translation ===
        // Translate the tool in world coordinates to make it independent of its rotation
        aerators[activeAeratorIndex].transform.Translate(movementDirection * toolMovementSpeed * Time.deltaTime, Space.World);

        // === Tool Rotation ===
        // Rotate the tool around its local axes (X and Z) based on input
        aerators[activeAeratorIndex].transform.Rotate(rotation.y * toolMovementSpeed, 0, 0, Space.Self); // Rotate around X-axis
        aerators[activeAeratorIndex].transform.Rotate(0, 0, rotation.x * toolMovementSpeed, Space.Self); // Rotate around Z-axis
    }

    public void RegisterAerator(Aerator aerator)
    {
        if (!aerators.Contains(aerator))
        {
            aerators.Add(aerator);
            maxAeratorIndex = aerators.Count;
        }
    }

    public void UnregisterAerator(Aerator aerator)
    {
        if (aerators.Contains(aerator))
        {
            aerators.Remove(aerator);
            maxAeratorIndex = aerators.Count;
        }
    }

    public void SetMaxAeratorCount()
    {
        maxAeratorIndex = aerators.Count;
    }

    public void YawPitchRotation(InputAction.CallbackContext context)
    {
        rotation = context.ReadValue<Vector2>();
    }

    public void XZMovement(InputAction.CallbackContext context)
    {
        movementDirection.x = context.ReadValue<Vector2>().x;
        movementDirection.z = context.ReadValue<Vector2>().y;
    }

    public void YMovement(InputAction.CallbackContext context)
    {
        movementDirection.y = context.ReadValue<float>();
    }

    public void ToolRotationPower(InputAction.CallbackContext context)
    {
        float rawValue = context.ReadValue<float>();

        // Apply deadzone
        const float deadzoneThreshold = 0.1f;
        RightTriggerValue = Mathf.Abs(rawValue) > deadzoneThreshold ? rawValue : 0f;

        // Use the adjusted value for rumble
        RumbleManager.instance.SetTriggerRumble(RightTriggerValue / 5.0f);
    }

    public void ToolMovementSpeed(InputAction.CallbackContext context)
    {
        float rawValue = context.ReadValue<float>();

        // Invert
        LeftTriggerValue = 1.0f - rawValue;
        toolMovementSpeed = LeftTriggerValue;
    }

    public void DPad(InputAction.CallbackContext context)
    {
        Vector2 dPadValue = context.ReadValue<Vector2>();
        if (!context.performed)
        {
            return;
        }

        if (dPadValue.x > 0)
        {
            CycleAeratorForward();
        }
        else if (dPadValue.x < 0)
        {
            CycleAeratorBackward();
        }
        else if (dPadValue.y > 0)
        {
            IncrementToolMovementSpeed();
        }
        else if (dPadValue.y < 0)
        {
            DecrementToolMovementSpeed();
        }
    }

    public void CycleAeratorForward()
    {
        // Increment the index and wrap around to 0 if it exceeds the max index
        activeAeratorIndex = (activeAeratorIndex + 1) % maxAeratorIndex;
        Debug.Log("Active Aerator: " + activeAeratorIndex);
    }

    public void CycleAeratorBackward()
    {
        // Decrement the index and wrap around to the max index if it goes below 0
        activeAeratorIndex = (activeAeratorIndex - 1 + maxAeratorIndex) % maxAeratorIndex;
        Debug.Log("Active Aerator: " + activeAeratorIndex);
    }

    public void IncrementToolMovementSpeed()
    {
        toolMovementSpeed += 0.1f;
        toolMovementSpeed = Mathf.Clamp(toolMovementSpeed, 0.1f, 1.0f);
    }

    public void DecrementToolMovementSpeed()
    {
        toolMovementSpeed -= 0.1f;
        toolMovementSpeed = Mathf.Clamp(toolMovementSpeed, 0.1f, 1.0f);
    }
}
