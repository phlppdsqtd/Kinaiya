using System.Collections;
using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    [Header("Testing Overrides")]
    [Tooltip("Check this to force rain immediately for testing.")]
    [SerializeField] private bool forceRainOnStart = false;

    [Header("Weather Settings")]
    [Tooltip("How often (in seconds) the game checks to see if it should rain today.")]
    [SerializeField] private float checkIntervalSeconds = 30f;

    [Tooltip("Minimum duration of rain in seconds.")]
    [SerializeField] private float minRainDuration = 15f;

    [Tooltip("Maximum duration of rain in seconds.")]
    [SerializeField] private float maxRainDuration = 60f;

    [Header("References")]
    [SerializeField] private ParticleSystem rainParticles;

    private bool isRaining = false;
    private float todayRainChance = 0f;

    void Start()
    {
        if (rainParticles != null) rainParticles.Stop();

        if (forceRainOnStart)
        {
            StartCoroutine(StartRainRoutine(Random.Range(minRainDuration, maxRainDuration)));
        }

        GenerateDailyForecast();
        StartCoroutine(WeatherCheckLoop());
    }

    public void GenerateDailyForecast()
    {
        todayRainChance = Random.Range(0f, 100f);
        //Debug.Log($"[WeatherManager] New Day! Today's chance of rain is: {todayRainChance:F1}%");
    }

    private IEnumerator WeatherCheckLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkIntervalSeconds);

            if (!isRaining)
            {
                // 1. Roll the dice
                float roll = Random.Range(0f, 100f);
                
                // 2. Announce the check and the rolled number
                //Debug.Log($"[WeatherManager] Checking weather... Rolled {roll:F1} (Needs {todayRainChance:F1} or lower to rain).");

                // 3. Check for pass/fail
                if (roll <= todayRainChance || forceRainOnStart)
                {
                    //Debug.Log("<color=green>[WeatherManager] Check PASS! A storm is starting.</color>");
                    float duration = Random.Range(minRainDuration, maxRainDuration);
                    StartCoroutine(StartRainRoutine(duration));
                }
                else
                {
                    //Debug.Log("<color=yellow>[WeatherManager] Check FAIL. It stays sunny.</color>");
                }
            }
        }
    }

    private IEnumerator StartRainRoutine(float duration)
    {
        isRaining = true;
        if (rainParticles != null) rainParticles.Play();

        // Prints when it starts raining and how long it will last
        //Debug.Log($"[WeatherManager] It is raining! Rain will last for {duration:F1} seconds.");

        yield return new WaitForSeconds(duration);

        if (rainParticles != null) rainParticles.Stop();
        isRaining = false;

        // Prints when the rain stops
        //Debug.Log("[WeatherManager] The rain has stopped.");
    }
}