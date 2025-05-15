using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Controllers
{
    public static class XRInputController
    {
        public static float GetLeftTriggerValue()
        {
            InputDevice leftHand = GetDevice(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller);
            return TryGetTriggerValue(leftHand);
        }

        public static float GetRightTriggerValue()
        {
            InputDevice rightHand = GetDevice(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller);
            return TryGetTriggerValue(rightHand);
        }
        
        public static bool GetRightAcceptButton()
        {
            var rightHand = GetDevice(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller);
            return TryGetBoolFeature(rightHand, CommonUsages.primary2DAxisClick);
        }

        public static bool GetDPadLeftButton()
        {
            var rightHand = GetDevice(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller);
            if (TryGetBoolFeature(rightHand, CommonUsages.primary2DAxisClick) &&
                TryGet2DAxisFeature(rightHand, CommonUsages.primary2DAxis, out Vector2 axis))
            {
                return axis.x < -0.5f;
            }
            return false;
        }

        public static bool GetDPadRightButton()
        {
            var rightHand = GetDevice(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller);
            if (TryGetBoolFeature(rightHand, CommonUsages.primary2DAxisClick) &&
                TryGet2DAxisFeature(rightHand, CommonUsages.primary2DAxis, out Vector2 axis))
            {
                return axis.x > 0.5f;
            }
            return false;
        }
        
        public static bool GetLeftMenuButton()
        {
            var leftHand = GetDevice(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller);
            return TryGetBoolFeature(leftHand, CommonUsages.menuButton);
        }

        public static bool GetRightMenuButton()
        {
            var rightHand = GetDevice(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller);
            return TryGetBoolFeature(rightHand, CommonUsages.menuButton);
        }

        private static InputDevice GetDevice(InputDeviceCharacteristics characteristics)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            return devices.Count > 0 ? devices[0] : default;
        }

        private static float TryGetTriggerValue(InputDevice device)
        {
            if (device.isValid && device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
                return triggerValue;
            return 0f;
        }
        
        private static bool TryGetBoolFeature(InputDevice device, InputFeatureUsage<bool> feature)
        {
            return device.isValid && device.TryGetFeatureValue(feature, out bool value) && value;
        }

        private static bool TryGet2DAxisFeature(InputDevice device, InputFeatureUsage<Vector2> feature, out Vector2 value)
        {
            value = Vector2.zero;
            return device.isValid && device.TryGetFeatureValue(feature, out value);
        }
    }
}
