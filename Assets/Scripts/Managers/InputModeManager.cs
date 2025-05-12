using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace Managers
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputModeManager : MonoBehaviour
    {
        public static InputModeManager instance;
        private Tooth activeTooth;

        [Header("References")] private PlayerInput playerInput;
        [SerializeField] private string cameraMap = "CameraMap";
        [SerializeField] private string toolMap = "ToolMap";

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            SwitchActionMap(cameraMap);
            Cursor.lockState = CursorLockMode.Locked;
            
            activeTooth = FindObjectOfType<Tooth>();
            if (activeTooth == null)
                Debug.LogError("Tooth not found in scene!");
        }

        public void OnToggleControlMode(InputAction.CallbackContext ctx)
        {
            // flip between maps
            if (playerInput.currentActionMap.name == cameraMap)
            {
                SwitchActionMap(toolMap);
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                SwitchActionMap(cameraMap);
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private void SwitchActionMap(string map)
        {
            playerInput.SwitchCurrentActionMap(map);
            playerInput.actions.FindActionMap("Global").Enable();
        }

        public void SaveCurrentToothState(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            activeTooth.SaveState();
        }
    }
}
