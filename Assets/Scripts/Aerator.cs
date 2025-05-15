using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Managers;

public enum Handedness { None, Left, Right }

public class Aerator : MonoBehaviour
{
    private int index;
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    public Handedness holdingHand = Handedness.None;
    
    private void Awake()
    {
        if (ToolManager.instance != null)
        {
            ToolManager.instance.RegisterAerator(this);
        }
    }

    private void Start()
    {
        defaultPosition = transform.localPosition;
        defaultRotation = transform.localRotation;
    }

    private void OnDestroy()
    {
        if (ToolManager.instance != null)
        {
            ToolManager.instance.UnregisterAerator(this);
        }
    }
    
    // Reset back to Level-Default position and rotation
    public void Reset()
    {
        transform.SetLocalPositionAndRotation(defaultPosition, defaultRotation);
    }

    public void SetIndex(int newIndex)
    {
        index = newIndex;
    }

    public void OnGrab(SelectEnterEventArgs args)
    {
        var controllerTag = args.interactorObject.transform.gameObject.tag;
//        Debug.Log(args.interactorObject.transform.gameObject);

//        Debug.Log($"Interactor Tag: {controllerTag}");
        if (controllerTag == "HandLeft")
        {
            holdingHand = Handedness.Left;
            ToolManager.instance.SetActiveAerator(index);
        }
        else if (controllerTag == "HandRight")
        {
            holdingHand = Handedness.Right;
            ToolManager.instance.SetActiveAerator(index);
        }
    }

    public void OnRelease()
    {
        holdingHand = Handedness.None;
    }
    
}
