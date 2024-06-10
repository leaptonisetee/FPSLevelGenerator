using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

public class GeometryGenerator : MonoBehaviour
{
    public OSMParser osmParser; // Ссылка на компонент OSMParser
    public string prefabSavePath = "Assets/GeneratedPrefabs/GeneratedGeometry.prefab"; // Путь сохранения префаба

    public void GenerateAndSaveGeometry()
    {
        // Убедитесь, что все дочерние объекты сгенерированы и являются дочерними OSMParser
        osmParser.ParseOSMData(osmParser.osmFilePath);
        GameObject geometryRoot = new GameObject("GeneratedGeometry");

        // Перенос всех дочерних объектов из OSMParser в geometryRoot
        foreach (Transform child in osmParser.transform)
        {
            GameObject obj = Instantiate(child.gameObject, geometryRoot.transform);
            obj.name = child.name; // сохраняем оригинальные имена объектов
            UnityEngine.Debug.Log($"Object {obj.name} added to geometryRoot");
        }

        SaveAsPrefab(geometryRoot);
    }

    private void SaveAsPrefab(GameObject geometryRoot)
    {
#if UNITY_EDITOR
        if (!System.IO.Directory.Exists("Assets/GeneratedPrefabs"))
        {
            System.IO.Directory.CreateDirectory("Assets/GeneratedPrefabs");
        }

        PrefabUtility.SaveAsPrefabAsset(geometryRoot, prefabSavePath);
        DestroyImmediate(geometryRoot);
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GeometryGenerator))]
    public class GeometryGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GeometryGenerator generator = (GeometryGenerator)target;
            if (GUILayout.Button("Generate and Save Geometry"))
            {
                generator.GenerateAndSaveGeometry();
            }
        }
    }
#endif
}