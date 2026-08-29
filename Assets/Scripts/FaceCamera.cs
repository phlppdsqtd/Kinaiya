using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Transform mainCamera;

    void Start()
    {
        // Automatically find the player's VR headset camera
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Forces the icon to mirror the camera's rotation, 
            // keeping it perfectly flat against your vision from any angle.
            transform.forward = mainCamera.forward;
        }
    }
}