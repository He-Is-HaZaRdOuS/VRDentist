using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace Managers
{
    public enum InputMode {Camera, Tool, ToothSelector}
    
    [RequireComponent(typeof(PlayerInput))]
    public class InputModeManager : MonoBehaviour
    {
        public static InputModeManager instance;
        public InputMode currentMode;

        [Header("References")] private PlayerInput playerInput;
        [SerializeField] public string cameraMap = "CameraMap";
        [SerializeField] public string toolMap = "ToolMap";
        [SerializeField] public string toothSelectorMap = "ToothSelectorMap";

        private void Awake()
        {
            if (instance == null) instance = this;
            
            playerInput = GetComponent<PlayerInput>();
            SetMode(InputMode.ToothSelector);
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void OnToggleControlMode(InputAction.CallbackContext ctx)
        {
            // flip between maps
            if (playerInput.currentActionMap.name == cameraMap)
            {
                SetMode(InputMode.Tool);
                Cursor.lockState = CursorLockMode.None;
            }
            else if (playerInput.currentActionMap.name == toolMap)
            {
                SetMode(InputMode.Camera);
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void OnRestartScene(InputAction.CallbackContext ctx)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void SwitchActionMap(string map)
        {
            map += "Map";
            playerInput.SwitchCurrentActionMap(map);
            playerInput.actions.FindActionMap("Global").Enable();
        }

        public InputMode GetCurrentMode()
        {
            return currentMode;
        }

        public void SetMode(InputMode mode)
        {
            currentMode = mode;
            SwitchActionMap(currentMode.ToString());
        }
    }
}
