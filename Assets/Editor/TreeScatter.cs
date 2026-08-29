using UnityEngine;
using UnityEditor;

public static class TreeScatter
{
    // Farm fence bounds
    const float FARM_MIN = -38f;
    const float FARM_MAX =  38f;

    // Outside the fence — scatter up to this radius
    const float OUTER_RADIUS = 80f;

    // Tree prefab paths
    static readonly string[] TREE_PATHS = {
        "Assets/Environment/Prefabs/Tree_01.prefab",
        "Assets/Environment/Prefabs/Tree_02.prefab",
        "Assets/Environment/Prefabs/Tree_03.prefab",
        "Assets/Environment/Prefabs/Tree_04.prefab",
        "Assets/Environment/Prefabs/Tree_05.prefab",
    };

    [MenuItem("Tools/Scatter Trees")]
    static void Scatter()
    {
        var trees = new GameObject[TREE_PATHS.Length];
        for (int i = 0; i < TREE_PATHS.Length; i++)
        {
            trees[i] = AssetDatabase.LoadAssetAtPath<GameObject>(TREE_PATHS[i]);
            if (trees[i] == null) Debug.LogWarning($"[TreeScatter] Not found: {TREE_PATHS[i]}");
        }

        var root = new GameObject("Trees_Scattered");
        Undo.RegisterCreatedObjectUndo(root, "Scatter Trees");

        int seed = 77;

        // -- Inside farm: 60 trees spread across the terrain, avoiding buildings/pens
        for (int i = 0; i < 60; i++)
        {
            float x = Lerp(FARM_MIN + 2f, FARM_MAX - 2f, Next(ref seed));
            float z = Lerp(FARM_MIN + 2f, FARM_MAX - 2f, Next(ref seed));

            // Skip cow pens zone (X:14-26, Z:-21 to +21)
            if (x > 12f && x < 27f && z > -22f && z < 22f) { i--; continue; }
            // Skip building cluster (X:-12 to 12, Z:0 to 35)
            if (x > -13f && x < 13f && z > -2f && z < 36f) { i--; continue; }
            // Skip path corridors roughly
            if (Mathf.Abs(z - 5f) < 3f && x > -10f && x < 15f) { i--; continue; }

            PlaceTree(root, trees, x, z, ref seed);
        }

        // -- Outside farm: 120 trees in the border ring
        int placed = 0;
        int tries = 0;
        while (placed < 120 && tries < 2000)
        {
            tries++;
            float x = Lerp(-OUTER_RADIUS, OUTER_RADIUS, Next(ref seed));
            float z = Lerp(-OUTER_RADIUS, OUTER_RADIUS, Next(ref seed));

            // Must be outside fence
            bool outsideFence = x < FARM_MIN - 1f || x > FARM_MAX + 1f || z < FARM_MIN - 1f || z > FARM_MAX + 1f;
            if (!outsideFence) continue;

            PlaceTree(root, trees, x, z, ref seed);
            placed++;
        }

        Debug.Log($"[TreeScatter] Done — {60 + placed} trees placed.");
    }

    [MenuItem("Tools/Clear Trees")]
    static void Clear()
    {
        var go = GameObject.Find("Trees_Scattered");
        if (go != null) { Undo.DestroyObjectImmediate(go); Debug.Log("[TreeScatter] Cleared."); }
    }

    static void PlaceTree(GameObject root, GameObject[] trees, float x, float z, ref int seed)
    {
        var prefab = trees[(int)(Next(ref seed) * trees.Length) % trees.Length];
        if (prefab == null) return;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(x, 0f, z);
        go.transform.rotation = Quaternion.Euler(0f, Next(ref seed) * 360f, 0f);
        float scale = 0.7f + Next(ref seed) * 0.8f;
        go.transform.localScale = Vector3.one * scale;
        Undo.RegisterCreatedObjectUndo(go, "Scatter Trees");
    }

    static float Next(ref int s) { s = s * 1664525 + 1013904223; return (s & 0x7FFFFFFF) / (float)0x7FFFFFFF; }
    static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
