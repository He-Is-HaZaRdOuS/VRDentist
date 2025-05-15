using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Managers;

namespace Controllers
{
    [RequireComponent(typeof(PlayerInput))]
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float maxSensitivity = 100f;
        public float sensitivity = 50f; // Mouse sensitivity
        [SerializeField] private float maxMoveSpeed = 10f;
        public float moveSpeed = 5f; // Camera movement speed

        private Vector2 lookInput; // Store mouse movement input
        private Vector3 movementDirection; // Store WSAD input
        private float xRotation; // Camera rotation around the x-axis
        private Vector3 defaultPosition;
        private Vector3 defaultRotation;
        
        [SerializeField] private float positionSmoothTime = 0.05f;
        private Vector3 currentVelocity; // For SmoothDamp movement
        private Vector3 targetPosition;  // Where we want to move
        
        [SerializeField] private float rotationSmoothTime = 0.05f;
        private float targetYaw;
        private float targetPitch;
        private float smoothYawVelocity;
        private float smoothPitchVelocity;
        
        public float RightTriggerValue { get; private set; }
        public float LeftTriggerValue { get; private set; }
        

        private void OnEnable()
        {
            defaultPosition = transform.localPosition;
            defaultRotation = transform.rotation.eulerAngles;
            xRotation = defaultRotation.x;
            
            targetPitch = defaultRotation.x;
            targetYaw = defaultRotation.y;
            targetPosition = transform.position;
            
            ToolManager.instance.mainCamera = Camera.main;
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            // Apply look input to target rotation values
            targetYaw += lookInput.x * Time.deltaTime;
            targetPitch -= lookInput.y * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch, -90f, 90f);

            // Smooth current rotation values
            xRotation = Mathf.SmoothDamp(xRotation, targetPitch, ref smoothPitchVelocity, rotationSmoothTime);
            float smoothYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref smoothYawVelocity, rotationSmoothTime);

            // Apply rotations
            transform.localRotation = Quaternion.Euler(xRotation, smoothYaw, 0f);

            // Compute movement vector in local camera space
            Vector3 forward = transform.forward * movementDirection.z;
            Vector3 right = transform.right * movementDirection.x;
            Vector3 vertical = Vector3.up * movementDirection.y;

            Vector3 rawMove = (forward + right + vertical) * (moveSpeed * Time.deltaTime);

            // Update target position
            targetPosition += rawMove;

            // Smoothly move camera to target position
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);
        }

        // Look method for mouse input
        public void Look(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>() * sensitivity;
        }

        // Lock and unlock the cursor
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

        // Move method for WASD input
        public void XZMovement(InputAction.CallbackContext context)
        {
            var v = context.ReadValue<Vector2>();
            movementDirection.x = v.x;
            movementDirection.z = v.y;
        }

        public void YMovement(InputAction.CallbackContext context)
        {
            // Q -> Move down, E -> Move up
            movementDirection.y = context.ReadValue<float>();
        }
        
        public void MovementSpeed(InputAction.CallbackContext ctx)
        {
            float raw = ctx.ReadValue<float>();
            LeftTriggerValue = 1f - raw;
            moveSpeed = LeftTriggerValue * maxMoveSpeed;
        }
        
        public void Sensitivity(InputAction.CallbackContext ctx)
        {
            float raw = ctx.ReadValue<float>();
            RightTriggerValue = 1f - raw;
            sensitivity = RightTriggerValue * maxSensitivity;
        }
        
        public void DPad(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            var d = ctx.ReadValue<Vector2>();
            if (d.x > 0) IncrementSensitivity();
            else if (d.x < 0) DecrementSensitivity();
            else if (d.y > 0) IncrementMovementSpeed();
            else if (d.y < 0) DecrementMovementSpeed();
        }
        
        // Reset back to Level-Default position and rotation
        public void Reset(InputAction.CallbackContext context)
        {
            Debug.Log("Reset Camera");
            transform.SetLocalPositionAndRotation(defaultPosition, Quaternion.Euler(defaultRotation));
            
            // Reset smoothing targets to avoid jitter
            targetPosition = defaultPosition;
            xRotation = defaultRotation.x;
            targetPitch = defaultRotation.x;
            targetYaw = defaultRotation.y;
            targetPosition = transform.position;
        }
        
        public void IncrementMovementSpeed() => moveSpeed = Mathf.Clamp(moveSpeed + 0.5f, 0.5f, maxMoveSpeed);
        public void DecrementMovementSpeed() => moveSpeed = Mathf.Clamp(moveSpeed - 0.5f, 0.5f, maxMoveSpeed);
        public void IncrementSensitivity() => sensitivity = Mathf.Clamp(sensitivity + 5f, 1f, maxSensitivity);
        public void DecrementSensitivity() => sensitivity = Mathf.Clamp(sensitivity - 5f, 1f, maxSensitivity);
    }
}
