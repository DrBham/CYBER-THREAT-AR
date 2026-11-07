
using UnityEngine;
using UnityEditor;

public static class EnvelopeFlapMeshTool
{
    [MenuItem("Tools/Cyber Threat AR/Create Envelope Triangle Mesh")]
    public static void CreateTriangle()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null || selected.name != "Envelope_Flap")
        {
            Debug.LogWarning("Pilih Envelope_Flap dahulu!");
            return;
        }

        MeshFilter filter = selected.GetComponent<MeshFilter>();

        if (filter == null)
        {
            Debug.LogError("Envelope_Flap tidak memiliki Mesh Filter.");
            return;
        }

        string path = "Assets/04_Models/EnvelopeTriangle.asset";

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "EnvelopeTriangle";

            // Tiga sudut lipatan amplop
            Vector3 topLeft = new Vector3(-0.5f, 0.5f, 0);
            Vector3 topRight = new Vector3(0.5f, 0.5f, 0);
            Vector3 bottomCenter = new Vector3(0, -0.2f, 0);

            // Dua sisi agar terlihat dari depan maupun belakang
            mesh.vertices = new Vector3[]
            {
                topLeft, topRight, bottomCenter,
                topLeft, bottomCenter, topRight
            };

            mesh.triangles = new int[]
            {
                0, 1, 2,
                3, 4, 5
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(0.5f, 0),

                new Vector2(0, 1),
                new Vector2(0.5f, 0),
                new Vector2(1, 1)
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
        }

        Undo.RecordObject(filter, "Set Envelope Triangle Mesh");
        filter.sharedMesh = mesh;

        EditorUtility.SetDirty(filter);

        Debug.Log("Envelope triangle mesh berhasil dipasang!");
    }
}
