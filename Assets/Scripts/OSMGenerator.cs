/*using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OSMGenerator : MonoBehaviour
{
    public GameObject wallPrefab;
    public GameObject groundPrefab;
    public Material roadMaterial;
    public Material roofMaterial;
    public GameObject objectPrefab;
    public int objectCount = 10;
    public float scaleFactor = 2000f;
    public float roadHeight = 0;
    public float roadWidth = 20f;
    public Vector3 mapCenter = Vector3.zero;
    public float defaultBuildingHeight = 15f;
    public Mesh roadMesh;

    private List<Vector3> generatedPositions = new List<Vector3>();
    private List<Vector3> roadPositions = new List<Vector3>();
    private List<Vector3> buildingPositions = new List<Vector3>();
    private List<Building> buildings = new List<Building>();

    private void Start()
    {
        string osmFilePath = Path.Combine(Application.dataPath, "OSMMaps/OSMMap1.osm");
        Debug.Log("OSM file path: " + osmFilePath);
        OSMParser osmParser = new OSMParser();
        var parsedData = osmParser.ParseOSMData(osmFilePath);

        var buildingGenerator = new BuildingGenerator(wallPrefab, groundPrefab, roofMaterial, defaultBuildingHeight);
        buildings = buildingGenerator.GenerateBuildings(parsedData, ref buildingPositions);

        var roadGenerator = new RoadGenerator(roadMaterial, roadWidth, roadHeight, roadMesh);
        roadPositions = roadGenerator.GenerateRoads(parsedData);

        PositionCamera();
        GenerateObjectsOnRoads();
    }

    void PositionCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && generatedPositions.Count > 0)
        {
            Vector3 center = Utility.GetCenterPosition(generatedPositions);
            mainCamera.transform.position = center + new Vector3(0, 100, -100);
            mainCamera.transform.LookAt(center);
        }
    }

    void GenerateObjectsOnRoads()
    {
        for (int i = 0; i < objectCount; i++)
        {
            Vector3 randomPosition = Utility.GetRandomPositionOnRoad(roadPositions, roadWidth, roadHeight, buildingPositions);
            Instantiate(objectPrefab, randomPosition, Quaternion.identity);
        }
    }
}*/
