using UnityEngine;
using TMPro;

public class CowStatsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CowLogic cowLogic;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private GameObject uiPanel;      
    [SerializeField] private GameObject warningIcon;  

    [Header("Warning Settings")]
    [Tooltip("Show warning icon if Hunger, Thirst, or Health drops below this value.")]
    [SerializeField] private float warningThreshold = 50f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (uiPanel != null) uiPanel.SetActive(false); 
        if (warningIcon != null) warningIcon.SetActive(false);
    }

    void Update()
    {
        if (cowLogic == null) return;

        // 1. Check if any survival stat is critically low
        bool isLow = cowLogic.CurrentHunger < warningThreshold || 
                     cowLogic.CurrentThirst < warningThreshold || 
                     cowLogic.CurrentHealth < warningThreshold;

        if (warningIcon != null)
        {
            warningIcon.SetActive(isLow);
        }

        // 2. Update stats text to include Thirst
        if (uiPanel != null && uiPanel.activeSelf && statsText != null)
        {
            statsText.text = $"Hunger: {cowLogic.CurrentHunger:F0} / {cowLogic.MaxHunger:F0}\n" +
                             $"Thirst: {cowLogic.CurrentThirst:F0} / {cowLogic.MaxThirst:F0}\n" +
                             $"Health: {cowLogic.CurrentHealth:F0} / {cowLogic.MaxHealth:F0}";
        }

        // 3. Face the VR camera continuously if either UI element is active
        bool isAnyUIActive = (uiPanel != null && uiPanel.activeSelf) || isLow;
        if (mainCamera != null && isAnyUIActive)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        }
    }

    public void ShowUI() => uiPanel.SetActive(true);
    public void HideUI() => uiPanel.SetActive(false);
}

/*
using UnityEngine;
using TMPro;

public class CowStatsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CowLogic cowLogic;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private GameObject uiPanel;      // Main stats text box
    [SerializeField] private GameObject warningIcon;  // Red exclamation UI Image

    [Header("Warning Settings")]
    [Tooltip("Show warning icon if Hunger or Health drops below this value.")]
    [SerializeField] private float warningThreshold = 50f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (uiPanel != null) uiPanel.SetActive(false); // Hide text panel by default
        if (warningIcon != null) warningIcon.SetActive(false);
    }

    void Update()
    {
        if (cowLogic == null) return;

        // 1. Check if either stat is critically low
        bool isLow = cowLogic.CurrentHunger < warningThreshold || cowLogic.CurrentHealth < warningThreshold;

        if (warningIcon != null)
        {
            warningIcon.SetActive(isLow);
        }

        // 2. Update stats text if the inspector panel is toggled on via hover
        if (uiPanel != null && uiPanel.activeSelf && statsText != null)
        {
            statsText.text = $"Hunger: {cowLogic.CurrentHunger:F0} / {cowLogic.MaxHunger:F0}\n" +
                             $"Health: {cowLogic.CurrentHealth:F0} / {cowLogic.MaxHealth:F0}";
        }

        // 3. Face the VR camera continuously if either UI element is active
        bool isAnyUIActive = (uiPanel != null && uiPanel.activeSelf) || isLow;
        if (mainCamera != null && isAnyUIActive)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        }
    }

    // Called by XR Hover / Raycast events
    public void ShowUI() => uiPanel.SetActive(true);
    public void HideUI() => uiPanel.SetActive(false);
}
*/