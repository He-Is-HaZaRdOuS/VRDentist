using System;
using System.Linq;
using Controllers;
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
        }

        private void Start()
        {
        }
        
        private void OnEnable()
        {
            if (playerInput == null)
                playerInput = GetComponent<PlayerInput>();

            SetMode(InputMode.ToothSelector);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            /*Debug.Log(playerInput.currentActionMap.name);*/

            if (XRModeSwitcher.instance.isXRMode)
            {
                if (XRInput.GetLeftMenuButton())
                {
                    OnRestartScene();
                }
            }
        }

        public void OnToggleControlMode(InputAction.CallbackContext ctx)
        {
            // flip between maps
            if (playerInput.currentActionMap.name == cameraMap)
            {
                SetMode(InputMode.Tool);
                Cursor.lockState = CursorLockMode.None;
            }
            else if (playerInput.currentActionMap.name == toolMap  || playerInput.currentActionMap.name == "Global")
            {
                SetMode(InputMode.Camera);
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void OnRestartScene(InputAction.CallbackContext ctx)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        
        public void OnRestartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void SwitchActionMap(string map)
        {
            map += "Map";
            playerInput.SwitchCurrentActionMap(map);
            playerInput.actions.FindActionMap("Global").Enable();
            Debug.Log("Switching to " + map);
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
