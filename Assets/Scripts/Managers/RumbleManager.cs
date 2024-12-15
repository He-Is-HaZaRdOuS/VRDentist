using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RumbleManager : MonoBehaviour
{
    public static RumbleManager instance;
    private Gamepad pad;
    private Coroutine stopRumbleAfterTimeCoroutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void RumblePulse(float lowFres, float highFreq, float duration)
    {
        pad = Gamepad.current;
        if (pad != null)
        {
            pad.SetMotorSpeeds(lowFres, highFreq);
            stopRumbleAfterTimeCoroutine = StartCoroutine(StopRumbleAfterTime(duration, pad));
        }
    }

    private IEnumerator StopRumbleAfterTime(float duration, Gamepad pad)
    {
        yield return new WaitForSeconds(duration);
        pad.SetMotorSpeeds(0f, 0f);
    }
}
