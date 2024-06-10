/*using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Tags;
using System.Linq;

using UnityEngine;

public class BuildingGenerator
{
    private GameObject wallPrefab;
    private GameObject groundPrefab;
    private Material roofMaterial;
    private float defaultBuildingHeight;

    public BuildingGenerator(GameObject wallPrefab, GameObject groundPrefab, Material roofMaterial, float defaultBuildingHeight)
    {
        this.wallPrefab = wallPrefab;
        this.groundPrefab = groundPrefab;
        this.roofMaterial = roofMaterial;
        this.defaultBuildingHeight = defaultBuildingHeight;
    }

    public List<Building> GenerateBuildings((Dictionary<long, Node> nodes, Dictionary<long, Way> ways, List<Relation> relations) parsedData, ref List<Vector3> buildingPositions)
    {
        List<Building> buildings = new List<Building>();
        Dictionary<long, Way> ways = parsedData.ways;
        Dictionary<long, Node> nodes = parsedData.nodes;
        List<Relation> relations = parsedData.relations;

        HashSet<long> processedWays = new HashSet<long>();

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
                var relatedWays = relatedWayIds.Select(id => ways[id]).ToList();
                CreateBuildingFromPositions(relationPositions, new TagsCollection(relation.Tags), relation.Id.Value, relatedWays, nodes, ref buildings, ref buildingPositions);
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
                Debug.LogWarning($"Way Tags is null for way id: {way?.Id}");
                continue;
            }

            bool isBuilding = way.Tags.ContainsKey("building") || way.Tags.ContainsKey("building:levels") || way.Tags.ContainsKey("building:part");

            if (isBuilding)
            {
                CreateBuildingFromWay(way, nodes, ref buildings, ref buildingPositions);
            }
        }

        MergeConnectedBuildings(ref buildings);
        return buildings;
    }

    List<Vector3> GetWayPositions(Way way, Dictionary<long, Node> nodes)
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var nodeId in way.Nodes)
        {
            if (nodes.ContainsKey(nodeId))
            {
                var node = nodes[nodeId];
                var position = Utility.NormalizeCoordinates((float)node.Longitude, (float)node.Latitude);
                positions.Add(position);
            }
        }
        return positions;
    }

    void CreateBuildingFromPositions(List<Vector3> positions, TagsCollectionBase tags, long? relationId, List<Way> relatedWays, Dictionary<long, Node> nodes, ref List<Building> buildings, ref List<Vector3> buildingPositions)
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

            if (relationId.HasValue)
            {
                Debug.Log($"Building created from relation ID {relationId}");
            }
            else
            {
                Debug.Log($"Building created from way ID {tags["id"]}");
            }

            GenerateBuildingWalls(newBuilding, ref buildingPositions);
        }
    }

    void CreateBuildingFromWay(Way way, Dictionary<long, Node> nodes, ref List<Building> buildings, ref List<Vector3> buildingPositions)
    {
        List<Vector3> positions = GetWayPositions(way, nodes);
        CreateBuildingFromPositions(positions, way.Tags, way.Id.Value, new List<Way> { way }, nodes, ref buildings, ref buildingPositions);
    }

    void GenerateBuildingWalls(Building building, ref List<Vector3> buildingPositions)
    {
        HashSet<string> generatedWalls = new HashSet<string>();
        var contours = Utility.GetContours(building.RelatedWays, building.Nodes);

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

                var wall = UnityEngine.Object.Instantiate(wallPrefab, wallPosition, rotation);
                wall.transform.localScale = new Vector3(0.1f, building.Height, length);
                wall.transform.position = new Vector3(wall.transform.position.x, building.Height / 2, wall.transform.position.z);
                buildingPositions.Add(wall.transform.position);

                Debug.Log($"Wall created from {startPosition} to {endPosition} with height: {building.Height}");
            }
        }

        RoofGenerator.CreateRoof(building.Positions, building.Height, roofMaterial, ref buildingPositions);
    }

    void MergeConnectedBuildings(ref List<Building> buildings)
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
    }
}*/
