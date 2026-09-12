using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CowMouthTrigger : MonoBehaviour
{
    [SerializeField] private CowLogic myCowLogic;

    private void OnTriggerEnter(Collider other)
    {
        if (myCowLogic == null) return;

        // 1. Check for Food
        if (other.TryGetComponent<FeedItem>(out FeedItem food))
        {
            bool isInTrough = false;
            if (other.TryGetComponent<XRGrabInteractable>(out var grab))
            {
                var interactor = grab.firstInteractorSelecting as MonoBehaviour;
                if (interactor != null && interactor.GetComponent<XRSocketInteractor>() != null)
                {
                    isInTrough = true;
                }
            }

            if ((!food.isHeld || isInTrough) && !food.isBeingEaten)
            {
                myCowLogic.ConsumeFood(food);
            }
        }
        
        // 2. Check for Water Trough
        if (other.TryGetComponent<WaterTrough>(out WaterTrough trough))
        {
            if (trough.HasWater && !myCowLogic.IsDrinking)
            {
                myCowLogic.ConsumeWater(trough);
            }
        }
    }
}

/*
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CowMouthTrigger : MonoBehaviour
{
    [SerializeField] private CowLogic myCowLogic;

    private void Awake()
    {
        if (myCowLogic == null)
        {
            myCowLogic = GetComponentInParent<CowLogic>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (myCowLogic == null) return;
        
        if (other.TryGetComponent<FeedItem>(out FeedItem food))
        {
            bool isInTrough = false;

            // Check if the food is being held by an XR Socket
            if (other.TryGetComponent<XRGrabInteractable>(out var grab))
            {
                var interactor = grab.firstInteractorSelecting as MonoBehaviour;
                if (interactor != null && interactor.GetComponent<XRSocketInteractor>() != null)
                {
                    isInTrough = true;
                }
            }

            // Only eat if the player is NOT holding it (or it is safely inside a trough socket)
            if ((!food.isHeld || isInTrough) && !food.isBeingEaten)
            {
                myCowLogic.ConsumeFood(food);
            }
        }
    }
}
*/