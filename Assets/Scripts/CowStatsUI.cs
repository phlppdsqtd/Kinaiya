using UnityEngine;
using TMPro;

public class CowStatsUI : MonoBehaviour
{
    [SerializeField] private CowLogic cowLogic;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private GameObject uiPanel;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (uiPanel != null) uiPanel.SetActive(false); // Hidden by default
    }

    void Update()
    {
        if (cowLogic != null && statsText != null && uiPanel.activeSelf)
        {
            statsText.text = $"Hunger: {cowLogic.CurrentHunger:F0} / {cowLogic.MaxHunger:F0}";

            // Face the VR camera continuously
            if (mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            }
        }
    }

    // Call these from XR Simple Interactable hover events on the Cow
    public void ShowUI() => uiPanel.SetActive(true);
    public void HideUI() => uiPanel.SetActive(false);
}