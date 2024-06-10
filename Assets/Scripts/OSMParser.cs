using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using UnityEngine;

public class OSMParser : MonoBehaviour
{
    public GameObject wallPrefab;
    public GameObject groundPrefab;
    public Material roadMaterial;
    public Material roofMaterial;
    public string osmFilePath;
    private Vector2 minCoords;
    private Vector2 maxCoords;
    public float scaleFactor = 2000;
    public float roadHeight = 0;
    public float roadWidth = 20f;
    public Vector3 mapCenter = Vector3.zero;
    public float defaultBuildingHeight = 15f;
    public Mesh roadMesh;
    public bool isRoofCreated;

    private List<Vector3> generatedPositions = new List<Vector3>();
    private List<Vector3> roadPositions = new List<Vector3>();
    private List<Vector3> buildingPositions = new List<Vector3>();
    private List<Building> buildings = new List<Building>();

    public void ParseOSMData(string filePath)
    {
        try
        {
            using (var fileStream = new FileInfo(filePath).OpenRead())
            {
                var source = new XmlOsmStreamSource(fileStream);
                Dictionary<long, Node> nodes = new Dictionary<long, Node>();
                Dictionary<long, Way> ways = new Dictionary<long, Way>();
                List<Relation> relations = new List<Relation>();
                HashSet<long> processedWays = new HashSet<long>();

                foreach (var element in source)
                {
                    if (element.Type == OsmGeoType.Node)
                    {
                        var node = (Node)element;
                        if (node.Id.HasValue)
                        {
                            nodes[node.Id.Value] = node;
                        }
                    }
                    else if (element.Type == OsmGeoType.Way)
                    {
                        var way = (Way)element;
                        if (way.Id.HasValue)
                        {
                            ways[way.Id.Value] = way;
                        }
                    }
                    else if (element.Type == OsmGeoType.Relation)
                    {
                        var relation = (Relation)element;
                        if (relation.Id.HasValue)
                        {
                            relations.Add(relation);
                        }
                    }
                }

                FindBoundingBox(nodes.Values);

                foreach (var relation in relations)
                {
                    List<Vector3> relationPositions = new List<Vector3>();
                    HashSet<long> relatedWayIds = new HashSet<long>();

                    foreach (var member in relation.Members)
                    {
                        if (member.Type == OsmGeoType.Way && ways.ContainsKey(member.Id))
                        {
                            var way = ways[member.Id];
                            if (way.Tags == null)
                            {
                                way.Tags = new TagsCollection();
                            }

                            foreach (var tag in relation.Tags)
                            {
                                if (!way.Tags.ContainsKey(tag.Key))
                                {
                                    way.Tags.Add(tag);
                                }
                            }

                            bool isBuilding = way.Tags.ContainsKey("building") || way.Tags.ContainsKey("building:levels") || way.Tags.ContainsKey("building:part");

                            if (isBuilding)
                            {
                                relationPositions.AddRange(GetWayPositions(way, nodes));
                                relatedWayIds.Add(way.Id.Value);
                                processedWays.Add(way.Id.Value);
                            }
                        }
                    }

                    if (relationPositions.Count > 1)
                    {
                        UnityEngine.Debug.Log($"Relation ID {relation.Id}: related way IDs: {string.Join(", ", relatedWayIds)}");
                        var relatedWays = relatedWayIds.Select(id => ways[id]).ToList();
                        CreateBuildingFromPositions(relationPositions, new TagsCollection(relation.Tags), relation.Id.Value, relatedWays, nodes);
                    }

                    foreach (var wayId in relatedWayIds)
                    {
                        processedWays.Add(wayId);
                    }
                }

                foreach (var way in ways.Values)
                {
                    if (processedWays.Contains(way.Id.Value))
                    {
                        continue;
                    }

                    if (way.Tags == null)
                    {
                        UnityEngine.Debug.LogWarning($"Way Tags is null for way id: {way?.Id}");
                        continue;
                    }

                    bool isBuilding = way.Tags.ContainsKey("building") || way.Tags.ContainsKey("building:levels") || way.Tags.ContainsKey("building:part");
                    bool isHighway = way.Tags.ContainsKey("highway") &&
                                     (way.Tags["highway"] == "primary" ||
                                      way.Tags["highway"] == "tertiary" ||
                                      way.Tags["highway"] == "residential");

                    if (isBuilding)
                    {
                        UnityEngine.Debug.Log($"Processing building with way ID: {way.Id}");
                        CreateBuildingFromWay(way, nodes);
                    }
                    else if (isHighway)
                    {
                        CreateRoad(way, nodes);
                    }
                }

                MergeConnectedBuildings();

                Vector3[] boundingBox = GetBoundingBox(generatedPositions);
                CreateGround(boundingBox);
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Error parsing OSM data: " + ex.Message);
            UnityEngine.Debug.LogError("StackTrace: " + ex.StackTrace);
        }
    }

    void MergeConnectedBuildings()
    {
        Dictionary<int, List<Building>> buildingClusters = new Dictionary<int, List<Building>>();
        int clusterId = 0;

        foreach (var building in buildings)
        {
            bool merged = false;
            foreach (var cluster in buildingClusters)
            {
                foreach (var clusteredBuilding in cluster.Value)
                {
                    if (building.Overlaps(clusteredBuilding))
                    {
                        cluster.Value.Add(building);
                        merged = true;
                        break;
                    }
                }
                if (merged) break;
            }
            if (!merged)
            {
                buildingClusters[clusterId++] = new List<Building> { building };
            }
        }

        List<Building> mergedBuildings = new List<Building>();
        foreach (var cluster in buildingClusters.Values)
        {
            if (cluster.Count > 1)
            {
                Building mergedBuilding = cluster[0];
                for (int i = 1; i < cluster.Count; i++)
                {
                    mergedBuilding = Building.Merge(mergedBuilding, cluster[i]);
                }
                mergedBuildings.Add(mergedBuilding);
            }
            else
            {
                mergedBuildings.Add(cluster[0]);
            }
        }

        buildings = mergedBuildings;

        foreach (var building in buildings)
        {
            GenerateBuildingWalls(building);
        }
    }

    List<Vector3> GetWayPositions(Way way, Dictionary<long, Node> nodes)
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var nodeId in way.Nodes)
        {
            if (nodes.ContainsKey(nodeId))
            {
                var node = nodes[nodeId];
                var position = NormalizeCoordinates((float)node.Longitude, (float)node.Latitude);
                positions.Add(position);
            }
        }
        return positions;
    }

    void CreateBuildingFromPositions(List<Vector3> positions, TagsCollectionBase tags, long? relationId, List<Way> relatedWays, Dictionary<long, Node> nodes)
    {
        if (positions.Count > 1)
        {
            int levels = 1;
            if (tags.ContainsKey("building:levels"))
            {
                int.TryParse(tags["building:levels"], out levels);
            }

            float buildingHeight = levels * defaultBuildingHeight;
            Building newBuilding = new Building(positions, buildingHeight, relatedWays, nodes);

            buildings.Add(newBuilding);

            GameObject buildingRoot = new GameObject($"Building_{relationId ?? 0}");
            buildingRoot.transform.parent = this.transform;
            buildingRoot.tag = "Building"; // Добавляем тег "Building"

            foreach (var position in positions)
            {
                var obj = Instantiate(wallPrefab, position, Quaternion.identity);
                obj.transform.parent = buildingRoot.transform;
            }

            if (isRoofCreated) CreateRoof(positions, buildingHeight, buildingRoot.transform);

            UnityEngine.Debug.Log($"Building created from relation ID {relationId}");
        }
    }

    void CreateBuildingFromWay(Way way, Dictionary<long, Node> nodes)
    {
        List<Vector3> positions = GetWayPositions(way, nodes);
        CreateBuildingFromPositions(positions, way.Tags, way.Id.Value, new List<Way> { way }, nodes);
    }

    void GenerateBuildingWalls(Building building)
    {
        HashSet<string> generatedWalls = new HashSet<string>();
        var contours = GetContours(building.RelatedWays, building.Nodes);

        foreach (var contour in contours)
        {
            for (int i = 0; i < contour.Count - 1; i++)
            {
                Vector3 startPosition = contour[i];
                Vector3 endPosition = contour[i + 1];

                string wallKey = $"{startPosition}-{endPosition}";
                string reverseWallKey = $"{endPosition}-{startPosition}";

                if (generatedWalls.Contains(wallKey) || generatedWalls.Contains(reverseWallKey))
                {
                    continue;
                }

                generatedWalls.Add(wallKey);

                Vector3 wallPosition = (startPosition + endPosition) / 2;
                Vector3 direction = endPosition - startPosition;
                float length = direction.magnitude;
                Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

                var wall = Instantiate(wallPrefab, wallPosition, rotation);
                wall.transform.localScale = new Vector3(0.1f, building.Height, length);
                wall.transform.position = new Vector3(wall.transform.position.x, building.Height / 2, wall.transform.position.z);
                wall.transform.parent = this.transform;

                buildingPositions.Add(wall.transform.position);

                UnityEngine.Debug.Log($"Wall created from {startPosition} to {endPosition} with height: {building.Height}");
            }
        }

        if (isRoofCreated) CreateRoof(building.Positions, building.Height, this.transform);
    }

    List<List<Vector3>> GetContours(List<Way> relatedWays, Dictionary<long, Node> nodes)
    {
        List<List<Vector3>> contours = new List<List<Vector3>>();
        HashSet<long> processedNodes = new HashSet<long>();

        foreach (var way in relatedWays)
        {
            List<Vector3> positions = new List<Vector3>();
            foreach (var nodeId in way.Nodes)
            {
                if (nodes.ContainsKey(nodeId) && !processedNodes.Contains(nodeId))
                {
                    var node = nodes[nodeId];
                    var position = NormalizeCoordinates((float)node.Longitude, (float)node.Latitude);
                    positions.Add(position);
                    processedNodes.Add(nodeId);
                }
            }
            if (positions.Count > 0)
            {
                if (positions[0] != positions[positions.Count - 1])
                {
                    positions.Add(positions[0]);
                }
                contours.Add(positions);
            }
        }
        return contours;
    }

