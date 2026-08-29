using UnityEngine;
using UnityEditor;

public static class PathwayBuilder
{
    static Material _dirtMat;

    const float TILE_LEN    = 0.8f;  // length of each mini tile
    const float JITTER_LAT  = 0.35f; // max lateral (sideways) drift per tile
    const float JITTER_ROT  = 8f;    // max Y rotation wobble per tile (degrees)
    const float WIDTH_VARY  = 0.25f; // max width variation per tile

    [MenuItem("Tools/Build Pathways")]
    static void BuildPathways()
    {
        var root = new GameObject("Pathways");
        Undo.RegisterCreatedObjectUndo(root, "Build Pathways");

        var paths = new (Vector3 a, Vector3 b, float w)[]
        {
            (new Vector3(-8f, 0.01f,  5f), new Vector3(14f,  0.01f,  5f),  2.5f),
            (new Vector3(14f, 0.01f,  5f), new Vector3(14f,  0.01f, -7f),  2f),
            (new Vector3(-8f, 0.01f,  5f), new Vector3(-8f,  0.01f, 18f),  2f),
            (new Vector3(-8f, 0.01f,  5f), new Vector3(-8f,  0.01f,-10f),  2f),
            (new Vector3(14f, 0.01f, -7f), new Vector3(26f,  0.01f, -7f),  2f),
        };

        var mat = GetDirtMaterial();
        int seed = 42;

        foreach (var (a, b, w) in paths)
        {
            PlaceRoughPath(root, a, b, w, mat, ref seed);
            seed += 17;
        }

        Debug.Log("[PathwayBuilder] Done — rough paths placed.");
    }

    [MenuItem("Tools/Clear Pathways")]
    static void ClearPathways()
    {
        var go = GameObject.Find("Pathways");
        if (go != null) { Undo.DestroyObjectImmediate(go); Debug.Log("[PathwayBuilder] Cleared."); }
    }

    static void PlaceRoughPath(GameObject parent, Vector3 a, Vector3 b, float width, Material mat, ref int seed)
    {
        Vector3 dir     = (b - a).normalized;
        Vector3 lateral = Vector3.Cross(dir, Vector3.up).normalized;
        float   total   = Vector3.Distance(a, b);
        int     count   = Mathf.Max(1, Mathf.RoundToInt(total / TILE_LEN));
        float   step    = total / count;

        Vector3 cursor = a;

        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;

            // Lateral drift — sine wave + noise for organic feel
            float drift = Mathf.Sin(t * Mathf.PI * 3.1f) * JITTER_LAT * 0.6f
                        + (NextFloat(ref seed) - 0.5f) * JITTER_LAT;

            Vector3 pos = a + dir * (i * step + step * 0.5f) + lateral * drift;
            pos.y = 0.01f;

            float rotY   = (NextFloat(ref seed) - 0.5f) * 2f * JITTER_ROT;
            float wScale = width + (NextFloat(ref seed) - 0.5f) * WIDTH_VARY;
            float lScale = step * 1.15f; // slight overlap to avoid gaps

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "PathTile";
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, rotY, 0f);
            go.transform.localScale = new Vector3(wScale, 0.02f, lScale);
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Undo.RegisterCreatedObjectUndo(go, "Build Pathways");
        }
    }

    static float NextFloat(ref int seed)
    {
        seed = seed * 1664525 + 1013904223;
        return ((seed & 0x7FFFFFFF) / (float)0x7FFFFFFF);
    }

    static Material GetDirtMaterial()
    {
        if (_dirtMat != null) return _dirtMat;
        _dirtMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        _dirtMat.color = new Color(0.45f, 0.28f, 0.12f);
        return _dirtMat;
    }
}
