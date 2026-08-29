using UnityEngine;
using UnityEditor;

public static class GateBuilder
{
    [MenuItem("Tools/Build Gates")]
    static void BuildGates()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Environment/Models/Farm/Fences.fbx");
        Mesh gateMesh = null;
        foreach (var a in assets)
            if (a is Mesh m) { gateMesh = m; break; }

        if (gateMesh == null) { Debug.LogError("[GateBuilder] No mesh in Fences.fbx"); return; }

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.42f, 0.26f, 0.10f);

        var root = new GameObject("Gates");
        Undo.RegisterCreatedObjectUndo(root, "Build Gates");

        // Farm entrance gate — south perimeter, centered (auto-open on proximity)
        PlaceGate(root, gateMesh, mat, new Vector3(0f, 0f, -38f), 0f, "Entrance_Gate", 0f, true);

        // Each cow pen gets a door slightly ajar (25°) by default — no trigger
        float[] penZCenters = { -21f + 7f, -21f + 7f + 14f, -21f + 7f + 28f };
        for (int i = 0; i < penZCenters.Length; i++)
            PlaceGate(root, gateMesh, mat, new Vector3(14f, 0f, penZCenters[i]), 90f, $"Pen_{i+1}_Gate", 25f, false);

        Debug.Log("[GateBuilder] Gates placed.");
    }

    [MenuItem("Tools/Clear Gates")]
    static void ClearGates()
    {
        var go = GameObject.Find("Gates");
        if (go != null) { Undo.DestroyObjectImmediate(go); Debug.Log("[GateBuilder] Cleared."); }
    }

    static void PlaceGate(GameObject parent, Mesh mesh, Material mat, Vector3 pos, float yRot, string name, float staticOpenAngle = 0f, bool autoOpen = false)
    {
        var pivot = new GameObject(name + "_Pivot");
        pivot.transform.SetParent(parent.transform, false);
        pivot.transform.position = pos;
        // Bake base yRot + static open angle into pivot rotation
        pivot.transform.rotation = Quaternion.Euler(0f, yRot + staticOpenAngle, 0f);
        Undo.RegisterCreatedObjectUndo(pivot, "Build Gates");

        var panel = new GameObject(name + "_Panel");
        panel.transform.SetParent(pivot.transform, false);
        panel.transform.localPosition = new Vector3(1f, 0f, 0f);
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = new Vector3(1f, 0.55f, 1f); // match fence height

        var mf = panel.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        panel.AddComponent<MeshRenderer>().sharedMaterial = mat;
        Undo.RegisterCreatedObjectUndo(panel, "Build Gates");

        if (autoOpen)
            pivot.AddComponent<AutoGate>();
    }
}
