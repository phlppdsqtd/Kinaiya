using UnityEngine;
using System.Collections.Generic;

public class ToolResetManager : MonoBehaviour
{
    private class ToolData
    {
        public Transform toolTransform;
        public Vector3 startPosition;
        public Quaternion startRotation;
        public Rigidbody rb;
    }

    private List<ToolData> toolsList = new List<ToolData>();

    void Start()
    {
        // Automatically loop through all child GameObjects inside the "Tools" parent
        foreach (Transform child in transform)
        {
            ToolData data = new ToolData();
            data.toolTransform = child;
            data.startPosition = child.position;
            data.startRotation = child.rotation;
            data.rb = child.GetComponent<Rigidbody>();
            
            toolsList.Add(data);
        }
    }

    public void ResetAllTools()
    {
        foreach (ToolData tool in toolsList)
        {
            if (tool.rb != null)
            {
                // Teleport the Rigidbody directly so physics interpolation doesn't snap it back
                tool.rb.position = tool.startPosition;
                tool.rb.rotation = tool.startRotation;
                
                // Reset velocities
                tool.rb.linearVelocity = Vector3.zero;
                tool.rb.angularVelocity = Vector3.zero;
            }
            else
            {
                // Fallback for any tools that don't have a Rigidbody
                tool.toolTransform.position = tool.startPosition;
                tool.toolTransform.rotation = tool.startRotation;
            }
        }
    }
}