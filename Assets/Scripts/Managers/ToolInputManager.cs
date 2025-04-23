using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Managers
{
    public class ToolInputManager : MonoBehaviour
    {
        public static ToolInputManager instance;

        [Header("Tool Input")]
        [SerializeField] private List<Aerator> aerators = new List<Aerator>();

        [Header("Materials")]
        [SerializeField] private Material activeMaterial;
        [SerializeField] private Material defaultMaterial;

        [Header("Tool Movement")]
        [SerializeField] private float toolMovementSpeed = 1f;

        private Camera mainCamera;
        private List<Renderer> aeratorRenderers = new List<Renderer>();
        private int activeAeratorIndex = 0;
        private int prevActiveIndex = -1;
        private Vector2 rotation;
        private Vector3 movementDirection;
        public float RightTriggerValue { get; private set; }
        public float LeftTriggerValue { get; private set; }

        private void Awake()
        {
            if (instance == null) instance = this;
            mainCamera = Camera.main;
        }

        private void Start()
        {
            // Cache renderers
        
            // Cache renderers (on self or children)
            aeratorRenderers.Clear();
            foreach (var aer in aerators)
            {
                Renderer rend = aer.GetComponent<Renderer>();
                if (rend == null) rend = aer.GetComponentInChildren<Renderer>();
                aeratorRenderers.Add(rend);
            }

            // Initialize visuals
            UpdateActiveVisual();
            UpdateActiveVisual();
        }

        private void LateUpdate()
        {
            if (aerators.Count == 0) return;

            // Update active aerator material if changed
            if (prevActiveIndex != activeAeratorIndex)
            {
                UpdateActiveVisual();
            }

            // Movement aligned to camera XZ plane
            if (movementDirection.x != 0f || movementDirection.z != 0f)
            {
                Vector3 camF = mainCamera.transform.forward;
                Vector3 camR = mainCamera.transform.right;
                camF.y = 0; camR.y = 0;
                camF.Normalize(); camR.Normalize();

                Vector3 worldDir = camR * movementDirection.x + camF * movementDirection.z;
                aerators[activeAeratorIndex].transform.position += worldDir * (toolMovementSpeed * Time.deltaTime);
            }
            // Y-axis movement
            if (movementDirection.y != 0f)
            {
                aerators[activeAeratorIndex].transform.Translate(
                    Vector3.up * (movementDirection.y * toolMovementSpeed * Time.deltaTime),
                    Space.World);
            }

            // Tool rotation (local)
            var activeTrans = aerators[activeAeratorIndex].transform;
            activeTrans.Rotate(rotation.y * toolMovementSpeed, 0f, 0f, Space.Self);
            activeTrans.Rotate(0f, 0f, rotation.x * toolMovementSpeed, Space.Self);
        }

        private void UpdateActiveVisual()
        {
            // reset previous
            if (prevActiveIndex >= 0 && prevActiveIndex < aeratorRenderers.Count)
            {
                var prevR = aeratorRenderers[prevActiveIndex];
                if (prevR != null) prevR.material = defaultMaterial;
            }
            // set new
            var currentR = aeratorRenderers[activeAeratorIndex];
            if (currentR != null) currentR.material = activeMaterial;

            prevActiveIndex = activeAeratorIndex;
        }

        public void RegisterAerator(Aerator aerator)
        {
            if (!aerators.Contains(aerator))
            {
                aerators.Add(aerator);
                aeratorRenderers.Add(aerator.GetComponent<Renderer>());
            }
        }

        public void UnregisterAerator(Aerator aerator)
        {
            int idx = aerators.IndexOf(aerator);
            if (idx >= 0)
            {
                aerators.RemoveAt(idx);
                aeratorRenderers.RemoveAt(idx);
                if (activeAeratorIndex >= aerators.Count)
                    activeAeratorIndex = aerators.Count - 1;
            }
        }

        public void YawPitchRotation(InputAction.CallbackContext ctx)
        {
            rotation = ctx.ReadValue<Vector2>();
        }

        public void XZMovement(InputAction.CallbackContext ctx)
        {
            var v = ctx.ReadValue<Vector2>();
            movementDirection.x = v.x;
            movementDirection.z = v.y;
        }

        public void YMovement(InputAction.CallbackContext ctx)
        {
            movementDirection.y = ctx.ReadValue<float>();
        }

        public void ToolRotationPower(InputAction.CallbackContext ctx)
        {
            const float deadzone = 0.1f;
            float raw = ctx.ReadValue<float>();
            RightTriggerValue = Mathf.Abs(raw) > deadzone ? raw : 0f;
            RumbleManager.instance.SetTriggerRumble(RightTriggerValue / 5f);
        }

        public void ToolMovementSpeed(InputAction.CallbackContext ctx)
        {
            float raw = ctx.ReadValue<float>();
            LeftTriggerValue = 1f - raw;
            toolMovementSpeed = LeftTriggerValue;
        }

        public void DPad(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            var d = ctx.ReadValue<Vector2>();
            if (d.x > 0) CycleAeratorForward();
            else if (d.x < 0) CycleAeratorBackward();
            else if (d.y > 0) IncrementToolMovementSpeed();
            else if (d.y < 0) DecrementToolMovementSpeed();
        }

        public void CycleAeratorForward()
        {
            activeAeratorIndex = (activeAeratorIndex + 1) % aerators.Count;
        }
        public void CycleAeratorBackward()
        {
            activeAeratorIndex = (activeAeratorIndex - 1 + aerators.Count) % aerators.Count;
        }
        public void IncrementToolMovementSpeed() => toolMovementSpeed = Mathf.Clamp(toolMovementSpeed + 0.1f, 0.1f, 1f);
        public void DecrementToolMovementSpeed() => toolMovementSpeed = Mathf.Clamp(toolMovementSpeed - 0.1f, 0.1f, 1f);
    }
}
