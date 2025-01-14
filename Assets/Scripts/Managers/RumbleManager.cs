using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RumbleManager : MonoBehaviour
{
    public static RumbleManager instance;

    [Header("Rumble Settings")]
    [SerializeField] private float maxDuration = 1.0f; // Maximum rumble duration
    [SerializeField] private float minDuration = 0.1f; // Minimum rumble duration
    [SerializeField] private float smoothingSpeed = 10f; // Speed of Lerp smoothing

    private float targetLowFreq = 0.0f; // Trigger-based low frequency rumble value
    private float targetHighFreq = 0.0f; // Collision-based high frequency rumble value

    private Gamepad pad;
    private Coroutine activeRumbleCoroutine;

    [Header("Debug")]
    [SerializeField] private Vector2 currentRumble; // Current motor speeds
    [SerializeField] public Vector2 targetRumble; // Target motor speeds
    [SerializeField] private float currentDuration; // Current rumble duration

    // Variable to store collision intensity
    private float collisionIntensity = 0.0f;
    private float collisionFadeTime = 0.0f; // Time since last collision update

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void OnDestroy()
    {
        if (pad != null)
            pad.SetMotorSpeeds(0f, 0f); // Stop rumble when the script is destroyed
    }

    void Update()
    {
        if (pad == null)
        {
            pad = Gamepad.current;
        }

        if (pad != null)
        {
            // Update rumble as long as the trigger is held (low frequency)
            if (targetLowFreq > 0.0f)
            {
                currentRumble.x = Mathf.Lerp(currentRumble.x, targetLowFreq, Time.deltaTime * smoothingSpeed);
            }
            else
            {
                // Smoothly fade out the rumble if the trigger is released
                currentRumble.x = Mathf.Lerp(currentRumble.x, 0.0f, Time.deltaTime * smoothingSpeed);
            }

            // Apply collision rumble only if collision intensity is set and the trigger rumble is active
            if (targetLowFreq > 0.0f && collisionIntensity > 0.0f)
            {
                // Smoothly fade collision rumble intensity to zero if no collision is detected
                collisionFadeTime += Time.deltaTime; // Track time since last collision
                currentRumble.y = Mathf.Lerp(currentRumble.y, collisionIntensity, Time.deltaTime * smoothingSpeed);

                // If collision intensity isn't updated for a while, gradually fade it to zero
                if (collisionFadeTime > 1.0f) // You can adjust this threshold
                {
                    collisionIntensity = Mathf.Lerp(collisionIntensity, 0.0f, Time.deltaTime * smoothingSpeed);
                }
            }
            else
            {
                // Smoothly reduce collision rumble if trigger rumble is not active
                currentRumble.y = Mathf.Lerp(currentRumble.y, 0.0f, Time.deltaTime * smoothingSpeed);
            }

            // Apply rumble to the gamepad
            pad.SetMotorSpeeds(currentRumble.x, currentRumble.y);
        }
    }

    public void RumblePulse(float lowFreq, float highFreq, float duration)
    {
        pad = Gamepad.current;
        if (pad != null)
        {
            // Set target rumble values (these will be gradually applied)
            targetLowFreq = lowFreq;
            targetHighFreq = highFreq;

            // Stop any active rumble coroutine and start a new one
            if (activeRumbleCoroutine != null)
            {
                StopCoroutine(activeRumbleCoroutine);
            }

            activeRumbleCoroutine = StartCoroutine(StopRumbleAfterTime(duration, pad));
        }
    }

    private IEnumerator StopRumbleAfterTime(float duration, Gamepad pad)
    {
        // Hold the rumble for the set duration
        yield return new WaitForSeconds(duration);

        // Smoothly transition to zero rumble after the duration
        targetLowFreq = 0.0f;
        targetHighFreq = 0.0f;

        // Allow a small time for smoothing before fully stopping
        yield return new WaitForSeconds(0.1f);

        currentRumble = Vector2.zero;
        pad.SetMotorSpeeds(0f, 0f);
    }

    public void TriggerDynamicRumble(float lowFreq, float highFreq, float intensity)
    {
        // Dynamically calculate duration based on intensity
        float duration = Mathf.Lerp(minDuration, maxDuration, intensity);

        // Call the existing RumblePulse function
        RumblePulse(lowFreq, highFreq, duration);
    }

    public void SetTriggerRumble(float triggerValue)
    {
        // Map the trigger value to a rumble intensity
        targetLowFreq = Mathf.Clamp(triggerValue, 0.0f, 1.0f) * 1.0f; // You can adjust the multiplier if necessary
    }

    // Set the collision intensity (this is a separate variable)
    public void SetCollisionIntensity(float intensity)
    {
        collisionIntensity = Mathf.Clamp(intensity, 0.0f, 1.0f);
    }
}
