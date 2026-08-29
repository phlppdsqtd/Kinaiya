using UnityEngine;

public class FeedItem : MonoBehaviour
{
    [Tooltip("How much hunger this food restores.")]
    public float nutritionValue = 25f;

    [Tooltip("Is the player currently holding this?")]
    public bool isHeld = false; 

    [HideInInspector]
    public bool isBeingEaten = false; 

    // This method allows Unity's XR Events to change the isHeld variable
    public void SetIsHeld(bool value)
    {
        isHeld = value;
    }
}