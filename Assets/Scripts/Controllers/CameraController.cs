using UnityEngine;
using UnityEngine.InputSystem;

namespace Controllers
{
    [RequireComponent(typeof(PlayerInput))]
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        public float sensitivity = 1f; // Mouse sensitivity
        public float moveSpeed = 2f; // Camera movement speed
        public float verticalMoveSpeed = 1f; // Vertical movement speed (for Q/E keys)

        private Vector2 lookInput; // Store mouse movement input
        private Vector2 movementDirection; // Store WSAD input
        private float xRotation = 0f; // Camera rotation around the x-axis
        private Vector3 verticalMovement = Vector3.zero; // Store vertical movement (Q/E adjustment)
        private Vector3 defaultPosition;
        private Quaternion defaultRotation;

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
            Vector3 forward = transform.forward * movementDirection.y;
            Vector3 right = transform.right * movementDirection.x;

            // Move the camera horizontally
            Vector3 movement = (forward + right) * (moveSpeed * Time.deltaTime);
            transform.position += movement;

            // Apply vertical movement (Q/E for up/down)
            transform.position += verticalMovement * Time.deltaTime;
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
            movementDirection = context.ReadValue<Vector2>();
        }

        public void YMovement(InputAction.CallbackContext context)
        {
            // Q -> Move down, E -> Move up
            verticalMovement = new Vector3(0, context.ReadValue<float>(), 0) * verticalMoveSpeed;

        }
    }
}