void CreateRoof(List<Vector3> positions, float buildingHeight, Transform parent)
{
    if (positions.Count < 3)
    {
        Debug.LogWarning("Not enough vertices to create a roof.");
        return;
    }

    Debug.Log($"Creating roof with {positions.Count} vertices at height {buildingHeight}");

    GameObject roof = new GameObject("Roof");
    roof.transform.parent = parent;
    roof.tag = "BuildingRoof"; // Устанавливаем тег "BuildingRoof" для крыши
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
        vertices[i].y = 0; // Высота относительно центра, поэтому здесь 0
    }

    Debug.Log("Vertices for roof created");
    Debug.Log($"Center of roof: {center}");

    List<Vector2> hull = ComputeConvexHull(vertices.Select(v => new Vector2(v.x, v.z)).ToList());

    if (hull.Count < 3)
    {
        Debug.LogWarning("Not enough vertices in the convex hull to create a roof.");
        return;
    }

    int[] indices = Triangulate(hull.ToArray());

    if (indices.Length == 0)
    {
        Debug.LogWarning("No triangles generated for the roof.");
        return;
    }

    Debug.Log($"Generated {indices.Length / 3} triangles for the roof");

    mesh.vertices = hull.Select(v => new Vector3(v.x, 0, v.y)).ToArray(); // Устанавливаем высоту вершин как 0
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
    string folderPath = "Assets/GeneratedRoofs";
    if (!Directory.Exists(folderPath))
    {
        Directory.CreateDirectory(folderPath);
    }

    string meshPath = $"{folderPath}/Roof_{roof.GetInstanceID()}.asset";
    AssetDatabase.CreateAsset(mesh, meshPath);
    AssetDatabase.SaveAssets();
    Debug.Log($"Mesh saved at {meshPath}");
