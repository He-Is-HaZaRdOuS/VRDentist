using System.Collections.Generic;
using UnityEngine.XR;

namespace Controllers
{
    public static class XRInput
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
    }
}
