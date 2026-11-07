using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingHookMeshTool
{
    private const string MeshPath = "Assets/04_Models/FishingHookMesh.asset";
    private const string MaterialPath = "Assets/06_Materials/MAT_FishingHook.mat";
    private const string MenuPath = "Tools/Cyber Threat AR/Create Fishing Hook Mesh";

    private static readonly string[] LegacyHookParts =
    {
        "Hook_01",
        "Hook_02",
        "Hook_03",
        "Hook_04",
        "Hook_05",
        "Hook_Tip"
    };

    [InitializeOnLoadMethod]
    private static void ScheduleFirstGeneration()
    {
        EditorApplication.delayCall += GenerateOnceForOpenPhishingPrefab;
    }

    private static void GenerateOnceForOpenPhishingPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath) != null)
            return;

        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage == null || prefabStage.prefabContentsRoot == null)
            return;

        if (prefabStage.prefabContentsRoot.name != "Threat_Phising")
            return;

        Selection.activeGameObject = prefabStage.prefabContentsRoot;
        CreateFishingHookMesh();
    }

    [MenuItem(MenuPath)]
    public static void CreateFishingHookMesh()
    {
        Transform fishingHookRoot = FindFishingHookRoot();
        if (fishingHookRoot == null)
        {
            Debug.LogWarning(
                "FishingHookRoot tidak ditemukan. Buka Threat_Phising.prefab dan pilih " +
                "Threat_Phising, AnimatedVisual, FishingHookRoot, Hook, atau salah satu anaknya."
            );
            return;
        }

        Transform hookRoot = fishingHookRoot.Find("Hook");
        if (hookRoot == null)
        {
            Debug.LogError("GameObject Hook tidak ditemukan di bawah FishingHookRoot.");
            return;
        }

        EnsureModelsFolderExists();

        Mesh generatedMesh = BuildHookMesh();
        Mesh meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);

        if (meshAsset == null)
        {
            meshAsset = generatedMesh;
            AssetDatabase.CreateAsset(meshAsset, MeshPath);
        }
        else
        {
            Undo.RecordObject(meshAsset, "Regenerate Fishing Hook Mesh");
            EditorUtility.CopySerialized(generatedMesh, meshAsset);
            Object.DestroyImmediate(generatedMesh);
            EditorUtility.SetDirty(meshAsset);
        }

        GameObject hookMeshObject = GetOrCreateHookMeshObject(hookRoot);
        MeshFilter meshFilter = GetOrAddComponent<MeshFilter>(hookMeshObject);
        MeshRenderer meshRenderer = GetOrAddComponent<MeshRenderer>(hookMeshObject);

        Undo.RecordObject(meshFilter, "Assign Fishing Hook Mesh");
        meshFilter.sharedMesh = meshAsset;

        Material hookMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (hookMaterial != null)
        {
            Undo.RecordObject(meshRenderer, "Assign Fishing Hook Material");
            meshRenderer.sharedMaterial = hookMaterial;
        }
        else
        {
            Debug.LogWarning($"Material tidak ditemukan di {MaterialPath}. Mesh tetap dibuat.");
        }

        DisableLegacyHookParts(hookRoot);

        EditorUtility.SetDirty(meshFilter);
        EditorUtility.SetDirty(meshRenderer);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = hookMeshObject;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log(
            "Fishing hook mesh berhasil dibuat dan dipasang. " +
            "Capsule lama dinonaktifkan (tidak dihapus) dan dapat diaktifkan kembali bila diperlukan."
        );
    }

    private static Mesh BuildHookMesh()
    {
        const int radialSegments = 12;
        const float tubeRadius = 0.0018f;
        const float depth = -0.006f;

        List<Vector3> path = new List<Vector3>();

        // Batang vertikal: pangkalnya tepat di bawah FishingLine. Ujung bawah
        // berhenti cukup tinggi agar lengkungan kail tidak masuk ke amplop.
        const int shankSteps = 6;
        for (int i = 0; i < shankSteps; i++)
        {
            float t = i / (float)(shankSteps - 1);
            path.Add(new Vector3(0f, Mathf.Lerp(0.071f, 0.060f, t), depth));
        }

        // Lengkungan bawah membentuk badan utama huruf J.
        const int curveSteps = 17;
        Vector2 curveCenter = new Vector2(0.011f, 0.060f);
        const float curveRadius = 0.011f;
        for (int i = 1; i < curveSteps; i++)
        {
            float t = i / (float)(curveSteps - 1);
            float angle = Mathf.Lerp(Mathf.PI, Mathf.PI * 2f, t);
            path.Add(new Vector3(
                curveCenter.x + Mathf.Cos(angle) * curveRadius,
                curveCenter.y + Mathf.Sin(angle) * curveRadius,
                depth
            ));
        }

        // Ujung kail naik dan sedikit mengarah ke dalam agar siluetnya mudah dikenali.
        path.Add(new Vector3(0.0220f, 0.0640f, depth));
        path.Add(new Vector3(0.0218f, 0.0680f, depth));
        path.Add(new Vector3(0.0212f, 0.0715f, depth));
        path.Add(new Vector3(0.0198f, 0.0745f, depth));

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 tangent;
            if (i == 0)
                tangent = (path[1] - path[0]).normalized;
            else if (i == path.Count - 1)
                tangent = (path[i] - path[i - 1]).normalized;
            else
                tangent = (path[i + 1] - path[i - 1]).normalized;

            Vector3 planarNormal = new Vector3(-tangent.y, tangent.x, 0f).normalized;
            Vector3 binormal = Vector3.forward;

            for (int side = 0; side < radialSegments; side++)
            {
                float angle = side / (float)radialSegments * Mathf.PI * 2f;
                Vector3 offset =
                    planarNormal * (Mathf.Cos(angle) * tubeRadius) +
                    binormal * (Mathf.Sin(angle) * tubeRadius);

                vertices.Add(path[i] + offset);
                uvs.Add(new Vector2(side / (float)radialSegments, i / (float)(path.Count - 1)));
            }
        }

        for (int ring = 0; ring < path.Count - 1; ring++)
        {
            int current = ring * radialSegments;
            int next = (ring + 1) * radialSegments;

            for (int side = 0; side < radialSegments; side++)
            {
                int nextSide = (side + 1) % radialSegments;

                triangles.Add(current + side);
                triangles.Add(next + side);
                triangles.Add(next + nextSide);

                triangles.Add(current + side);
                triangles.Add(next + nextSide);
                triangles.Add(current + nextSide);
            }
        }

        AddEndCap(path[0], 0, true, radialSegments, vertices, uvs, triangles);
        AddEndCap(
            path[path.Count - 1],
            (path.Count - 1) * radialSegments,
            false,
            radialSegments,
            vertices,
            uvs,
            triangles
        );

        Mesh mesh = new Mesh
        {
            name = "FishingHookMesh"
        };

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddEndCap(
        Vector3 center,
        int ringStart,
        bool reverse,
        int radialSegments,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<int> triangles
    )
    {
        int centerIndex = vertices.Count;
        vertices.Add(center);
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (int side = 0; side < radialSegments; side++)
        {
            int nextSide = (side + 1) % radialSegments;
            if (reverse)
            {
                triangles.Add(centerIndex);
                triangles.Add(ringStart + nextSide);
                triangles.Add(ringStart + side);
            }
            else
            {
                triangles.Add(centerIndex);
                triangles.Add(ringStart + side);
                triangles.Add(ringStart + nextSide);
            }
        }
    }

    private static GameObject GetOrCreateHookMeshObject(Transform hookRoot)
    {
        Transform existing = hookRoot.Find("HookMesh");
        if (existing != null)
        {
            Undo.RecordObject(existing.gameObject, "Enable Fishing Hook Mesh");
            existing.gameObject.SetActive(true);
            return existing.gameObject;
        }

        GameObject hookMeshObject = new GameObject("HookMesh");
        Undo.RegisterCreatedObjectUndo(hookMeshObject, "Create Fishing Hook Mesh Object");
        hookMeshObject.transform.SetParent(hookRoot, false);
        hookMeshObject.transform.localPosition = Vector3.zero;
        hookMeshObject.transform.localRotation = Quaternion.identity;
        hookMeshObject.transform.localScale = Vector3.one;
        return hookMeshObject;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void DisableLegacyHookParts(Transform hookRoot)
    {
        foreach (string partName in LegacyHookParts)
        {
            Transform part = hookRoot.Find(partName);
            if (part == null || !part.gameObject.activeSelf)
                continue;

            Undo.RecordObject(part.gameObject, "Disable Legacy Fishing Hook Part");
            part.gameObject.SetActive(false);
            EditorUtility.SetDirty(part.gameObject);
        }
    }

    private static Transform FindFishingHookRoot()
    {
        Transform current = Selection.activeTransform;
        while (current != null)
        {
            if (current.name == "FishingHookRoot")
                return current;

            Transform child = current.Find("AnimatedVisual/FishingHookRoot");
            if (child != null)
                return child;

            child = current.Find("FishingHookRoot");
            if (child != null)
                return child;

            current = current.parent;
        }

        GameObject threatRoot = GameObject.Find("Threat_Phising");
        return threatRoot != null
            ? threatRoot.transform.Find("AnimatedVisual/FishingHookRoot")
            : null;
    }

    private static void EnsureModelsFolderExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/04_Models"))
        {
            AssetDatabase.CreateFolder("Assets", "04_Models");
        }
    }
}
