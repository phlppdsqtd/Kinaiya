using UnityEngine;
using UnityEditor;

public static class FlattenGround
{
    [MenuItem("Tools/Flatten Ground Y")]
    static void Flatten()
    {
        int count = 0;
        const float TARGET_WORLD_Y = 0f;
        const float MAX_WORLD_Y    = 1.5f;

        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (!go.name.Contains("Ground_03")) continue;
            float worldY = go.transform.position.y;
            if (Mathf.Abs(worldY - TARGET_WORLD_Y) < 0.01f) continue;
            if (worldY > MAX_WORLD_Y) continue;
            Undo.RecordObject(go.transform, "Flatten Ground Y");
            Vector3 wp = go.transform.position;
            go.transform.position = new Vector3(wp.x, TARGET_WORLD_Y, wp.z);
            count++;
        }
        Debug.Log($"[FlattenGround] Snapped {count} flat-area tiles to world Y=0.");
    }

    [MenuItem("Tools/Replace Flat Ground Meshes")]
    static void ReplaceFlatMeshes()
    {
        Mesh flatMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Environment/Models/Ground_01.fbx");
        if (flatMesh == null)
        {
            // Try searching all meshes named Ground_01
            string[] guids = AssetDatabase.FindAssets("Ground_01 t:Mesh");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                flatMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (flatMesh != null) { Debug.Log("[FlattenGround] Found flat mesh at: " + path); break; }
            }
        }

        if (flatMesh == null) { Debug.LogError("[FlattenGround] Could not find Ground_01 mesh!"); return; }

        int count = 0;
        const float MAX_WORLD_Y = 1.5f;

        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (!go.name.Contains("Ground_03")) continue;
            if (go.transform.position.y > MAX_WORLD_Y) continue; // skip mountains

            var mf = go.GetComponent<MeshFilter>();
            if (mf == null) continue;
            if (mf.sharedMesh == flatMesh) continue;

            Undo.RecordObject(mf, "Replace Flat Ground Mesh");
            mf.sharedMesh = flatMesh;
            count++;
        }

        Debug.Log($"[FlattenGround] Replaced mesh on {count} flat-area tiles.");
    }
}
