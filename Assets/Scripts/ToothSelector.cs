using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToothSelector : MonoBehaviour
{
    public static ToothSelector instance;

    [Header("Tooth Settings")]
    [SerializeField] private List<GameObject> toothObjects;
    [SerializeField] private Material highlightMat;
    [SerializeField] private Material transparentMat;
    [SerializeField] private float floatSpeed = 0.5f;
    [SerializeField] private float floatAmount = 0.05f;

    private struct ToothData
    {
        public Transform tf;
        public Vector3 initialLocalPos;
        public Quaternion initialLocalRot;
        public Material originalMaterial;
        public GameObject obj;
        public Tooth component;
    }

    private List<ToothData> cachedTeeth;
    private int currentIndex = 0;
    private int selectedIndex = -1;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        cachedTeeth = new List<ToothData>();

        foreach (var obj in toothObjects)
        {
            var tf = obj.transform;
            var renderer = obj.GetComponent<Renderer>();
            Material origMat = renderer ? renderer.material : null;
            var toothComponent = obj.GetComponent<Tooth>();

            if (toothComponent) toothComponent.enabled = false;

            cachedTeeth.Add(new ToothData
            {
                tf = tf,
                initialLocalPos = tf.localPosition,
                initialLocalRot = tf.localRotation,
                originalMaterial = origMat,
                obj = obj,
                component = toothComponent
            });
        }

        HighlightCurrentTooth();
    }

    private void Update()
    {
        if (InputModeManager.instance.GetCurrentMode() == InputMode.ToothSelector)
        {
            AnimateHover(cachedTeeth[currentIndex]);
        }
    }

    private void AnimateHover(ToothData tooth)
    {
        // Float only *above* original position
        float offsetY = Mathf.PingPong(Time.time * floatSpeed, floatAmount);
        Vector3 pos = tooth.initialLocalPos;
        pos.y += offsetY;
        tooth.tf.localPosition = pos;
    }

    private void HighlightCurrentTooth()
    {
        var tooth = cachedTeeth[currentIndex];
        var renderer = tooth.obj.GetComponent<Renderer>();
        if (renderer) renderer.material = highlightMat;
    }

    private void UnhighlightCurrentTooth()
    {
        var tooth = cachedTeeth[currentIndex];
        ResetTransform(tooth);
        RestoreMaterial(tooth);
    }

    private void SelectCurrentTooth()
    {
        selectedIndex = currentIndex;

        for (int i = 0; i < cachedTeeth.Count; i++)
        {
            var tooth = cachedTeeth[i];

            ResetTransform(tooth);

            var renderer = tooth.obj.GetComponent<Renderer>();
            if (renderer)
            {
                if (i == selectedIndex)
                    renderer.material = tooth.originalMaterial;
                else
                    renderer.material = transparentMat;
            }

            if (i == selectedIndex && tooth.component)
            {
                tooth.component.enabled = true;
            }
        }
    }

    private void ResetTransform(ToothData tooth)
    {
        tooth.tf.SetLocalPositionAndRotation(tooth.initialLocalPos, tooth.initialLocalRot);
    }

    private void RestoreMaterial(ToothData tooth)
    {
        var renderer = tooth.obj.GetComponent<Renderer>();
        if (renderer && tooth.originalMaterial)
        {
            renderer.material = tooth.originalMaterial;
        }
    }

    public void Select(InputAction.CallbackContext ctx)
    {
        SelectCurrentTooth();
        InputModeManager.instance.SetMode(InputMode.Camera);
    }
    
    public void DPad(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        var d = ctx.ReadValue<Vector2>();
        if (d.x > 0) CycleToothForward();
        else if (d.x < 0) CycleToothBackward();
    }

    private void CycleToothForward()
    {
        UnhighlightCurrentTooth();
        currentIndex = (currentIndex + 1) % cachedTeeth.Count;
        HighlightCurrentTooth();
    }
    
    private void CycleToothBackward()
    {
        UnhighlightCurrentTooth();
        currentIndex = (currentIndex - 1 + cachedTeeth.Count) % cachedTeeth.Count;
        HighlightCurrentTooth();
    }
}
