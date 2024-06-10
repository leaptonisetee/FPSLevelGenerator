using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Streams;

public static class OsmDataLoader
{
    public static void LoadData(XmlOsmStreamSource source, Dictionary<long, Node> nodes, Dictionary<long, Way> ways, List<Relation> relations)
    {
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
    }
}