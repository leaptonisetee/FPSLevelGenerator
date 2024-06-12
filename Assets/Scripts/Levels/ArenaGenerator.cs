using System.Collections.Generic;
using UnityEngine;

public class ArenaGenerator : MonoBehaviour
{
    public GameObject coverPrefab;
    public GameObject enemyPrefab;
    public GameObject interactivePrefab;
    public int minCoverCount = 5;
    public int maxCoverCount = 10;
    public int minEnemyCount = 3;
    public int maxEnemyCount = 7;
    public int minInteractiveCount = 2;
    public int maxInteractiveCount = 5;
    public float minOpenAreaSize = 30f; // Минимальная площадь открытого пространства для арены
    public float arenaWidth = 20f; // Ширина арены
    public float arenaLength = 30f; // Длина арены

    private List<GameObject> arenas = new List<GameObject>();
    public List<Rect> arenaBounds = new List<Rect>(); // Сделаем этот список публичным для доступа из других классов

    public void GenerateArenas(GameObject geometry)
    {
        if (geometry == null)
        {
            Debug.LogError("Geometry object is null");
            return;
        }

        arenas.Clear();
        arenaBounds.Clear();

        List<Vector3> roadPositions = new List<Vector3>();
        List<Vector3> buildingPositions = new List<Vector3>();

        // Находим все объекты с тегом "Road" и "Building"
        foreach (Transform child in geometry.transform)
        {
            if (child.CompareTag("Road"))
            {
                roadPositions.Add(child.position);
            }
            else if (child.CompareTag("Building"))
            {
                buildingPositions.Add(child.position);
            }
        }

        if (roadPositions.Count == 0)
        {
            Debug.LogWarning("No road positions found");
        }

        if (buildingPositions.Count == 0)
        {
            Debug.LogWarning("No building positions found");
        }

        List<Vector3> openAreas = FindOpenAreas(roadPositions, buildingPositions);

        // Создаем объект-контейнер для арен вне иерархии сгенерированного уровня
        GameObject arenaContainer = new GameObject("ArenaContainer");

        foreach (var area in openAreas)
        {
            if (IsSuitableForArena(area, roadPositions, buildingPositions))
            {
                Rect newArenaBounds = new Rect(area.x - arenaWidth / 2, area.z - arenaLength / 2, arenaWidth, arenaLength);
                if (!IsOverlappingWithExistingArenas(newArenaBounds))
                {
                    GameObject arena = CreateArena(area, arenaContainer.transform);
                    arenas.Add(arena);
                    arenaBounds.Add(newArenaBounds); // Добавляем границы новой арены
                }
            }
        }
    }

    List<Vector3> FindOpenAreas(List<Vector3> roadPositions, List<Vector3> buildingPositions)
    {
        List<Vector3> openAreas = new List<Vector3>();

        foreach (var roadPos in roadPositions)
        {
            bool isNearBuilding = false;

            foreach (var buildingPos in buildingPositions)
            {
                if (Vector3.Distance(roadPos, buildingPos) < 5f) // Проверка расстояния до зданий
                {
                    isNearBuilding = true;
                    break;
                }
            }

            if (!isNearBuilding)
            {
                openAreas.Add(roadPos);
            }
        }

        return openAreas;
    }

    bool IsSuitableForArena(Vector3 position, List<Vector3> roadPositions, List<Vector3> buildingPositions)
    {
        float areaSize = 0f;

        foreach (var roadPos in roadPositions)
        {
            if (Vector3.Distance(position, roadPos) < 10f) // Проверка на минимальную площадь
            {
                areaSize += 10f; // Площадь одной дороги
            }
        }

        bool hasNearbyBuilding = false;
        foreach (var buildingPos in buildingPositions)
        {
            if (Vector3.Distance(position, buildingPos) < 20f) // Уменьшение расстояния до зданий
            {
                hasNearbyBuilding = true;
                break;
            }
        }

        return areaSize >= minOpenAreaSize && !hasNearbyBuilding;
    }

    bool IsOverlappingWithExistingArenas(Rect newArenaBounds)
    {
        foreach (Rect bounds in arenaBounds)
        {
            if (bounds.Overlaps(newArenaBounds))
            {
                return true;
            }
        }
        return false;
    }

