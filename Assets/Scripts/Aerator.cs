using Controllers;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public enum Handedness { None, Left, Right }

public class Aerator : MonoBehaviour
{
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    public Handedness holdingHand = Handedness.None;
    
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

    public void OnGrab(SelectEnterEventArgs args)
    {
        var controllerTag = args.interactorObject.transform.gameObject.tag;
        Debug.Log(args.interactorObject.transform.gameObject);

        Debug.Log($"Interactor Tag: {controllerTag}");
        if (controllerTag == "HandLeft")
        {
            holdingHand = Handedness.Left;
        }
        else if (controllerTag == "HandRight")
        {
            holdingHand = Handedness.Right;
        }
    }

    public void OnRelease()
    {
        holdingHand = Handedness.None;
    }
    
}
