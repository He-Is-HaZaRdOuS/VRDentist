using System;
using System.Collections;
using System.Collections.Generic;
using Controllers;
using UnityEngine;

public class Aerator : MonoBehaviour
{
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private void Awake()
    {
        if (ToolInputManager.instance != null)
        {
            ToolInputManager.instance.RegisterAerator(this);
        }
    }

    private void Start()
    {
        defaultPosition = transform.localPosition;
        defaultRotation = transform.localRotation;
    }

    private void OnDestroy()
    {
        if (ToolInputManager.instance != null)
        {
            ToolInputManager.instance.UnregisterAerator(this);
        }
    }
    
    // Reset back to Level-Default position and rotation
    public void Reset()
    {
        transform.SetLocalPositionAndRotation(defaultPosition, defaultRotation);
    }
    
}
