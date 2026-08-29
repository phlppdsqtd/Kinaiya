using UnityEngine;
using UnityEditor;

public static class EnvironmentBuilder
{
    const float CLEAR_RADIUS = 8f;
    const float SCATTER_RADIUS = 120f;
    const float RAY_HEIGHT = 300f;

    [MenuItem("Tools/Build Flat Environment")]
    static void BuildEnvironment()
    {
        GameObject old = GameObject.Find("Environment");
        if (old != null)
            Undo.DestroyObjectImmediate(old);

        // Find the world-space bounds of all ground meshes
        Bounds terrainBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool boundsInit = false;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (r.gameObject.name.ToLower().Contains("ground"))
            {
                if (!boundsInit) { terrainBounds = r.bounds; boundsInit = true; }
                else terrainBounds.Encapsulate(r.bounds);
            }
        }

        float scatter = boundsInit
            ? Mathf.Max(terrainBounds.extents.x, terrainBounds.extents.z) * 0.95f
            : SCATTER_RADIUS;

        Vector3 center = boundsInit ? new Vector3(terrainBounds.center.x, 0, terrainBounds.center.z) : Vector3.zero;

        Debug.Log($"[EnvironmentBuilder] Terrain bounds center={center} scatter radius={scatter:F1}");

        Physics.SyncTransforms();

        GameObject root = new GameObject("Environment");
        Undo.RegisterCreatedObjectUndo(root, "Build Flat Environment");

        string p = "Assets/Environment/Prefabs/";

        Scatter(p, new[]{"Tree_01","Tree_02","Tree_03","Tree_04","Tree_05"},   root.transform, 160, CLEAR_RADIUS+4f,   scatter,        0.8f, 1.4f, center);
        Scatter(p, new[]{"Rock_01","Rock_02","Rock_03","Rock_04","Rock_05"},   root.transform,  90, CLEAR_RADIUS+1f,   scatter*0.95f,  0.7f, 1.5f, center);
        Scatter(p, new[]{"Bush_01","Bush_02","Bush_03"},                       root.transform,  90, CLEAR_RADIUS+0.5f, scatter*0.9f,   0.8f, 1.3f, center);
        Scatter(p, new[]{"Stump_01","Branch_01"},                              root.transform,  35, CLEAR_RADIUS+1f,   scatter*0.85f,  0.9f, 1.1f, center);
        Scatter(p, new[]{"Mushroom_01","Mushroom_02"},                         root.transform,  45, CLEAR_RADIUS+0.5f, scatter*0.8f,   0.7f, 1.1f, center);
        Scatter(p, new[]{"Flowers_01","Flowers_02","Grass_01","Grass_02"},     root.transform, 120, CLEAR_RADIUS,      scatter*0.75f,  0.8f, 1.2f, center);

        Debug.Log("[EnvironmentBuilder] Done — environment populated.");
    }

    static void Scatter(string basePath, string[] names, Transform parent,
        int count, float minR, float maxR, float scaleMin, float scaleMax, Vector3 center)
    {
        for (int i = 0; i < count; i++)
        {
            string name = names[Random.Range(0, names.Length)];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + name + ".prefab");
            if (prefab == null) { Debug.LogWarning("Missing prefab: " + name); continue; }

            // Random point in annular zone, offset by terrain center
            float angle  = Random.Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Random.Range(0f, 1f)) * (maxR - minR) + minR;
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;

            // Raycast from above to snap to terrain surface
            float y = 0f;
            if (Physics.Raycast(new Vector3(x, RAY_HEIGHT, z), Vector3.down, out RaycastHit hit, RAY_HEIGHT * 2f))
                y = hit.point.y;

            // Skip if too close to world-space origin (clear zone)
            if (new Vector2(x, z).magnitude < CLEAR_RADIUS) continue;

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(go, "Place prefab");
            go.transform.position = new Vector3(x, y, z);
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            go.transform.localScale = Vector3.one * Random.Range(scaleMin, scaleMax);
        }
    }
}
