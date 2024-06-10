/*using System.Collections.Generic;
using OsmSharp;
using UnityEngine;

public class RoadGenerator
{
    private Material roadMaterial;
    private float roadWidth;
    private float roadHeight;
    private Mesh roadMesh;

    public RoadGenerator(Material roadMaterial, float roadWidth, float roadHeight, Mesh roadMesh)
    {
        this.roadMaterial = roadMaterial;
        this.roadWidth = roadWidth;
        this.roadHeight = roadHeight;
        this.roadMesh = roadMesh;
    }

    public List<Vector3> GenerateRoads((Dictionary<long, Node> nodes, Dictionary<long, Way> ways, List<Relation> relations) parsedData)
    {
        List<Vector3> roadPositions = new List<Vector3>();
        Dictionary<long, Way> ways = parsedData.ways;
        Dictionary<long, Node> nodes = parsedData.nodes;

        foreach (var way in ways.Values)
        {
            if (way.Tags.ContainsKey("highway"))
            {
                CreateRoad(way, nodes, ref roadPositions);
            }
        }

        return roadPositions;
    }

    void CreateRoad(Way way, Dictionary<long, Node> nodes, ref List<Vector3> roadPositions)
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var nodeId in way.Nodes)
        {
            if (nodes.ContainsKey(nodeId))
            {
                var node = nodes[nodeId];
                var position = Utility.NormalizeCoordinates((float)node.Longitude, (float)node.Latitude, roadHeight);
                positions.Add(position);
            }
            else
            {
                Debug.LogWarning($"Node with id {nodeId} not found for way {way.Id}");
            }
        }

        int lanes = 1;
        if (way.Tags.ContainsKey("lanes"))
        {
            int.TryParse(way.Tags["lanes"], out lanes);
        }
        float adjustedRoadWidth = roadWidth * lanes;

        if (positions.Count > 1)
        {
            for (int i = 0; i < positions.Count - 1; i++)
            {
                Vector3 start = positions[i];
                Vector3 end = positions[i + 1];
                CreateRoadSegment(start, end, way.Id.GetValueOrDefault(), i, adjustedRoadWidth, ref roadPositions);
            }

            roadPositions.AddRange(positions);
        }
        else
        {
            Debug.LogWarning("Not enough points to create a road for way with id: " + way.Id);
        }
    }

    void CreateRoadSegment(Vector3 start, Vector3 end, long wayId, int segmentIndex, float roadWidth, ref List<Vector3> roadPositions)
    {
        var roadSegment = new GameObject($"Road_{wayId}_Segment_{segmentIndex}");

        Vector3 direction = (end - start).normalized;
        Vector3 segmentPosition = (start + end) / 2;
        float segmentLength = Vector3.Distance(start, end);

        roadSegment.transform.position = segmentPosition;
        roadSegment.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        roadSegment.transform.localScale = new Vector3(roadWidth / 10f, 1, segmentLength / 10f);

        var meshFilter = roadSegment.AddComponent<MeshFilter>();
        var meshRenderer = roadSegment.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = roadMaterial;

        if (roadMesh != null)
        {
            meshFilter.mesh = roadMesh;
        }
        else
        {
            Mesh mesh = new Mesh();

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            float halfWidth = roadWidth / 2f;
            float uvScale = 1.0f / roadWidth;

            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;

            Vector3 v1 = start - perpendicular * halfWidth - segmentPosition;
            Vector3 v2 = start + perpendicular * halfWidth - segmentPosition;
            Vector3 v3 = end - perpendicular * halfWidth - segmentPosition;
            Vector3 v4 = end + perpendicular * halfWidth - segmentPosition;

            vertices.AddRange(new Vector3[] { v1, v2, v3, v4 });

            int vertexIndex = 0;
            triangles.AddRange(new int[]
            {
                vertexIndex, vertexIndex + 1, vertexIndex + 2,
                vertexIndex + 1, vertexIndex + 3, vertexIndex + 2
            });

            float distance = Vector3.Distance(start, end);
            uvs.AddRange(new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, uvScale * distance),
                new Vector2(1, uvScale * distance)
            });

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }

        var meshCollider = roadSegment.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.mesh;

        roadPositions.Add(roadSegment.transform.position);
        Debug.Log($"Road segment created for way id: {wayId}, segment index: {segmentIndex}");
    }
}*/
