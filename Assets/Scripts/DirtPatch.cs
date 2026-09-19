using UnityEngine;

public class DirtPatch : MonoBehaviour
{
    [Tooltip("How much scrubbing it takes to clean.")]
    public float dirtHealth = 100f;
    [Tooltip("How fast the dirt fades while sweeping.")]
    public float cleanSpeed = 40f;
    [Tooltip("Minimum brush speed required to clean.")]
    public float requiredScrubSpeed = 0.5f; 
    [Tooltip("Drag your floating dirty icon here.")]
    public GameObject dirtyIcon; 

    private Renderer[] dirtRenderers;
    private Color[][] originalColors; 

    void Start()
    {
        // Get ALL renderers on this object and any of its children
        dirtRenderers = GetComponentsInChildren<Renderer>();
        
        // Setup a 2D array to hold the colors for every material on every renderer
        originalColors = new Color[dirtRenderers.Length][];
        
        for (int i = 0; i < dirtRenderers.Length; i++)
        {
            originalColors[i] = new Color[dirtRenderers[i].materials.Length];
            for (int j = 0; j < dirtRenderers[i].materials.Length; j++)
            {
                originalColors[i][j] = dirtRenderers[i].materials[j].color;
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Brush"))
        {
            // Check if the brush is actually moving fast enough to count as scrubbing
            Rigidbody brushRb = other.GetComponentInParent<Rigidbody>();
            if (brushRb != null && brushRb.linearVelocity.magnitude < requiredScrubSpeed)
            {
                return; // Brush is moving too slowly, ignore
            }

            dirtHealth -= cleanSpeed * Time.deltaTime;
            float alphaRatio = dirtHealth / 100f;
            
            if (alphaRatio >= 0)
            {
                // Loop through every renderer and fade every material
                for (int i = 0; i < dirtRenderers.Length; i++)
                {
                    for (int j = 0; j < dirtRenderers[i].materials.Length; j++)
                    {
                        Color newColor = originalColors[i][j];
                        newColor.a = originalColors[i][j].a * alphaRatio;
                        dirtRenderers[i].materials[j].color = newColor;
                    }
                }
            }

            if (dirtHealth <= 0)
            {
                if (dirtyIcon != null)
                {
                    dirtyIcon.SetActive(false);
                }
                gameObject.SetActive(false); 
            }
        }
    }
}