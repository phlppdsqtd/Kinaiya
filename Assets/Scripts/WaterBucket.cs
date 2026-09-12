using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

public class WaterBucket : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform waterPrimitive;
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private GameObject pourUIPrompt; // Drag your UI Canvas here
    [SerializeField] private Transform bucketMesh; // Drag the child object containing the bucket mesh here

    [Header("Settings")]
    [SerializeField] private float fillDuration = 2f;
    [SerializeField] private float maxWaterScaleY = 0.5f;

    public bool IsFull { get; private set; } = false;
    private WaterTrough currentTrough = null;
    private bool isPouring = false;

    void Awake()
    {
        if (pourUIPrompt != null) pourUIPrompt.SetActive(false);
        if (grabInteractable == null) grabInteractable = GetComponent<XRGrabInteractable>();
        
        // Listen for the trigger button while holding the bucket
        grabInteractable.activated.AddListener(OnPourButtonPressed);
    }

    public void FillBucket()
    {
        if (!IsFull) StartCoroutine(ScaleWater(maxWaterScaleY, true));
    }

    // Called by the Trough when entering the collider
    public void SetPourZone(WaterTrough trough)
    {
        // Change !trough.HasWater to !trough.IsFull
        if (IsFull && !isPouring && trough != null && !trough.IsFull)
        {
            currentTrough = trough;
            if (pourUIPrompt != null) pourUIPrompt.SetActive(true);
        }
    }

    // Called by the Trough when leaving the collider
    public void ClearPourZone()
    {
        currentTrough = null;
        if (pourUIPrompt != null) pourUIPrompt.SetActive(false);
    }

    private void OnPourButtonPressed(ActivateEventArgs args)
    {
        if (IsFull && currentTrough != null && !isPouring)
        {
            StartCoroutine(PourRoutine());
        }
    }

    private IEnumerator PourRoutine()
    {
        isPouring = true;
        if (pourUIPrompt != null) pourUIPrompt.SetActive(false);

        // Tell the trough to start filling its own water primitive
        currentTrough.FillTrough();

        // 1. Tilt the Bucket model forward
        Quaternion startRot = bucketMesh.localRotation;
        Quaternion tiltRot = startRot * Quaternion.Euler(90f, 0f, 0f); // Tips bucket forward
        
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            bucketMesh.localRotation = Quaternion.Lerp(startRot, tiltRot, t / 0.5f);
            yield return null;
        }

        // 2. Empty the bucket water visual
        yield return StartCoroutine(ScaleWater(0f, false));

        // 3. Tilt the Bucket model back to normal
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            bucketMesh.localRotation = Quaternion.Lerp(tiltRot, startRot, t / 0.5f);
            yield return null;
        }

        isPouring = false;
        currentTrough = null; 
    }

    private IEnumerator ScaleWater(float targetY, bool setFullState)
    {
        Vector3 initialScale = waterPrimitive.localScale;
        Vector3 targetScale = new Vector3(initialScale.x, targetY, initialScale.z);
        float elapsedTime = 0f;

        while (elapsedTime < fillDuration)
        {
            float t = elapsedTime / fillDuration;
            waterPrimitive.localScale = Vector3.Lerp(initialScale, targetScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        waterPrimitive.localScale = targetScale;
        IsFull = setFullState;
    }

    void OnDestroy()
    {
        grabInteractable.activated.RemoveListener(OnPourButtonPressed);
    }
}