using UnityEngine;

public class CowWeightController : MonoBehaviour
{
    public SkinnedMeshRenderer cowMesh;
    
    [Range(0, 100)]
    public float bloatValue = 20f; 

    private int leftIndex = -1;
    private int rightIndex = -1;

    void Awake()
    {
        // Cache the indices at startup for better performance in LateUpdate
        if (cowMesh != null && cowMesh.sharedMesh != null)
        {
            leftIndex = cowMesh.sharedMesh.GetBlendShapeIndex("BloatedLeftSide");
            rightIndex = cowMesh.sharedMesh.GetBlendShapeIndex("BloatedRightSide");
        }
    }

    // This method runs automatically in the Editor whenever a value is changed in the Inspector
    void OnValidate()
    {
        if (cowMesh == null || cowMesh.sharedMesh == null) return;

        int tempLeft = cowMesh.sharedMesh.GetBlendShapeIndex("BloatedLeftSide");
        int tempRight = cowMesh.sharedMesh.GetBlendShapeIndex("BloatedRightSide");

        if (tempLeft != -1) cowMesh.SetBlendShapeWeight(tempLeft, bloatValue);
        if (tempRight != -1) cowMesh.SetBlendShapeWeight(tempRight, bloatValue);
    }

    void LateUpdate()
    {
        if (cowMesh == null) return;

        // Apply cached indices to override Animator every frame
        if (leftIndex != -1) cowMesh.SetBlendShapeWeight(leftIndex, bloatValue);
        if (rightIndex != -1) cowMesh.SetBlendShapeWeight(rightIndex, bloatValue);
    }

    // Called by CowLogic to dynamically update the weight
    public void SetWeight(float newValue)
    {
        bloatValue = Mathf.Clamp(newValue, 0f, 100f);
    }
}