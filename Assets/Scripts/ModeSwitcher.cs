using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public class XRModeSwitcher : MonoBehaviour
{
    public static XRModeSwitcher instance;
    
    public GameObject standardSetup; // Standard Setup
    public GameObject xrSetup;       // XR Setup
    [Header("ModeSwitcher")]
    [SerializeField] private bool xrEnabled;
    public bool isXRMode => xrEnabled;
    
    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    void Start()
    {
        /*if (isXRMode)
        {
            xrEnabled = !xrEnabled;
            SwitchToXR();
        }
        else
        {
            xrEnabled = !xrEnabled;
            SwitchTo2D();
        }
        xrEnabled = !xrEnabled;*/
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
            SwitchToXR();
        else if (Input.GetKeyDown(KeyCode.B))
            SwitchTo2D();
    }

    public void SwitchToXR()
    {
        if (xrEnabled) return;

        standardSetup.SetActive(false);
        xrSetup.SetActive(true);

        StartCoroutine(StartXR());
        xrEnabled = true;
    }

    public void SwitchTo2D()
    {
        if (!xrEnabled) return;

        StartCoroutine(StopXR(() =>
        {
            xrSetup.SetActive(false);
            standardSetup.SetActive(true);
            xrEnabled = false;
        }));
    }

    private IEnumerator StartXR()
    {
        XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
        if (XRGeneralSettings.Instance.Manager.activeLoader is null)
        {
            Debug.LogError("XR loader init failed.");
            yield break;
        }
        XRGeneralSettings.Instance.Manager.StartSubsystems();
    }

    private IEnumerator StopXR(System.Action onComplete)
    {
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        yield return null;
        onComplete?.Invoke();
    }
}
