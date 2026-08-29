using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class FenceBuilder
{
    // Outer farm perimeter
    const float FARM_MIN_X = -38f;
    const float FARM_MAX_X =  38f;
    const float FARM_MIN_Z = -38f;
    const float FARM_MAX_Z =  38f;

    // 3 cow pens — placed along the right side near the cowshed
    // Each pen is 12 x 14 units, side by side along Z axis
    const float PEN_X_MIN  =  14f;
    const float PEN_X_MAX  =  26f;
    const float PEN_Z_START = -21f;
    const float PEN_WIDTH   =  14f; // Z span per pen
    const int   PEN_COUNT   =  3;

    const float FENCE_Y     = 0f;
    const float PANEL_LEN   = 2f;   // assumed fence panel world length
    const float POST_OFFSET = 0f;   // post at start of each panel

    [MenuItem("Tools/Build Fences")]
    static void BuildFences()
    {
        // Load all meshes from the FBX
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Environment/Models/Farm/Fences.fbx");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogError("[FenceBuilder] Could not load Fences.fbx");
            return;
        }

        Mesh panelMesh = null;
        Mesh postMesh  = null;

        foreach (var a in assets)
        {
            if (!(a is Mesh m)) continue;
            string n = m.name.ToLower();
            if (postMesh == null && (n.Contains("pole") || n.Contains("post")))
                postMesh = m;
            else if (panelMesh == null && (n.Contains("fence") || n.Contains("rail") || n.Contains("panel") || n.Contains("plank")))
                panelMesh = m;
        }

        // Fallback: just pick first two meshes
        var meshList = new List<Mesh>();
        foreach (var a in assets) if (a is Mesh m) meshList.Add(m);
        if (meshList.Count > 0)
        {
            if (panelMesh == null) panelMesh = meshList[meshList.Count > 1 ? 1 : 0];
            if (postMesh  == null) postMesh  = meshList[0];
        }

        if (panelMesh == null) { Debug.LogError("[FenceBuilder] No mesh found in Fences.fbx"); return; }

        // Measure real panel length from mesh bounds
        float realLen = panelMesh.bounds.size.x;
        if (realLen < 0.1f) realLen = panelMesh.bounds.size.z;
        if (realLen < 0.1f) realLen = PANEL_LEN;

        // Parent container
        GameObject root = new GameObject("Fences_Built");
        Undo.RegisterCreatedObjectUndo(root, "Build Fences");

        // ---- OUTER PERIMETER ----
        GameObject perimRoot = new GameObject("Perimeter");
        perimRoot.transform.SetParent(root.transform, false);

        PlaceLine(perimRoot, panelMesh, postMesh, new Vector3(FARM_MIN_X, FENCE_Y, FARM_MIN_Z), new Vector3(FARM_MAX_X, FENCE_Y, FARM_MIN_Z), realLen, "South");
        PlaceLine(perimRoot, panelMesh, postMesh, new Vector3(FARM_MIN_X, FENCE_Y, FARM_MAX_Z), new Vector3(FARM_MAX_X, FENCE_Y, FARM_MAX_Z), realLen, "North");
        PlaceLine(perimRoot, panelMesh, postMesh, new Vector3(FARM_MIN_X, FENCE_Y, FARM_MIN_Z), new Vector3(FARM_MIN_X, FENCE_Y, FARM_MAX_Z), realLen, "West");
        PlaceLine(perimRoot, panelMesh, postMesh, new Vector3(FARM_MAX_X, FENCE_Y, FARM_MIN_Z), new Vector3(FARM_MAX_X, FENCE_Y, FARM_MAX_Z), realLen, "East");

        // ---- COW PENS ----
        GameObject pensRoot = new GameObject("CowPens");
        pensRoot.transform.SetParent(root.transform, false);

        for (int i = 0; i < PEN_COUNT; i++)
        {
            float zMin = PEN_Z_START + i * PEN_WIDTH;
            float zMax = zMin + PEN_WIDTH;
            float xMin = PEN_X_MIN;
            float xMax = PEN_X_MAX;

            GameObject pen = new GameObject($"Pen_{i + 1}");
            pen.transform.SetParent(pensRoot.transform, false);

            PlaceLine(pen, panelMesh, postMesh, new Vector3(xMin, FENCE_Y, zMin), new Vector3(xMax, FENCE_Y, zMin), realLen, "Front");
            PlaceLine(pen, panelMesh, postMesh, new Vector3(xMin, FENCE_Y, zMax), new Vector3(xMax, FENCE_Y, zMax), realLen, "Back");
            PlaceLine(pen, panelMesh, postMesh, new Vector3(xMin, FENCE_Y, zMin), new Vector3(xMin, FENCE_Y, zMax), realLen, "Left");
            // Only close right wall on last pen (others share wall with next pen's left)
            if (i == PEN_COUNT - 1)
                PlaceLine(pen, panelMesh, postMesh, new Vector3(xMax, FENCE_Y, zMin), new Vector3(xMax, FENCE_Y, zMax), realLen, "Right");
        }

        // Shared walls between pens (right wall doubles as next pen's left — add the shared dividers)
        for (int i = 0; i < PEN_COUNT - 1; i++)
        {
            float zShared = PEN_Z_START + (i + 1) * PEN_WIDTH;
            // already covered by Back of pen i and Front of pen i+1 — divider between pens on X axis
            // Actually the pens share Z walls — add the right X wall for pens 0 and 1
            float zMin = PEN_Z_START + i * PEN_WIDTH;
            float zMax = zMin + PEN_WIDTH;
            PlaceLine(pensRoot, panelMesh, postMesh,
                new Vector3(PEN_X_MAX, FENCE_Y, zMin),
                new Vector3(PEN_X_MAX, FENCE_Y, zMax), realLen, $"Pen_{i+1}_Right");
        }

        Debug.Log($"[FenceBuilder] Done. Panel mesh: {panelMesh.name} (len={realLen:F2}). Select 'Fences_Built' in hierarchy to adjust position.");
    }

    [MenuItem("Tools/Paint Fences Brown")]
    static void PaintFencesBrown()
    {
        var root = GameObject.Find("Fences_Built");
        if (root == null) { Debug.LogError("[FenceBuilder] No Fences_Built found."); return; }

        var brownMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        brownMat.color = new Color(0.42f, 0.26f, 0.10f); // brown

        int count = 0;
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
        {
            Undo.RecordObject(mr, "Paint Fences Brown");
            mr.sharedMaterial = brownMat;
            count++;
        }
        Debug.Log($"[FenceBuilder] Painted {count} renderers brown.");
    }

    [MenuItem("Tools/Clear Built Fences")]
    static void ClearFences()
    {
        var go = GameObject.Find("Fences_Built");
        if (go != null) { Undo.DestroyObjectImmediate(go); Debug.Log("[FenceBuilder] Cleared."); }
        else Debug.Log("[FenceBuilder] Nothing to clear.");
    }

    static void PlaceLine(GameObject parent, Mesh panel, Mesh post, Vector3 from, Vector3 to, float panelLen, string label, float heightScale = 0.55f)
    {
        Vector3 dir = (to - from);
        float totalLen = dir.magnitude;
        dir.Normalize();
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, dir);

        // Snap count so panels fit evenly
        int count = Mathf.Max(1, Mathf.RoundToInt(totalLen / panelLen));
        float step = totalLen / count;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = from + dir * (i * step + step * 0.5f);

            var go = new GameObject($"{label}_{i}");
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;
            go.transform.rotation = rot;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = panel;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GetFenceMaterial();
            go.transform.localScale = new Vector3(1f, heightScale, 1f);

            // Post at start of each section
            if (post != null)
            {
                var postGo = new GameObject($"{label}_Post_{i}");
                postGo.transform.SetParent(parent.transform, false);
                postGo.transform.position = from + dir * (i * step);
                postGo.transform.rotation = rot;
                postGo.AddComponent<MeshFilter>().sharedMesh = post;
                postGo.AddComponent<MeshRenderer>().sharedMaterial = GetFenceMaterial();
                postGo.transform.localScale = new Vector3(1f, heightScale, 1f);
            }
        }

        // Final post
        if (post != null)
        {
            var postGo = new GameObject($"{label}_Post_end");
            postGo.transform.SetParent(parent.transform, false);
            postGo.transform.position = to;
            postGo.transform.rotation = rot;
            postGo.AddComponent<MeshFilter>().sharedMesh = post;
            postGo.AddComponent<MeshRenderer>().sharedMaterial = GetFenceMaterial();
            postGo.transform.localScale = new Vector3(1f, heightScale, 1f);
        }
    }

    static Material _mat;
    static Material GetFenceMaterial()
    {
        if (_mat != null) return _mat;
        // Try to grab embedded material from FBX
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Environment/Models/Farm/Fences.fbx");
        foreach (var a in assets)
            if (a is Material m) { _mat = m; return _mat; }
        _mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        return _mat;
    }
}
