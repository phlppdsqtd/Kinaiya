using UnityEngine;

public class GateController : MonoBehaviour
{
    [Tooltip("Use 90 to swing one way, or -90 to swing the opposite way.")]
    public float swingAngle = 90f; 
    
    private bool isClosed = true;
    private float closedAngle;
    private float openAngle;

    void Start()
    {
        // Save the starting rotation as the closed state
        closedAngle = transform.localEulerAngles.y;
        // Calculate the open state using your custom swing angle
        openAngle = closedAngle + swingAngle; 
    }

    public void ToggleGate()
    {
        isClosed = !isClosed;
        
        // Determine the target angle based on the new state
        float targetAngle = isClosed ? closedAngle : openAngle;
        
        // Apply the new rotation to the Y axis instantly
        transform.localEulerAngles = new Vector3(
            transform.localEulerAngles.x, 
            targetAngle, 
            transform.localEulerAngles.z
        );
    }
}