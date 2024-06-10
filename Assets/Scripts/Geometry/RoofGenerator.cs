/*using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public static class RoofGenerator
{
    public static void CreateRoof(List<Vector3> positions, float buildingHeight, Material roofMaterial, ref List<Vector3> buildingPositions)
    {
        if (positions.Count < 3)
        {
            Debug.LogWarning("Not enough vertices to create a roof.");
            return;
        }

        Debug.Log($"Creating roof with {positions.Count} vertices at height {buildingHeight}");

        GameObject roof = new GameObject("Roof");
        MeshFilter meshFilter = roof.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = roof.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = roof.AddComponent<MeshCollider>();

        if (roofMaterial != null)
        {
            meshRenderer.material = roofMaterial;
            Debug.Log("Assigned roof material from public variable");
        }
        else
        {
            Debug.LogWarning("Roof material is missing.");
        }

        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;

        Vector3 center = Vector3.zero;
        foreach (var pos in positions)
        {
            center += pos;
        }
        center /= positions.Count;

        Vector3[] vertices = new Vector3[positions.Count];
        for (int i = 0; i < positions.Count; i++)
        {
            vertices[i] = positions[i] - center;
            vertices[i].y = buildingHeight;
        }

        Debug.Log("Vertices for roof created");
        Debug.Log($"Center of roof: {center}");

        List<Vector2> hull = Utility.ComputeConvexHull(vertices.Select(v => new Vector2(v.x, v.z)).ToList());

        if (hull.Count < 3)
        {
            Debug.LogWarning("Not enough vertices in the convex hull to create a roof.");
            return;
        }

        int[] indices = Utility.Triangulate(hull.ToArray());

        if (indices.Length == 0)
        {
            Debug.LogWarning("No triangles generated for the roof.");
            return;
        }

        Debug.Log($"Generated {indices.Length / 3} triangles for the roof");

        mesh.vertices = hull.Select(v => new Vector3(v.x, buildingHeight, v.y)).ToArray();
        mesh.triangles = indices;
        mesh.RecalculateNormals();

        meshCollider.sharedMesh = mesh;

        Vector2[] uvs = new Vector2[mesh.vertices.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(mesh.vertices[i].x, mesh.vertices[i].z);
        }
        mesh.uv = uvs;

#if UNITY_EDITOR
        string folderPath = "Assets/GeneratedMeshes";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string meshPath = $"{folderPath}/Roof_{roof.GetInstanceID()}.asset";
        UnityEditor.AssetDatabase.CreateAsset(mesh, meshPath);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"Mesh saved at {meshPath}");
#endif

        roof.transform.position = center;
        buildingPositions.Add(roof.transform.position);
        Debug.Log("Roof created at position: " + roof.transform.position);
    }
}*/
