/*using System.Collections.Generic;
using OsmSharp;
using UnityEngine;
using System.Linq;

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
        float mergedHeight = Mathf.Max(b1.Height, b2.Height);
        return new Building(mergedPositions, mergedHeight, b1.RelatedWays.Union(b2.RelatedWays).ToList(), b1.Nodes);
    }
}*/