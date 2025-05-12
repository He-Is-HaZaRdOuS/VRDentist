using UnityEngine;
using UnityEngine.InputSystem;

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
        private Quaternion defaultRotation;
        
        public float RightTriggerValue { get; private set; }
        public float LeftTriggerValue { get; private set; }

        private void Start()
        {
            defaultPosition = transform.localPosition;
            defaultRotation = transform.localRotation;
        }

        private void Update()
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
            Vector3 forward = transform.forward * movementDirection.z;
            Vector3 right = transform.right * movementDirection.x;

            // Move the camera horizontally
            Vector3 movement = (forward + right) * (moveSpeed * Time.deltaTime);
            transform.position += movement;

            // Apply vertical movement (Q/E for up/down)
            transform.Translate(Vector3.up * (movementDirection.y * moveSpeed * Time.deltaTime), Space.World);
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

        // Reset back to Level-Default position and rotation
        public void Reset(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            Debug.Log("Reset Camera");
            transform.SetLocalPositionAndRotation(defaultPosition, defaultRotation);
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
        
        public void IncrementMovementSpeed() => moveSpeed = Mathf.Clamp(moveSpeed + 0.5f, 0.5f, maxMoveSpeed);
        public void DecrementMovementSpeed() => moveSpeed = Mathf.Clamp(moveSpeed - 0.5f, 0.5f, maxMoveSpeed);
        public void IncrementSensitivity() => sensitivity = Mathf.Clamp(sensitivity + 5f, 1f, maxSensitivity);
        public void DecrementSensitivity() => sensitivity = Mathf.Clamp(sensitivity - 5f, 1f, maxSensitivity);
    }
}
