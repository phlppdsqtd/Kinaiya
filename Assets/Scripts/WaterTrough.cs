using UnityEngine;
using System.Collections;

public class WaterTrough : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform troughWaterPrimitive;
    [SerializeField] private AudioSource splashAudio;

    [Header("Settings")]
    [SerializeField] private float fillDuration = 2f;
    [SerializeField] private float maxWaterScaleY = 1.0f;
    [SerializeField] private float maxCapacity = 100f;

    public float CurrentWater { get; private set; } = 0f;
    public bool IsFull => CurrentWater >= maxCapacity;
    public bool HasWater => CurrentWater > 0f;

    private Coroutine scaleCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<WaterBucket>(out WaterBucket bucket))
        {
            bucket.SetPourZone(this); 
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<WaterBucket>(out WaterBucket bucket))
        {
            bucket.ClearPourZone(); 
        }
    }

    // Called by the bucket
    public void FillTrough()
    {
        CurrentWater = maxCapacity;
        if (splashAudio != null) splashAudio.Play();
        
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleWater(maxWaterScaleY));
    }

    // Called by the CowLogic
    public bool TryDrinkWater(float amount)
    {
        if (CurrentWater <= 0f) return false;

        CurrentWater -= amount;
        CurrentWater = Mathf.Clamp(CurrentWater, 0f, maxCapacity);

        float targetY = (CurrentWater / maxCapacity) * maxWaterScaleY;
        
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleWater(targetY));

        return true;
    }

    private IEnumerator ScaleWater(float targetY)
    {
        Vector3 initialScale = troughWaterPrimitive.localScale;
        Vector3 targetScale = new Vector3(initialScale.x, targetY, initialScale.z);
        float elapsedTime = 0f;

        while (elapsedTime < fillDuration)
        {
            float t = elapsedTime / fillDuration;
            troughWaterPrimitive.localScale = Vector3.Lerp(initialScale, targetScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        troughWaterPrimitive.localScale = targetScale;
    }
}