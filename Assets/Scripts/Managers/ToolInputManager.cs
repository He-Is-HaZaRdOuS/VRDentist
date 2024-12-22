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

    [Header("UI Canvas")]
    [SerializeField] private TextMeshProUGUI debugText;

    private int maxAeratorIndex = 0;
    private int activeAeratorIndex = 0;
    private Vector2 rotation;
    private Vector3 movementDirection;
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
        if (aerators.Count <= 0)
        {
            Debug.LogError("No aerators found in the scene!");
            return;
        }
        debugText.text = "Active Aerator: " + activeAeratorIndex;
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

    public void CycleAeratorForward(InputAction.CallbackContext context)
    {
        activeAeratorIndex = Mathf.Min(activeAeratorIndex + 1, maxAeratorIndex - 1);
        debugText.text = "Active Aerator: " + activeAeratorIndex;
    }

    public void CycleAeratorBackward(InputAction.CallbackContext context)
    {
        activeAeratorIndex = Mathf.Max(activeAeratorIndex - 1, 0);
        debugText.text = "Active Aerator: " + activeAeratorIndex;
    }
}
