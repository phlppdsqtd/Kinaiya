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

    [Header("Cow Health Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float healthDepletionRate = 1f;
    [SerializeField] private float healthRecoveryRate = 0.2f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem munchParticles;
    [SerializeField] private AudioSource mooSound;

    private bool isInDirtyPen = false;

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
        currentHunger -= hungerDepletionRate * Time.deltaTime;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);

        currentThirst -= thirstDepletionRate * Time.deltaTime;
        currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);

        if (isInDirtyPen)
            currentHealth -= healthDepletionRate * Time.deltaTime;
        else
            currentHealth += healthRecoveryRate * Time.deltaTime;
            
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public void SetInDirtyPen(bool state) => isInDirtyPen = state;

    public void ConsumeFood(FeedItem food)
    {
        if (food.isBeingEaten) return;
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

            // Safely detach from XR interactor hierarchy before destroying to prevent Assertion failure
            food.transform.SetParent(null);
            Destroy(food.gameObject);
        }
        IsEating = false;
    }

    public void ConsumeWater(WaterTrough trough)
    {
        if (IsDrinking || trough == null || !trough.HasWater) return;
        StartCoroutine(DrinkWaterRoutine(trough));
    }

    private IEnumerator DrinkWaterRoutine(WaterTrough trough)
    {
        IsDrinking = true;

        if (trough.TryDrinkWater(25f)) 
        {
            float elapsedTime = 0f;
            while (elapsedTime < drinkDuration)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            currentThirst += 40f; 
            currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);
            if (mooSound != null) mooSound.Play(); 
        }

        IsDrinking = false;
    }
}