using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;

public class Aerator : MonoBehaviour
{
    private void Awake()
    {
        if (ToolInputManager.instance != null)
        {
            ToolInputManager.instance.RegisterAerator(this);
        }
    }

    private void OnDestroy()
    {
        if (ToolInputManager.instance != null)
        {
            ToolInputManager.instance.UnregisterAerator(this);
        }
    }
    
}
