using UnityEngine;
using System.Collections; 
using UnityEngine.XR.Interaction.Toolkit.Interactables; 

public class CowLogic : MonoBehaviour
{
    [Header("Cow Hunger Stats")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float currentHunger = 50f;
    [SerializeField] private float hungerThreshold = 80f;
    [SerializeField] private float hungerDepletionRate = 0.5f;
    [SerializeField] private float eatDuration = 3f; 

    [Header("Cow Thirst Stats")]
    [SerializeField] private float maxThirst = 100f;
    [SerializeField] private float currentThirst = 50f;
    [SerializeField] private float thirstThreshold = 75f;
    [SerializeField] private float thirstDepletionRate = 0.8f;
    [SerializeField] private float drinkDuration = 3f;
    [SerializeField] private float thirstReplenishAmount = 40f; 
    [SerializeField] private float troughDepletionAmount = 25f; 

    [Header("Cow Health Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float healthDepletionRate = 1f;
    [SerializeField] private float healthRecoveryRate = 0.2f;

    [Header("Weight & Bloat System")]
    [SerializeField] private CowWeightController weightController;
    [SerializeField] private float malnourishedThreshold = 50f; // Hunger below this means 0 bloat
    [SerializeField] private float healthyBloatValue = 30f;     // Bloat value when hunger is at max
    [SerializeField] private float diseaseBloatValue = 100f;    // Fixed bloat when sick with bloat disease
    public bool hasBloatDisease = false; // Placeholder to trigger disease bloat

    [Header("Effects")]
    [SerializeField] private ParticleSystem munchParticles;
    [SerializeField] private AudioSource mooSound;

    private bool isInDirtyPen = false;

    // Disease Placeholders for Animation Logic
    public bool IsSick { get; private set; } = false;
    public bool IsLyingDown { get; private set; } = false;

    public float CurrentHunger => currentHunger;
    public float MaxHunger => maxHunger;
    public bool IsHungry => currentHunger < hungerThreshold;

    public float CurrentThirst => currentThirst;
    public float MaxThirst => maxThirst;
    public bool IsThirsty => currentThirst < thirstThreshold;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public bool IsEating { get; private set; } = false;
    public bool IsDrinking { get; private set; } = false;

    void Update()
    {
        // Stat depletion
        currentHunger -= hungerDepletionRate * Time.deltaTime;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);

        currentThirst -= thirstDepletionRate * Time.deltaTime;
        currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);

        if (isInDirtyPen)
            currentHealth -= healthDepletionRate * Time.deltaTime;
        else
            currentHealth += healthRecoveryRate * Time.deltaTime;
            
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Update visuals based on new stats
        UpdateWeightVisuals();
    }

    private void UpdateWeightVisuals()
    {
        if (weightController == null) return;

        // 1. Check for disease override first
        if (hasBloatDisease)
        {
            weightController.SetWeight(diseaseBloatValue);
            return;
        }

        // 2. Check for malnourishment
        if (currentHunger <= malnourishedThreshold)
        {
            weightController.SetWeight(0f);
        }
        else
        {
            // 3. Interpolate between 0 and healthyBloatValue based on current hunger
            float hungerRatio = (currentHunger - malnourishedThreshold) / (maxHunger - malnourishedThreshold);
            float targetBloat = Mathf.Lerp(0f, healthyBloatValue, hungerRatio);
            weightController.SetWeight(targetBloat);
        }
    }

    public void SetInDirtyPen(bool state) => isInDirtyPen = state;

    public void SetSickState(bool state) => IsSick = state;
    public void SetLyingDownState(bool state) => IsLyingDown = state;

    public void ConsumeFood(FeedItem food)
    {
        if (IsEating || food == null || food.isBeingEaten || !IsHungry) return;
        food.isBeingEaten = true;
        StartCoroutine(EatFoodRoutine(food));
    }

    private IEnumerator EatFoodRoutine(FeedItem food)
    {
        IsEating = true;
        if (munchParticles != null) munchParticles.Play();

        Vector3 initialScale = food.transform.localScale;
        Vector3 targetScale = new Vector3(initialScale.x, 0f, initialScale.z); 
        float elapsedTime = 0f;

        while (elapsedTime < eatDuration)
        {
            if (food == null) break;
            float t = elapsedTime / eatDuration;
            food.transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (food != null)
        {
            currentHunger += food.nutritionValue;
            currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
            if (mooSound != null) mooSound.Play();

            food.transform.SetParent(null);
            Destroy(food.gameObject);
        }
        IsEating = false;
    }

    public void ConsumeWater(WaterTrough trough)
    {
        if (IsDrinking || trough == null || !trough.HasWater || !IsThirsty) return;
        StartCoroutine(DrinkWaterRoutine(trough));
    }

    private IEnumerator DrinkWaterRoutine(WaterTrough trough)
    {
        IsDrinking = true;

        if (trough.TryDrinkWater(troughDepletionAmount)) 
        {
            float elapsedTime = 0f;
            while (elapsedTime < drinkDuration)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            currentThirst += thirstReplenishAmount; 
            currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);
            if (mooSound != null) mooSound.Play(); 
        }

        IsDrinking = false;
    }
}