#endif

    // Устанавливаем позицию крыши относительно позиции здания
    roof.transform.localPosition = new Vector3(center.x, buildingHeight, center.z);

    buildingPositions.Add(roof.transform.position);
    Debug.Log("Roof created at position: " + roof.transform.position);
}

#if UNITY_EDITOR
[MenuItem("Tools/Clear Generated Roofs")]
private static void ClearGeneratedRoofs()
{
    string folderPath = "Assets/GeneratedRoofs";
    if (Directory.Exists(folderPath))
    {
        DirectoryInfo di = new DirectoryInfo(folderPath);
        foreach (FileInfo file in di.GetFiles())
        {
            file.Delete();
        }
        foreach (DirectoryInfo dir in di.GetDirectories())
        {
            dir.Delete(true);
        }
        AssetDatabase.Refresh();
        Debug.Log("Generated roofs cleared.");
    }
}
#endif

    List<Vector2> ComputeConvexHull(List<Vector2> points)
    {
        if (points.Count <= 1)
            return points;

        points = points.OrderBy(p => p.x).ThenBy(p => p.y).ToList();

        List<Vector2> hull = new List<Vector2>();

        foreach (Vector2 point in points)
        {
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }
            hull.Add(point);
        }

        int t = hull.Count + 1;
        for (int i = points.Count - 2; i >= 0; i--)
        {
            Vector2 point = points[i];
            while (hull.Count >= t && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }
            hull.Add(point);
        }

        hull.RemoveAt(hull.Count - 1);
        return hull;
    }

    float Cross(Vector2 O, Vector2 A, Vector2 B)
    {
        return (A.x - O.x) * (B.y - O.y) - (A.y - O.y) * (B.x - O.x);
    }

    int[] Triangulate(Vector2[] vertices)
    {
        List<int> indices = new List<int>();

        int n = vertices.Length;
        if (n < 3)
            return indices.ToArray();

        int[] V = new int[n];
        if (Area(vertices) > 0)
        {
            for (int v = 0; v < n; v++) V[v] = v;
        }
        else
        {
            for (int v = 0; n > 0 && v < n; v++) V[v] = (n - 1) - v;
        }

        int nv = n;
        int count = 2 * nv;
        for (int m = 0, v = nv - 1; nv > 2;)
        {
            if ((count--) <= 0)
                return indices.ToArray();

            int u = v;
            if (nv <= u) u = 0;
            v = u + 1;
            if (nv <= v) v = 0;
            int w = v + 1;
            if (nv <= w) w = 0;

            if (Snip(u, v, w, nv, V, vertices))
            {
                int a, b, c, s, t;
                a = V[u];
                b = V[v];
                c = V[w];
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
                m++;
                for (s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
                nv--;
                count = 2 * nv;
            }
        }

        indices.Reverse();
        return indices.ToArray();
    }

    float Area(Vector2[] vertices)
    {
        int n = vertices.Length;
        float A = 0.0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            Vector2 pval = vertices[p];
            Vector2 qval = vertices[q];
            A += pval.x * qval.y - qval.x * pval.y;
        }
        return A * 0.5f;
    }

    bool Snip(int u, int v, int w, int n, int[] V, Vector2[] vertices)
    {
        int p;
        Vector2 A = vertices[V[u]];
        Vector2 B = vertices[V[v]];
        Vector2 C = vertices[V[w]];
        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            return false;
        for (p = 0; p < n; p++)
        {
            if ((p == u) || (p == v) || (p == w))
                continue;
            Vector2 P = vertices[V[p]];
            if (InsideTriangle(A, B, C, P))
                return false;
        }
        return true;
    }

    bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        float ax, ay, bx, by, cx, cy, px, py, cCROSSap, bCROSScp, aCROSSbp;
        ax = C.x - B.x; ay = C.y - B.y;
        bx = A.x - C.x; by = A.y - C.y;
        cx = B.x - A.x; cy = B.y - A.y;
        px = P.x - A.x; py = P.y - A.y;

        aCROSSbp = ax * py - ay * px;
        px = P.x - B.x; py = P.y - B.y;
        bCROSScp = bx * py - by * px;
        px = P.x - C.x; py = P.y - C.y;
        cCROSSap = cx * py - cy * px;

        return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
    }

    void CreateRoad(Way way, Dictionary<long, Node> nodes)
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var nodeId in way.Nodes)
        {
            if (nodes.ContainsKey(nodeId))
            {
                var node = nodes[nodeId];
                var position = NormalizeCoordinates((float)node.Longitude, (float)node.Latitude, roadHeight);
                positions.Add(position);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Node with id {nodeId} not found for way {way.Id}");
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
                CreateRoadSegment(start, end, way.Id.GetValueOrDefault(), i, adjustedRoadWidth);
            }

            roadPositions.AddRange(positions);
        }
        else
        {
            UnityEngine.Debug.LogWarning("Not enough points to create a road for way with id: " + way.Id);
        }
    }

    void CreateRoadSegment(Vector3 start, Vector3 end, long wayId, int segmentIndex, float roadWidth)
    {
        var roadSegment = new GameObject($"Road_{wayId}_Segment_{segmentIndex}");

        Vector3 direction = (end - start).normalized;
        Vector3 segmentPosition = (start + end) / 2;
        float segmentLength = Vector3.Distance(start, end);

        float roadElevation = roadHeight + 0.1f; // 0.1 - небольшое значение для подъема
        start.y = roadElevation;
        end.y = roadElevation;

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
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        // Назначение слоя "Roads"
        roadSegment.layer = LayerMask.NameToLayer("Roads");
        roadSegment.tag = "Road"; // Добавляем тег "Road"

        roadSegment.transform.parent = this.transform;
        roadPositions.Add(roadSegment.transform.position);
        UnityEngine.Debug.Log($"Road segment created for way id: {wayId}, segment index: {segmentIndex}");
    }

    void PositionCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && generatedPositions.Count > 0)
        {
            Vector3 center = GetCenterPosition(generatedPositions);
            mainCamera.transform.position = center + new Vector3(0, 100, -100);
            mainCamera.transform.LookAt(center);
        }
    }

    Vector3 GetCenterPosition(List<Vector3> positions)
    {
        Vector3 sum = Vector3.zero;
        foreach (var pos in positions)
        {
            sum += pos;
        }
        return sum / positions.Count;
    }

    void FindBoundingBox(IEnumerable<Node> nodes)
    {
        float minLat = float.MaxValue;
        float minLon = float.MaxValue;
        float maxLat = float.MinValue;
        float maxLon = float.MinValue;

        foreach (var node in nodes)
        {
            if (node.Latitude < minLat) minLat = (float)node.Latitude;
            if (node.Longitude < minLon) minLon = (float)node.Longitude;
            if (node.Latitude > maxLat) maxLat = (float)node.Latitude;
            if (node.Longitude > maxLon) maxLon = (float)node.Longitude;
        }

        minCoords = new Vector2(minLon, minLat);
        maxCoords = new Vector2(maxLon, maxLat);
        UnityEngine.Debug.Log($"BoundingBox - Min: {minCoords}, Max: {maxCoords}");
    }

    void CreateGround(Vector3[] boundingBox)
    {
        Vector3 bottomLeft = boundingBox[0];
        Vector3 topRight = boundingBox[1];
        Vector3 center = (bottomLeft + topRight) / 2;

        float width = topRight.x - bottomLeft.x;
        float height = topRight.z - bottomLeft.z;

        GameObject ground = Instantiate(groundPrefab, center, Quaternion.identity);
        ground.transform.localScale = new Vector3(width / 10f, 1, height / 10f);
        ground.transform.position = new Vector3(center.x, roadHeight - 0.001f, center.z);

        // Назначение слоя "Ground"
        ground.layer = LayerMask.NameToLayer("Ground");
        ground.transform.parent = this.transform;
    }

    Vector3 NormalizeCoordinates(float lon, float lat, float height = 0)
    {
        float mercX = lon * 20037508.34f / 180f;
        float mercY = (float)(Math.Log(Math.Tan((90f + lat) * Math.PI / 360f)) / (Math.PI / 180f));
        mercY = mercY * 20037508.34f / 180f;

        float centerX = (minCoords.x + maxCoords.x) / 2 * 20037508.34f / 180f;
        float centerY = (float)(Math.Log(Math.Tan((90f + (minCoords.y + maxCoords.y) / 2) * Math.PI / 360f)) / (Math.PI / 180f)) * 20037508.34f / 180f;

        float normalizedX = (mercX - centerX) / 1000f * scaleFactor;
        float normalizedZ = (mercY - centerY) / 1000f * scaleFactor;

        Vector3 normalizedPosition = new Vector3(normalizedX, height, normalizedZ);
        generatedPositions.Add(normalizedPosition);

        return normalizedPosition;
    }
    Vector3[] GetBoundingBox(List<Vector3> positions)
    {
        if (positions == null || positions.Count == 0)
            return new Vector3[] { Vector3.zero, Vector3.zero };

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float minZ = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        float maxZ = float.MinValue;

        foreach (var pos in positions)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.z < minZ) minZ = pos.z;

            if (pos.x > maxX) maxX = pos.x;
            if (pos.y > maxY) maxY = pos.y;
            if (pos.z > maxZ) maxZ = pos.z;
        }

        return new Vector3[] { new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ) };
    }

    public List<Vector3> GetGeneratedPositions()
    {
        return generatedPositions;
    }
    public List<Vector3> GetRoadPositions()
    {
        return roadPositions;
    }

    public List<Vector3> GetBuildingPositions()
    {
        return buildingPositions;
    }
}

