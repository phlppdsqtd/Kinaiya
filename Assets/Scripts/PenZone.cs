using UnityEngine;

public class PenZone : MonoBehaviour
{
    [SerializeField] private PenDirtSpawner penSpawner;

    void OnTriggerStay(Collider other)
    {
        CowLogic cow = other.GetComponent<CowLogic>();
        if (cow != null && penSpawner != null)
        {
            // If the spawner has active dirt, inform the cow it is in a dirty area
            cow.SetInDirtyPen(penSpawner.HasActiveDirt());
        }
    }

    void OnTriggerExit(Collider other)
    {
        CowLogic cow = other.GetComponent<CowLogic>();
        if (cow != null)
        {
            // Cow stepped out of the pen
            cow.SetInDirtyPen(false);
        }
    }
}