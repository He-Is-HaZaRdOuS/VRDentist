using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] public GameObject loadingWindow;
    [SerializeField] public GameObject evaluationWindow;
    [SerializeField] public Toggle carvedToggle;
    [SerializeField] public Toggle undercutToggle;
    [SerializeField] public Toggle marginToggle;
    [SerializeField] public Toggle positiveToggle;
    [SerializeField] public Button restartButton;

    public GameObject CarvedVisualizer { get; set; }
    public GameObject UndercutVisualizer { get; set; }
    public GameObject DistanceVisualizer { get; set; }

    [Header("Distance Visualizer Materials")] [SerializeField]
    public Material positiveMat;

    [SerializeField] public Material defaultMat;


    public void Awake()
    {
        Instance = this;
    }

    public void ShowLoadingWindow(string text)
    {
        loadingWindow.SetActive(true);
        loadingWindow.GetNamedChild("Text").GetComponent<TMP_Text>().text = text;
    }

    public void HideLoadingWindow()
    {
        loadingWindow.SetActive(false);
    }

    public void ShowEvaluationWindow()
    {
        carvedToggle.interactable = CarvedVisualizer != null;
        undercutToggle.interactable = UndercutVisualizer != null;
        marginToggle.interactable = DistanceVisualizer != null;
        positiveToggle.gameObject.SetActive(marginToggle.interactable);
        evaluationWindow.SetActive(true);
        EventSystem.current.SetSelectedGameObject(CarvedVisualizer != null
            ? carvedToggle.gameObject
            : restartButton.gameObject);
    }

    public void HideEvaluationWindow()
    {
        evaluationWindow.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnCarvedToggle(Toggle toggle)
    {
        if (CarvedVisualizer != null)
            CarvedVisualizer.SetActive(toggle.isOn);
    }

    public void OnUndercutToggle(Toggle toggle)
    {
        if (UndercutVisualizer != null)
            UndercutVisualizer.SetActive(toggle.isOn);
    }

    public void OnDistanceToggle(Toggle toggle)
    {
        if (DistanceVisualizer != null)
        {
            DistanceVisualizer.SetActive(toggle.isOn);
            positiveToggle.gameObject.SetActive(toggle.isOn);
        }
    }

    public void OnPositiveToggle(Toggle toggle)
    {
        if (DistanceVisualizer != null)
            DistanceVisualizer.GetComponent<MeshRenderer>().material = toggle.isOn ? positiveMat : defaultMat;
    }

    public void OnRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}