public class Building
{
    public List<Vector3> Positions { get; private set; }
    public float Height { get; private set; }
    public List<Way> RelatedWays { get; private set; }
    public Dictionary<long, Node> Nodes { get; private set; }

    public Building(List<Vector3> positions, float height, List<Way> relatedWays, Dictionary<long, Node> nodes)
    {
        Positions = positions;
        Height = height;
        RelatedWays = relatedWays;
        Nodes = nodes;
    }

    public bool Overlaps(Building other)
    {
        foreach (var pos in Positions)
        {
            if (other.Positions.Contains(pos))
            {
                return true;
            }
        }
        return false;
    }

    public static Building Merge(Building b1, Building b2)
    {
        List<Vector3> mergedPositions = b1.Positions.Union(b2.Positions).ToList();
        float mergedHeight = Math.Max(b1.Height, b2.Height);
        return new Building(mergedPositions, mergedHeight, b1.RelatedWays.Union(b2.RelatedWays).ToList(), b1.Nodes);
    }

    public static List<(Vector3, Vector3)> MergeWalls(List<(Vector3, Vector3)> walls)
    {
        Dictionary<string, (Vector3, Vector3)> uniqueWalls = new Dictionary<string, (Vector3, Vector3)>();

        foreach (var wall in walls)
        {
            string wallKey = $"{wall.Item1}-{wall.Item2}";
            string reverseWallKey = $"{wall.Item2}-{wall.Item1}";

            if (!uniqueWalls.ContainsKey(wallKey) && !uniqueWalls.ContainsKey(reverseWallKey))
            {
                uniqueWalls[wallKey] = wall;
            }
            else if (uniqueWalls.ContainsKey(reverseWallKey))
            {
                continue;
            }
        }

        return uniqueWalls.Values.ToList();
    }
}
