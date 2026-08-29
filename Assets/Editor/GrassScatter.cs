using UnityEngine;
using UnityEditor;

public static class GrassScatter
{
    // Area beside the cowshed (blank side, not feed storage side)
    const float X_MIN = -5f;
    const float X_MAX = 13f;
    const float Z_MIN =  8f;
    const float Z_MAX = 25f;
    const int   COUNT = 180;  // abundant

    [MenuItem("Tools/Scatter Grass (Cowshed Side)")]
    static void Scatter()
    {
        var grass1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/Prefabs/Grass_01.prefab");
        var grass2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/Prefabs/Grass_02.prefab");

        if (grass1 == null) { Debug.LogError("[GrassScatter] Grass_01 prefab not found."); return; }

        var root = new GameObject("Grass_CowshedSide");
        Undo.RegisterCreatedObjectUndo(root, "Scatter Grass");

        int seed = 99;
        for (int i = 0; i < COUNT; i++)
        {
            float x = Lerp(X_MIN, X_MAX, Next(ref seed));
            float z = Lerp(Z_MIN, Z_MAX, Next(ref seed));

            // Skip path zone (Z ≈ 5, width ~2.5)
            if (z < 7.5f) continue;

            var prefab = (Next(ref seed) > 0.5f && grass2 != null) ? grass2 : grass1;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(x, 0f, z);

            float angle = Next(ref seed) * 360f;
            float scale = 0.8f + Next(ref seed) * 0.7f;
            go.transform.rotation = Quaternion.Euler(0f, angle, 0f);
            go.transform.localScale = Vector3.one * scale;

            Undo.RegisterCreatedObjectUndo(go, "Scatter Grass");
        }

        Debug.Log($"[GrassScatter] Done — grass scattered on cowshed side.");
    }

    [MenuItem("Tools/Clear Grass (Cowshed Side)")]
    static void Clear()
    {
        var go = GameObject.Find("Grass_CowshedSide");
        if (go != null) { Undo.DestroyObjectImmediate(go); Debug.Log("[GrassScatter] Cleared."); }
    }

    static float Next(ref int s) { s = s * 1664525 + 1013904223; return (s & 0x7FFFFFFF) / (float)0x7FFFFFFF; }
    static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
