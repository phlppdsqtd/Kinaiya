using UnityEngine;

public class CowLogic : MonoBehaviour
{
    [Header("Cow Stats")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float currentHunger = 50f;
    
    [Tooltip("Hunger level below which the cow starts searching for food.")]
    [SerializeField] private float hungerThreshold = 80f;

    [Tooltip("How much hunger the cow loses per second.")]
    [SerializeField] private float hungerDepletionRate = 0.5f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem munchParticles;
    [SerializeField] private AudioSource mooSound;

    public float CurrentHunger => currentHunger;
    public float MaxHunger => maxHunger;
    public bool IsHungry => currentHunger < hungerThreshold;

    void Update()
    {
        // Constantly deplete hunger over time
        currentHunger -= hungerDepletionRate * Time.deltaTime;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
    }

    public void ConsumeFood(FeedItem food)
    {
        if (food.isBeingEaten) return;
        food.isBeingEaten = true;

        currentHunger += food.nutritionValue;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        Debug.Log($"<color=cyan>[CowLogic] Chomp! Hunger is now {currentHunger:F1}/{maxHunger}</color>");

        if (munchParticles != null) munchParticles.Play();
        if (mooSound != null) mooSound.Play();

        Destroy(food.gameObject);
    }
}