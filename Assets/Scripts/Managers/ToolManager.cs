using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Controllers;

namespace Managers
{
    [RequireComponent(typeof(PlayerInput))]
    public class ToolManager : MonoBehaviour
    {
        public static ToolManager instance;

        [Header("Tool Input")]
        [SerializeField] private List<Aerator> aerators = new List<Aerator>();

        [Header("Materials")]
        [SerializeField] private Material activeMaterial;
        [SerializeField] private Material defaultMaterial;

        [Header("Tool Movement")]
        [SerializeField] private float toolMovementSpeed = 2.5f;
        [SerializeField] private float maxToolMovementSpeed = 5f;
        
        [SerializeField] private float movementSmoothTime = 0.05f; // For movement damping
        [SerializeField] private float rotationSmoothSpeed = 25; // For rotation damping
        
        [Header("Triggers")]
        [SerializeField] public float RightTriggerValue;
        [SerializeField] public float LeftTriggerValue;

        public Camera mainCamera;
        private List<Renderer> aeratorRenderers = new List<Renderer>();
        private int activeAeratorIndex = 0;
        private int prevActiveIndex = -1;
        private Vector2 rotation;
        private Vector3 movementDirection;
        
        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 targetPosition;
        private Quaternion cameraAlignedRotation;
        private Quaternion targetRotation;
        
        public Handedness CurrentHoldingHand = Handedness.None;

        private void Awake()
        {
            if (instance == null) instance = this;
        }

        private void OnEnable()
        {
            mainCamera = Camera.main;
        }

        private void Start()
        {
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
        }

        private void Update()
        {
            if (XRModeSwitcherManager.instance.isXRMode)
            {
                RightTriggerValue = XRInputController.GetRightTriggerValue();
                LeftTriggerValue = XRInputController.GetLeftTriggerValue();
                // TODO: Make `activeAeratorIndex` update when holding a valid aerator in XR mode. currently doesn't switch.
                CurrentHoldingHand = aerators.Count > 0 ? aerators[activeAeratorIndex].holdingHand : Handedness.None;

                if (XRInputController.GetRightMenuButton())
                {
                    Evaluate();
                }
            }
            
            targetRotation = aerators[activeAeratorIndex].transform.rotation;
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
            // Movement aligned to camera XZ plane
            Vector3 targetVelocity = Vector3.zero;

            if (movementDirection.x != 0f || movementDirection.z != 0f)
            {
                Vector3 camF = mainCamera.transform.forward;
                Vector3 camR = mainCamera.transform.right;
                camF.y = 0; camR.y = 0;
                camF.Normalize(); camR.Normalize();

                Vector3 worldDir = camR * movementDirection.x + camF * movementDirection.z;
                targetVelocity += worldDir;
            }

            // Y-axis movement
            if (movementDirection.y != 0f)
            {
                targetVelocity += Vector3.up * movementDirection.y;
            }

            // Apply smoothed movement
            targetVelocity = targetVelocity.normalized * toolMovementSpeed;
            Vector3 newPos = Vector3.SmoothDamp(
                aerators[activeAeratorIndex].transform.position,
                aerators[activeAeratorIndex].transform.position + targetVelocity * Time.deltaTime,
                ref currentVelocity,
                movementSmoothTime
            );

            aerators[activeAeratorIndex].transform.position = newPos;


            // Tool rotation
            var activeTrans = aerators[activeAeratorIndex].transform;

            if (rotation != Vector2.zero)
            {
                Vector3 camRight = mainCamera.transform.right; // X-axis (pitch)
                Vector3 camForward = mainCamera.transform.forward; // For Z-axis (roll)

                // Keep rotation on horizontal plane for Z-axis rotation
                camForward.y = 0f;
                camForward.Normalize();

                // Input: rotation.y = pitch (X), rotation.x = roll (Z)
                Quaternion deltaPitch = Quaternion.AngleAxis(rotation.y * toolMovementSpeed, camRight);
                Quaternion deltaRoll = Quaternion.AngleAxis(-rotation.x * toolMovementSpeed, camForward);

                // Only apply X (pitch) and Z (roll)
                targetRotation = deltaRoll * deltaPitch * targetRotation;
            }

            // Smooth toward target rotation
            activeTrans.rotation = Quaternion.Slerp(activeTrans.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
        }

        private void UpdateActiveVisual()
        {
            // reset previous
            if (prevActiveIndex >= 0 && prevActiveIndex < aeratorRenderers.Count)
            {
                var prevR = aeratorRenderers[prevActiveIndex];
                if (prevR is not null) prevR.material = defaultMaterial;
            }
            // set new
            var currentR = aeratorRenderers[activeAeratorIndex];
            if (currentR is not null) currentR.material = activeMaterial;

            prevActiveIndex = activeAeratorIndex;
        }

        public void SetActiveAerator(int index)
        {
            activeAeratorIndex = index;
        }

        public void RegisterAerator(Aerator aerator)
        {
            if (!aerators.Contains(aerator))
            {
                aerators.Add(aerator);
                aeratorRenderers.Add(aerator.GetComponent<Renderer>());
                activeAeratorIndex = aerators.IndexOf(aerator);
                aerator.SetIndex(activeAeratorIndex);
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

        public void RotationPower(InputAction.CallbackContext ctx)
        {
            float raw = ctx.ReadValue<float>();
            RightTriggerValue = raw;
            RumbleManager.instance.SetTriggerRumble(raw / 5f);
        }

        public void MovementSpeed(InputAction.CallbackContext ctx)
        {
            float raw = ctx.ReadValue<float>();
            LeftTriggerValue = 1f - raw;
            toolMovementSpeed = LeftTriggerValue * maxToolMovementSpeed;
        }

        public void Evaluate(InputAction.CallbackContext ctx)
        {
            Evaluate();
        }

        public void Evaluate()
        {
            var tooth = FindObjectsOfType<Tooth>().FirstOrDefault(t => t.isSelected);
            if (tooth is not null)
                tooth.Evaluate();
        }

        public void DPad(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            var d = ctx.ReadValue<Vector2>();
            if (d.x > 0) CycleAeratorForward();
            else if (d.x < 0) CycleAeratorBackward();
            else if (d.y > 0) IncrementMovementSpeed();
            else if (d.y < 0) DecrementMovementSpeed();
        }
        
        // Reset back to Level-Default position and rotation
        public void Reset(InputAction.CallbackContext ctx)
        {
            Debug.Log("Reset Aerator");
            aerators[activeAeratorIndex].Reset();
        }

        public void CycleAeratorForward()
        {
            activeAeratorIndex = (activeAeratorIndex + 1) % aerators.Count;
        }
        public void CycleAeratorBackward()
        {
            activeAeratorIndex = (activeAeratorIndex - 1 + aerators.Count) % aerators.Count;
        }
        public void IncrementMovementSpeed() => toolMovementSpeed = Mathf.Clamp(toolMovementSpeed + 0.1f, 0.1f, maxToolMovementSpeed);
        public void DecrementMovementSpeed() => toolMovementSpeed = Mathf.Clamp(toolMovementSpeed - 0.1f, 0.1f, maxToolMovementSpeed);
    }
}
