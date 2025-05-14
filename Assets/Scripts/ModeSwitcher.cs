using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
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
        if (isXRMode)
        {
            xrEnabled = !xrEnabled;
            SwitchToXR();
        }
        else
        {
            xrEnabled = !xrEnabled;
            SwitchTo2D();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
            SwitchToXR();
        else if (Input.GetKeyDown(KeyCode.C))
            SwitchTo2D();
    }
    
    private void SetXRInteractionManagers(bool enableXRManagerInXRSetup)
    {
        XRInteractionManager[] allManagers = FindObjectsOfType<XRInteractionManager>(true);

        foreach (var manager in allManagers)
        {
            // Enable only the XRInteractionManager inside xrSetup
            bool isInXRSetup = manager.transform.IsChildOf(xrSetup.transform);
            manager.enabled = (enableXRManagerInXRSetup && isInXRSetup);
        }
    }

    public void SwitchToXR()
    {
        if (xrEnabled) return;

        SetXRInteractionManagers(true);
        
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

            SetXRInteractionManagers(false);
            xrEnabled = false;
            
            // Restore main camera tag if XR changed it
            Camera main2DCam = standardSetup.GetComponentInChildren<Camera>();
            if (main2DCam is not null)
                main2DCam.tag = "MainCamera";
            
            MirrorScript[] mirrors = FindObjectsOfType<MirrorScript>();
            foreach (MirrorScript mirror in mirrors)
            {
                mirror.initCamera();
            }
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
        yield return null;
    }
    
    private IEnumerator StopXR(Action onComplete)
    {
        var manager = XRGeneralSettings.Instance.Manager;
        if (manager.activeLoader is not null)
        {
            manager.StopSubsystems();
            yield return null;
            manager.DeinitializeLoader();
            yield return new WaitUntil(() => manager.activeLoader is null);
        }
        else
        {
            Debug.LogWarning("XR loader not initialized; skipping stop/deinit.");
        }

        onComplete?.Invoke();
    }
}
