/*using System.Collections.Generic;
using System;
using OsmSharp;
using System.Linq;
using UnityEngine;

public static class Utility
{
    public static Vector3 NormalizeCoordinates(float lon, float lat, float height = 0)
    {
        float mercX = lon * 20037508.34f / 180f;
        float mercY = (float)(Math.Log(Math.Tan((90f + lat) * Math.PI / 360f)) / (Math.PI / 180f));
        mercY = mercY * 20037508.34f / 180f;

        float centerX = (minCoords.x + maxCoords.x) / 2 * 20037508.34f / 180f;
        float centerY = (float)(Math.Log(Math.Tan((90f + (minCoords.y + maxCoords.y) / 2) * Math.PI / 360f)) / (Math.PI / 180f)) * 20037508.34f / 180f;

        float normalizedX = (mercX - centerX) / 1000f * scaleFactor;
        float normalizedZ = (mercY - centerY) / 1000f * scaleFactor;

        Vector3 normalizedPosition = new Vector3(normalizedX, height, normalizedZ) + mapCenter;
        return normalizedPosition;
    }

    public static Vector3 GetCenterPosition(List<Vector3> positions)
    {
        Vector3 sum = Vector3.zero;
        foreach (var pos in positions)
        {
            sum += pos;
        }
        return sum / positions.Count;
    }

    public static Vector3 GetRandomPositionOnRoad(List<Vector3> roadPositions, float roadWidth, float roadHeight, List<Vector3> buildingPositions)
    {
        Vector3 randomPosition = Vector3.zero;
        bool validPosition = false;

        while (!validPosition)
        {
            int randomIndex = UnityEngine.Random.Range(0, roadPositions.Count);
            Vector3 roadPosition = roadPositions[randomIndex];

            float offsetX = UnityEngine.Random.Range(-roadWidth / 2f, roadWidth / 2f);
            float offsetZ = UnityEngine.Random.Range(-roadWidth / 2f, roadWidth / 2f);

            randomPosition = new Vector3(roadPosition.x + offsetX, roadHeight, roadPosition.z + offsetZ);
            validPosition = IsValidRoadPosition(randomPosition, roadPositions, roadWidth, buildingPositions);
        }

        Debug.Log($"Generated Random Position on Road: {randomPosition}");
        return randomPosition;
    }

    public static bool IsValidRoadPosition(Vector3 position, List<Vector3> roadPositions, float roadWidth, List<Vector3> buildingPositions)
    {
        foreach (var roadPosition in roadPositions)
        {
            if (Vector3.Distance(position, roadPosition) < roadWidth / 2f)
            {
                foreach (var buildingPosition in buildingPositions)
                {
                    if (Vector3.Distance(position, buildingPosition) < roadWidth)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        return false;
    }

    public static List<List<Vector3>> GetContours(List<Way> relatedWays, Dictionary<long, Node> nodes)
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

    public static List<Vector2> ComputeConvexHull(List<Vector2> points)
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

    static float Cross(Vector2 O, Vector2 A, Vector2 B)
    {
        return (A.x - O.x) * (B.y - O.y) - (A.y - O.y) * (B.x - O.x);
    }

    public static int[] Triangulate(Vector2[] vertices)
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
            for (int v = 0; v < n; v++) V[v] = (n - 1) - v;
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

    static float Area(Vector2[] vertices)
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

    static bool Snip(int u, int v, int w, int n, int[] V, Vector2[] vertices)
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

    static bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
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
}
*/