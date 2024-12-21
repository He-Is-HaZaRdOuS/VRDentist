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

    [Header("UI Canvas")]
    [SerializeField] private TextMeshProUGUI debugText;

    private int maxAeratorIndex = 0;
    private int activeAeratorIndex = 0;
    private Vector2 rotation;
    private Vector3 movementDirection;
    [SerializeField] private float toolMovementSpeed = 1f;
    public float triggerValue = 0.0f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        debugText.text = "Active Aerator: " + activeAeratorIndex;
    }

    void Update()
    {

    }

    void LateUpdate()
    {
        // === Tool Translation ===
        // Translate the tool in world coordinates to make it independent of its rotation
        aerators[activeAeratorIndex].transform.Translate(movementDirection * toolMovementSpeed * Time.deltaTime, Space.World);

        // === Tool Rotation ===
        // Rotate the tool around its local axes (X and Z) based on input
        aerators[activeAeratorIndex].transform.Rotate(rotation.y, 0, 0, Space.Self); // Rotate around X-axis
        aerators[activeAeratorIndex].transform.Rotate(0, 0, rotation.x, Space.Self); // Rotate around Z-axis
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
        movementDirection.y = context.ReadValue<Vector2>().y;
    }

    public void ToolRotationPower(InputAction.CallbackContext context)
    {
        triggerValue = context.ReadValue<float>();

        RumbleManager.instance.SetTriggerRumble(triggerValue / 5.0f);

    }

    public void CycleAerator(InputAction.CallbackContext context)
    {
        activeAeratorIndex = (activeAeratorIndex + 1) % maxAeratorIndex;
        debugText.text = "Active Aerator: " + activeAeratorIndex;
    }
}