    GameObject CreateArena(Vector3 position, Transform parent)
    {
        GameObject arena = new GameObject("Arena");
        arena.transform.position = position;
        arena.transform.parent = parent;

        // Ориентация арены по направлению дороги
        Vector3 direction = Vector3.forward; // Ориентируем по оси Z
        arena.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // Выбираем случайный паттерн для размещения укрытий, врагов и интерактивных элементов
        int coverCount = Random.Range(minCoverCount, maxCoverCount);
        int enemyCount = Random.Range(minEnemyCount, maxEnemyCount);
        int interactiveCount = Random.Range(minInteractiveCount, maxInteractiveCount);

        PlaceCoversInPatterns(arena.transform, coverCount);
        PlaceEnemiesInPatterns(arena.transform, enemyCount);
        PlaceInteractiveElementsInPatterns(arena.transform, interactiveCount);

        Debug.Log($"Arena created at position: {position}");
        return arena;
    }

    void PlaceCoversInPatterns(Transform arenaTransform, int coverCount)
    {
        // Применение нескольких паттернов для размещения укрытий
        int patternsToApply = Random.Range(1, 4);
        int remainingCoverCount = coverCount;

        for (int i = 0; i < patternsToApply; i++)
        {
            int countForPattern = remainingCoverCount / (patternsToApply - i);
            remainingCoverCount -= countForPattern;

            int pattern = Random.Range(0, 3);

            switch (pattern)
            {
                case 0:
                    PlaceCoversInLine(arenaTransform, countForPattern);
                    break;
                case 1:
                    PlaceCoversInGrid(arenaTransform, countForPattern);
                    break;
                case 2:
                    PlaceCoversInClusters(arenaTransform, countForPattern);
                    break;
            }
        }
    }

    void PlaceEnemiesInPatterns(Transform arenaTransform, int enemyCount)
    {
        // Применение нескольких паттернов для размещения врагов
        int patternsToApply = Random.Range(1, 3);
        int remainingEnemyCount = enemyCount;

        for (int i = 0; i < patternsToApply; i++)
        {
            int countForPattern = remainingEnemyCount / (patternsToApply - i);
            remainingEnemyCount -= countForPattern;

            int pattern = Random.Range(0, 2);

            switch (pattern)
            {
                case 0:
                    PlaceEnemiesInLine(arenaTransform, countForPattern);
                    break;
                case 1:
                    PlaceEnemiesInClusters(arenaTransform, countForPattern);
                    break;
            }
        }
    }

    void PlaceInteractiveElementsInPatterns(Transform arenaTransform, int interactiveCount)
    {
        // Применение нескольких паттернов для размещения интерактивных элементов
        int patternsToApply = Random.Range(1, 2);
        int remainingInteractiveCount = interactiveCount;

        for (int i = 0; i < patternsToApply; i++)
        {
            int countForPattern = remainingInteractiveCount / (patternsToApply - i);
            remainingInteractiveCount -= countForPattern;

            int pattern = Random.Range(0, 2);

            switch (pattern)
            {
                case 0:
                    PlaceInteractiveElementsInLine(arenaTransform, countForPattern);
                    break;
                case 1:
                    PlaceInteractiveElementsInClusters(arenaTransform, countForPattern);
                    break;
            }
        }
    }

    void PlaceCoversInLine(Transform arenaTransform, int coverCount)
    {
        float spacing = arenaLength / (coverCount + 1);
        for (int i = 0; i < coverCount; i++)
        {
            Vector3 coverPos = arenaTransform.position + new Vector3(Random.Range(-arenaWidth / 2, arenaWidth / 2), 0, -arenaLength / 2 + spacing * (i + 1));
            Quaternion coverRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            Instantiate(coverPrefab, coverPos, coverRotation, arenaTransform);
        }
    }

    void PlaceCoversInGrid(Transform arenaTransform, int coverCount)
    {
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(coverCount));
        float spacingX = arenaWidth / (gridSize + 1);
        float spacingZ = arenaLength / (gridSize + 1);

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (coverCount <= 0) return;

