using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public GameObject geometryPrefab;
    public GameObject levelRoot;
    public ArenaGenerator arenaGenerator;
    public SniperPositionGenerator sniperPositionGenerator;
    public NarrowPositionGenerator narrowPositionGenerator;
    public bool arenas = true;
    public bool sniperPositions = true;
    public bool narrowPositions = true;
    public void GenerateLevel()
    {
        if (geometryPrefab == null)
        {
            Debug.LogError("Geometry Prefab is not assigned.");
            return;
        }

        if (levelRoot != null)
        {
            DestroyImmediate(levelRoot);
        }

        levelRoot = Instantiate(geometryPrefab);
        levelRoot.name = "GeneratedLevel";

        if (arenas)
        {
            if (arenaGenerator != null)
            {
                arenaGenerator.GenerateArenas(levelRoot);
            }
            else
            {
                Debug.LogWarning("ArenaGenerator is not assigned.");
            }
        }

        if (sniperPositions)
        {
            if (sniperPositionGenerator != null)
            {
                sniperPositionGenerator.GenerateSniperPositions(levelRoot);
            }
            else
            {
                Debug.LogWarning("SniperPosinitonGenerator is not assigned.");
            }
        }
        if (narrowPositions)
        {
            if (arenaGenerator != null)
            {
                narrowPositionGenerator.GenerateNarrowPositions(levelRoot);
            }
            else
            {
                Debug.LogWarning("NarrowPositionGenerator is not assigned.");
            }
        }
    
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(LevelGenerator))]
    public class LevelGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LevelGenerator generator = (LevelGenerator)target;
            if (GUILayout.Button("Generate Level"))
            {
                generator.GenerateLevel();
            }
        }
    }
#endif
}