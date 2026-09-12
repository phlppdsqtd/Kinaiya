using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class WaterPump : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem waterParticles;
    [SerializeField] private XRSocketInteractor bucketSocket;
    [SerializeField] private AudioSource pumpAudio;

    private XRSimpleInteractable simpleInteractable;

    void Awake()
    {
        simpleInteractable = GetComponent<XRSimpleInteractable>();
        simpleInteractable.selectEntered.AddListener(OnPumpActivated);
    }

    private void OnPumpActivated(SelectEnterEventArgs args)
    {
        // Play FX and Audio
        if (waterParticles != null) waterParticles.Play();
        if (pumpAudio != null) pumpAudio.Play();

        // Check if a bucket is in the socket
        if (bucketSocket.hasSelection)
        {
            var interactable = bucketSocket.firstInteractableSelected as MonoBehaviour;
            if (interactable != null && interactable.TryGetComponent<WaterBucket>(out WaterBucket bucket))
            {
                bucket.FillBucket();
            }
        }
    }

    void OnDestroy()
    {
        simpleInteractable.selectEntered.RemoveListener(OnPumpActivated);
    }
}