                Vector3 coverPos = arenaTransform.position + new Vector3(-arenaWidth / 2 + spacingX * (x + 1), 0, -arenaLength / 2 + spacingZ * (z + 1));
                Quaternion coverRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                Instantiate(coverPrefab, coverPos, coverRotation, arenaTransform);
                coverCount--;
            }
        }
    }

    void PlaceCoversInClusters(Transform arenaTransform, int coverCount)
    {
        int clusterCount = Mathf.CeilToInt(coverCount / 3f);
        float clusterRadius = Mathf.Min(arenaWidth, arenaLength) / (2 * clusterCount);

        for (int i = 0; i < clusterCount; i++)
        {
            Vector3 clusterCenter = arenaTransform.position + new Vector3(Random.Range(-arenaWidth / 2, arenaWidth / 2), 0, Random.Range(-arenaLength / 2, arenaLength / 2));
            int coversInThisCluster = Random.Range(2, 4);

            for (int j = 0; j < coversInThisCluster; j++)
            {
                if (coverCount <= 0) return;

                Vector3 coverPos = clusterCenter + new Vector3(Random.Range(-clusterRadius, clusterRadius), 0, Random.Range(-clusterRadius, clusterRadius));
                Quaternion coverRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                Instantiate(coverPrefab, coverPos, coverRotation, arenaTransform);
                coverCount--;
            }
        }
    }

    void PlaceEnemiesInLine(Transform arenaTransform, int enemyCount)
    {
        float spacing = arenaLength / (enemyCount + 1);
        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 enemyPos = arenaTransform.position + new Vector3(Random.Range(-arenaWidth / 2, arenaWidth / 2), 1f, -arenaLength / 2 + spacing * (i + 1)); // Поднятие над землей
            Quaternion enemyRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            Instantiate(enemyPrefab, enemyPos, enemyRotation, arenaTransform);
        }
    }

    void PlaceEnemiesInClusters(Transform arenaTransform, int enemyCount)
    {
        int clusterCount = Mathf.CeilToInt(enemyCount / 2f);
        float clusterRadius = Mathf.Min(arenaWidth, arenaLength) / (2 * clusterCount);

        for (int i = 0; i < clusterCount; i++)
        {
            Vector3 clusterCenter = arenaTransform.position + new Vector3(Random.Range(-arenaWidth / 2, arenaWidth / 2), 0, Random.Range(-arenaLength / 2, arenaLength / 2));
            int enemiesInThisCluster = Random.Range(1, 3);

            for (int j = 0; j < enemiesInThisCluster; j++)
            {
                if (enemyCount <= 0) return;

                Vector3 enemyPos = clusterCenter + new Vector3(Random.Range(-clusterRadius, clusterRadius), 1f, Random.Range(-clusterRadius, clusterRadius)); // Поднятие над землей
                Quaternion enemyRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                Instantiate(enemyPrefab, enemyPos, enemyRotation, arenaTransform);
                enemyCount--;
            }
        }
    }

    void PlaceInteractiveElementsInLine(Transform arenaTransform, int interactiveCount)
    {
        float spacing = arenaLength / (interactiveCount + 1);
        for (int i = 0; i < interactiveCount; i++)
        {
            Vector3 interactivePos = arenaTransform.position + new Vector3(Random.Range(-arenaWidth / 2, arenaWidth / 2), 0.5f, -arenaLength / 2 + spacing * (i + 1)); // Поднятие над землей
            Quaternion interactiveRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            Instantiate(interactivePrefab, interactivePos, interactiveRotation, arenaTransform);
        }
    }

    void PlaceInteractiveElementsInClusters(Transform arenaTransform, int interactiveCount)
    {
        int clusterCount = Mathf.CeilToInt(interactiveCount / 2f);
        float clusterRadius = Mathf.Min(arenaWidth, arenaLength) / (2 * clusterCount);

        for (int i = 0; i < clusterCount; i++)
        {
            Vector3 clusterCenter = arenaTransform.position + new Vector3(Random.Range(-arenaWidth / 2, arenaWidth / 2), 0, Random.Range(-arenaLength / 2, arenaLength / 2));
            int interactiveInThisCluster = Random.Range(1, 3);

            for (int j = 0; j < interactiveInThisCluster; j++)
            {
                if (interactiveCount <= 0) return;

                Vector3 interactivePos = clusterCenter + new Vector3(Random.Range(-clusterRadius, clusterRadius), 0.5f, Random.Range(-clusterRadius, clusterRadius)); // Поднятие над землей
                Quaternion interactiveRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                Instantiate(interactivePrefab, interactivePos, interactiveRotation, arenaTransform);
                interactiveCount--;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (arenas == null) return;

        Gizmos.color = Color.yellow;
        foreach (var arena in arenas)
        {
            if (arena != null)
            {
                // Рисуем прямоугольник вокруг арены
                Vector3 center = arena.transform.position;
                Vector3 size = new Vector3(arenaWidth, 1, arenaLength);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}
