using UnityEngine;

public enum FlightZoneType
{
    Front,
    Rear,
    Left,
    Right
}

public class CowFlightZoneSensor : MonoBehaviour
{
    public FlightZoneType zoneType;
    [SerializeField] private CowAI parentAI;

    private void Start()
    {
        if (parentAI == null)
        {
            parentAI = GetComponentInParent<CowAI>();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Ignore this cow's own colliders
        if (other.transform.root == transform.root) return;

        bool isPlayer = other.CompareTag("Player");
        bool isCow = other.CompareTag("Cow");

        if (isPlayer || isCow)
        {
            if (parentAI != null)
            {
                parentAI.ApplyContinuousPressure(zoneType, other.transform.position, isPlayer);
            }
        }
    }
}