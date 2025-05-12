using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace Managers
{
    public class RumbleManager : MonoBehaviour
    {
        public static RumbleManager instance;

        [Header("Rumble Settings")]
        [SerializeField] private float maxDuration = 1.0f;
        [SerializeField] private float minDuration = 0.1f;
        [SerializeField] private float smoothingSpeed = 10f;

        private float targetLowFreq = 0.0f;
        private float targetHighFreq = 0.0f;

        private Gamepad pad;
        private Coroutine activeRumbleCoroutine;

        [Header("XR Haptics")]
        public XRBaseController leftController;
        public XRBaseController rightController;

        [Header("Debug")]
        [SerializeField] private Vector2 currentRumble;
        [SerializeField] public Vector2 targetRumble;
        [SerializeField] private float currentDuration;

        private float collisionIntensity = 0.0f;
        private float collisionFadeTime = 0.0f;

        private void Awake()
        {
            if (instance == null)
                instance = this;
        }

        private void OnDestroy()
        {
            if (pad != null)
                pad.SetMotorSpeeds(0f, 0f);
        }

        void Update()
        {
            pad ??= Gamepad.current;

            if (pad != null)
            {
                currentRumble.x = Mathf.Lerp(currentRumble.x, targetLowFreq, Time.deltaTime * smoothingSpeed);
                currentRumble.y = Mathf.Lerp(currentRumble.y, targetHighFreq, Time.deltaTime * smoothingSpeed);

                if (targetLowFreq > 0f && collisionIntensity > 0f)
                {
                    collisionFadeTime += Time.deltaTime;
                    currentRumble.y = Mathf.Lerp(currentRumble.y, collisionIntensity, Time.deltaTime * smoothingSpeed);

                    if (collisionFadeTime > 1.0f)
                        collisionIntensity = Mathf.Lerp(collisionIntensity, 0.0f, Time.deltaTime * smoothingSpeed);
                }
                else
                {
                    currentRumble.y = Mathf.Lerp(currentRumble.y, 0.0f, Time.deltaTime * smoothingSpeed);
                }

                pad.SetMotorSpeeds(currentRumble.x, currentRumble.y);
            }
        }

        public void RumblePulse(float lowFreq, float highFreq, float duration, string hand = "none")
        {
            if (XRModeSwitcher.instance.isXRMode)
            {
                XRBaseController targetController = null;
                if (hand == "left") targetController = leftController;
                else if (hand == "right") targetController = rightController;

                if (targetController != null)
                {
                    float amp = Mathf.Max(lowFreq, highFreq);
                    targetController.SendHapticImpulse(amp, duration);
                }
            }
            else
            {
                pad = Gamepad.current;
                if (pad != null)
                {
                    targetLowFreq = lowFreq;
                    targetHighFreq = highFreq;

                    if (activeRumbleCoroutine != null)
                        StopCoroutine(activeRumbleCoroutine);

                    activeRumbleCoroutine = StartCoroutine(StopRumbleAfterTime(duration, pad));
                }
            }
        }

        private IEnumerator StopRumbleAfterTime(float duration, Gamepad pad)
        {
            yield return new WaitForSeconds(duration);
            targetLowFreq = 0.0f;
            targetHighFreq = 0.0f;
            yield return new WaitForSeconds(0.1f);
            currentRumble = Vector2.zero;
            pad.SetMotorSpeeds(0f, 0f);
        }

        public void TriggerDynamicRumble(float lowFreq, float highFreq, float intensity, string hand = "none")
        {
            float duration = Mathf.Lerp(minDuration, maxDuration, intensity);
            RumblePulse(lowFreq, highFreq, duration, hand);
        }

        public void SetTriggerRumble(float triggerValue)
        {
            targetLowFreq = Mathf.Clamp(triggerValue, 0.0f, 1.0f);
        }

        public void SetCollisionIntensity(float intensity)
        {
            collisionIntensity = Mathf.Clamp(intensity, 0.0f, 1.0f);
            collisionFadeTime = 0.0f;
        }
    }
}