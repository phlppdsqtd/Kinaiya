using UnityEngine;
using UnityEditor;

public static class ScaleGrass
{
    const float SCALE_MULT = 0.45f; // shrink to 45% of current size

    [MenuItem("Tools/Scale Down All Grass")]
    static void ScaleDown()
    {
        // Only scale children of our scattered grass group, not terrain grass
        var root = GameObject.Find("Grass_CowshedSide");
        if (root == null) { Debug.LogError("[ScaleGrass] Grass_CowshedSide not found."); return; }

        int count = 0;
        foreach (Transform child in root.transform)
        {
            Undo.RecordObject(child, "Scale Down Grass");
            child.localScale *= SCALE_MULT;
            count++;
        }
        Debug.Log($"[ScaleGrass] Scaled down {count} grass objects.");
    }
}
