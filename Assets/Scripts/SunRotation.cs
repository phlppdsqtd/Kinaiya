using UnityEngine;

public class SunRotation : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("Length of the daytime in minutes.")]
    [SerializeField] private float dayDurationMinutes = 4.0f;
    
    [Tooltip("Length of the nighttime in minutes.")]
    [SerializeField] private float nightDurationMinutes = 1.0f;

    [Header("Lighting Settings")]
    [SerializeField] private float maxSunIntensity = 1.5f;
    [SerializeField] private float maxSkyExposure = 0.0035f;
    [SerializeField] private float minSkyExposure = 0.00005f;

    private Light sunLight;
    private float originalExposure;
    private bool wasNight;

    void Start()
    {
        sunLight = GetComponent<Light>();
        
        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
        {
            originalExposure = RenderSettings.skybox.GetFloat("_Exposure");
        }
        
        wasNight = Vector3.Dot(transform.forward, Vector3.down) <= 0;
    }

    void Update()
    {
        bool isDaytime = Vector3.Dot(transform.forward, Vector3.down) > 0;
        
        float currentDurationSeconds = isDaytime ? (dayDurationMinutes * 60f) : (nightDurationMinutes * 60f);
        float currentRotationSpeed = 180f / currentDurationSeconds;

        transform.Rotate(Vector3.right * currentRotationSpeed * Time.deltaTime);

        float dayFactor = Mathf.Clamp01(Vector3.Dot(transform.forward, Vector3.down));

        if (dayFactor > 0 && wasNight)
        {
            wasNight = false; // The sun just rose
            
            WeatherManager weather = FindFirstObjectByType<WeatherManager>();
            if (weather != null) weather.GenerateDailyForecast();

            // TRIGGER DIRT SPAWNS FOR ALL PENS
            PenDirtSpawner[] allSpawners = FindObjectsByType<PenDirtSpawner>(FindObjectsSortMode.None);
            foreach (PenDirtSpawner spawner in allSpawners)
            {
                spawner.SpawnDailyDirt();
            }
        }
        else if (dayFactor <= 0 && !wasNight)
        {
            wasNight = true; // The sun just set
            
            // ---> FOR LATER: THIS IS WHERE YOU WILL TRIGGER YOUR STATS SPLASH SCREEN <---
            Debug.Log("Night has fallen. Level ending..."); 
        }

        if (sunLight != null)
        {
            sunLight.intensity = dayFactor * maxSunIntensity;
        }

        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
        {
            float currentExposure = Mathf.Lerp(minSkyExposure, maxSkyExposure, dayFactor);
            RenderSettings.skybox.SetFloat("_Exposure", currentExposure);
        }
    }

    void OnDestroy()
    {
        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
        {
            RenderSettings.skybox.SetFloat("_Exposure", originalExposure);
        }
    }
}