using System.Collections.Generic;
using UnityEngine;

public class NarrowPositionGenerator : MonoBehaviour
{
    public GameObject coverPrefab;
    public GameObject enemyPrefab;
    public GameObject interactivePrefab;
    public int minCoverCount = 3;
    public int maxCoverCount = 7;
    public int minEnemyCount = 2;
    public int maxEnemyCount = 5;
    public int minInteractiveCount = 1;
    public int maxInteractiveCount = 3;
    public float minOpenAreaSize = 15f; // Минимальная площадь открытого пространства для узкой позиции
    public float narrowWidth = 10f; // Ширина узкой позиции
    public float narrowLength = 50f; // Длина узкой позиции

    private List<GameObject> narrowPositions = new List<GameObject>();
    public List<Rect> arenaBounds; // Добавляем список границ арен

    public void GenerateNarrowPositions(GameObject geometry)
    {
        if (geometry == null)
        {
            Debug.LogError("Geometry object is null");
            return;
        }

        narrowPositions.Clear();

        List<Vector3> roadPositions = new List<Vector3>();
        List<Vector3> roadDirections = new List<Vector3>(); // Добавляем список направлений дорог
        List<Vector3> buildingPositions = new List<Vector3>();

        // Находим все объекты с тегом "Road" и "Building"
        foreach (Transform child in geometry.transform)
        {
            if (child.CompareTag("Road"))
            {
                roadPositions.Add(child.position);
                roadDirections.Add(child.forward); // Сохраняем направление дороги
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

        // Создаем объект-контейнер для узких позиций вне иерархии сгенерированного уровня
        GameObject narrowContainer = new GameObject("NarrowPositionContainer");

        for (int i = 0; i < openAreas.Count; i++)
        {
            var area = openAreas[i];
            if (IsSuitableForNarrowPosition(area, roadPositions, buildingPositions))
            {
                Rect newNarrowBounds = new Rect(area.x - narrowWidth / 2, area.z - narrowLength / 2, narrowWidth, narrowLength);
                // Находим ближайшую дорогу и используем ее направление для ориентации узкой позиции
                Vector3 closestRoadDirection = roadDirections[FindClosestRoadIndex(area, roadPositions)];
                GameObject narrowPosition = CreateNarrowPosition(area, closestRoadDirection, narrowContainer.transform);

                if (!IsOverlappingWithExistingNarrowPositions(narrowPosition) && !IsOverlappingWithArenaBounds(newNarrowBounds))
                {
                    narrowPositions.Add(narrowPosition);
                }
                else
                {
                    Destroy(narrowPosition);
                }
            }
        }
    }

    int FindClosestRoadIndex(Vector3 position, List<Vector3> roadPositions)
    {
        float closestDistance = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < roadPositions.Count; i++)
        {
            float distance = Vector3.Distance(position, roadPositions[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
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

    bool IsSuitableForNarrowPosition(Vector3 position, List<Vector3> roadPositions, List<Vector3> buildingPositions)
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

    bool IsOverlappingWithExistingNarrowPositions(GameObject newNarrowPosition)
    {
        Rect newBounds = new Rect(newNarrowPosition.transform.position.x - narrowWidth / 2, newNarrowPosition.transform.position.z - narrowLength / 2, narrowWidth, narrowLength);
        newBounds = RotateRect(newBounds, newNarrowPosition.transform.rotation.eulerAngles.y);

        foreach (var existingNarrowPosition in narrowPositions)
        {
            if (existingNarrowPosition != null)
            {
                Rect existingBounds = new Rect(existingNarrowPosition.transform.position.x - narrowWidth / 2, existingNarrowPosition.transform.position.z - narrowLength / 2, narrowWidth, narrowLength);
                existingBounds = RotateRect(existingBounds, existingNarrowPosition.transform.rotation.eulerAngles.y);

                if (existingBounds.Overlaps(newBounds))
                {
                    return true;
                }
            }
        }
        return false;
    }

    bool IsOverlappingWithArenaBounds(Rect newNarrowBounds)
    {
        foreach (Rect bounds in arenaBounds)
        {
            if (bounds.Overlaps(newNarrowBounds))
            {
                return true;
            }
        }
        return false;
    }

    Rect RotateRect(Rect rect, float angle)
    {
        Vector2 center = rect.center;
        Vector2 size = rect.size;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        Vector2[] corners = new Vector2[4];
        corners[0] = center + (Vector2)(rotation * new Vector3(-size.x / 2, -size.y / 2));
        corners[1] = center + (Vector2)(rotation * new Vector3(size.x / 2, -size.y / 2));
        corners[2] = center + (Vector2)(rotation * new Vector3(size.x / 2, size.y / 2));
        corners[3] = center + (Vector2)(rotation * new Vector3(-size.x / 2, size.y / 2));

        Vector2 min = corners[0];
        Vector2 max = corners[0];
        foreach (var corner in corners)
        {
            min = Vector2.Min(min, corner);
            max = Vector2.Max(max, corner);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    GameObject CreateNarrowPosition(Vector3 position, Vector3 direction, Transform parent)
    {
        GameObject narrowPosition = new GameObject("NarrowPosition");
        narrowPosition.transform.position = position;
        narrowPosition.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        narrowPosition.transform.SetParent(parent, true);

        // Выбираем случайный паттерн для размещения укрытий, врагов и интерактивных элементов
        int coverCount = Random.Range(minCoverCount, maxCoverCount);
        int enemyCount = Random.Range(minEnemyCount, maxEnemyCount);
        int interactiveCount = Random.Range(minInteractiveCount, maxInteractiveCount);

        PlaceCoversInPatterns(narrowPosition.transform, coverCount);
        PlaceEnemiesInPatterns(narrowPosition.transform, enemyCount);
        PlaceInteractiveElementsInPatterns(narrowPosition.transform, interactiveCount);

        Debug.Log($"Narrow position created at position: {position}");
        return narrowPosition;
    }

    void PlaceCoversInPatterns(Transform narrowTransform, int coverCount)
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
                    PlaceCoversInLine(narrowTransform, countForPattern);
                    break;
                case 1:
                    PlaceCoversInGrid(narrowTransform, countForPattern);
                    break;
                case 2:
                    PlaceCoversInClusters(narrowTransform, countForPattern);
                    break;
            }
        }
    }

    void PlaceEnemiesInPatterns(Transform narrowTransform, int enemyCount)
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
                    PlaceEnemiesInLine(narrowTransform, countForPattern);
                    break;
                case 1:
                    PlaceEnemiesInClusters(narrowTransform, countForPattern);
                    break;
            }
        }
    }

    void PlaceInteractiveElementsInPatterns(Transform narrowTransform, int interactiveCount)
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
                    PlaceInteractiveElementsInLine(narrowTransform, countForPattern);
                    break;
                case 1:
                    PlaceInteractiveElementsInClusters(narrowTransform, countForPattern);
                    break;
            }
        }
    }

    void PlaceCoversInLine(Transform narrowTransform, int coverCount)
    {
        float spacing = narrowLength / (coverCount + 1);
        for (int i = 0; i < coverCount; i++)
        {
            Vector3 localPos = new Vector3(Random.Range(-narrowWidth / 2, narrowWidth / 2), 0, -narrowLength / 2 + spacing * (i + 1));
            Vector3 worldPos = narrowTransform.TransformPoint(localPos);
            Quaternion coverRotation = narrowTransform.rotation;
            Instantiate(coverPrefab, worldPos, coverRotation, narrowTransform);
        }
    }

    void PlaceCoversInGrid(Transform narrowTransform, int coverCount)
    {
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(coverCount));
        float spacingX = narrowWidth / (gridSize + 1);
        float spacingZ = narrowLength / (gridSize + 1);

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (coverCount <= 0) return;

                Vector3 localPos = new Vector3(-narrowWidth / 2 + spacingX * (x + 1), 0, -narrowLength / 2 + spacingZ * (z + 1));
                Vector3 worldPos = narrowTransform.TransformPoint(localPos);
                Quaternion coverRotation = narrowTransform.rotation;
                Instantiate(coverPrefab, worldPos, coverRotation, narrowTransform);
                coverCount--;
            }
        }
    }

    void PlaceCoversInClusters(Transform narrowTransform, int coverCount)
    {
        int clusterCount = Mathf.CeilToInt(coverCount / 3f);
        float clusterRadius = Mathf.Min(narrowWidth, narrowLength) / (2 * clusterCount);

        for (int i = 0; i < clusterCount; i++)
        {
            Vector3 clusterCenter = narrowTransform.position + narrowTransform.rotation * new Vector3(Random.Range(-narrowWidth / 2, narrowWidth / 2), 0, Random.Range(-narrowLength / 2, narrowLength / 2));
            int coversInThisCluster = Random.Range(2, 4);

            for (int j = 0; j < coversInThisCluster; j++)
            {
                if (coverCount <= 0) return;

                Vector3 localPos = new Vector3(Random.Range(-clusterRadius, clusterRadius), 0, Random.Range(-clusterRadius, clusterRadius));
                Vector3 worldPos = narrowTransform.TransformPoint(clusterCenter + localPos);
                Quaternion coverRotation = narrowTransform.rotation;
                Instantiate(coverPrefab, worldPos, coverRotation, narrowTransform);
                coverCount--;
            }
        }
    }

    void PlaceEnemiesInLine(Transform narrowTransform, int enemyCount)
    {
        float spacing = narrowLength / (enemyCount + 1);
        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 localPos = new Vector3(Random.Range(-narrowWidth / 2, narrowWidth / 2), 1f, -narrowLength / 2 + spacing * (i + 1));
            Vector3 worldPos = narrowTransform.TransformPoint(localPos);
            Quaternion enemyRotation = narrowTransform.rotation;
            Instantiate(enemyPrefab, worldPos, enemyRotation, narrowTransform);
        }
    }

    void PlaceEnemiesInClusters(Transform narrowTransform, int enemyCount)
    {
        int clusterCount = Mathf.CeilToInt(enemyCount / 2f);
        float clusterRadius = Mathf.Min(narrowWidth, narrowLength) / (2 * clusterCount);

        for (int i = 0; i < clusterCount; i++)
        {
            Vector3 clusterCenter = narrowTransform.position + narrowTransform.rotation * new Vector3(Random.Range(-narrowWidth / 2, narrowWidth / 2), 1f, Random.Range(-narrowLength / 2, narrowLength / 2));
            int enemiesInThisCluster = Random.Range(1, 3);

            for (int j = 0; j < enemiesInThisCluster; j++)
            {
                if (enemyCount <= 0) return;

                Vector3 localPos = new Vector3(Random.Range(-clusterRadius, clusterRadius), 1f, Random.Range(-clusterRadius, clusterRadius));
                Vector3 worldPos = narrowTransform.TransformPoint(clusterCenter + localPos);
                Quaternion enemyRotation = narrowTransform.rotation;
                Instantiate(enemyPrefab, worldPos, enemyRotation, narrowTransform);
                enemyCount--;
            }
        }
    }

    void PlaceInteractiveElementsInLine(Transform narrowTransform, int interactiveCount)
    {
        float spacing = narrowLength / (interactiveCount + 1);
        for (int i = 0; i < interactiveCount; i++)
        {
            Vector3 localPos = new Vector3(Random.Range(-narrowWidth / 2, narrowWidth / 2), 0.5f, -narrowLength / 2 + spacing * (i + 1));
            Vector3 worldPos = narrowTransform.TransformPoint(localPos);
            Quaternion interactiveRotation = narrowTransform.rotation;
            Instantiate(interactivePrefab, worldPos, interactiveRotation, narrowTransform);
        }
    }

    void PlaceInteractiveElementsInClusters(Transform narrowTransform, int interactiveCount)
    {
        int clusterCount = Mathf.CeilToInt(interactiveCount / 2f);
        float clusterRadius = Mathf.Min(narrowWidth, narrowLength) / (2 * clusterCount);

        for (int i = 0; i < clusterCount; i++)
        {
            Vector3 clusterCenter = narrowTransform.position + narrowTransform.rotation * new Vector3(Random.Range(-narrowWidth / 2, narrowWidth / 2), 0.5f, Random.Range(-narrowLength / 2, narrowLength / 2));
            int interactiveInThisCluster = Random.Range(1, 3);

            for (int j = 0; j < interactiveInThisCluster; j++)
            {
                if (interactiveCount <= 0) return;

                Vector3 localPos = new Vector3(Random.Range(-clusterRadius, clusterRadius), 0.5f, Random.Range(-clusterRadius, clusterRadius));
                Vector3 worldPos = narrowTransform.TransformPoint(clusterCenter + localPos);
                Quaternion interactiveRotation = narrowTransform.rotation;
                Instantiate(interactivePrefab, worldPos, interactiveRotation, narrowTransform);
                interactiveCount--;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (narrowPositions == null) return;

        Gizmos.color = Color.blue; // Изменяем цвет на синий
        foreach (var narrowPosition in narrowPositions)
        {
            if (narrowPosition != null)
            {
                // Рисуем прямоугольник вокруг узкой позиции
                Vector3 center = narrowPosition.transform.position;
                Vector3 size = new Vector3(narrowWidth, 1, narrowLength);
                Gizmos.matrix = Matrix4x4.TRS(center, narrowPosition.transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);
            }
        }
    }
}
