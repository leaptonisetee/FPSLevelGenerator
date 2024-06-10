using System.Collections.Generic;
using UnityEngine;

public class SniperPositionGenerator : MonoBehaviour
{
    public GameObject coverPrefab;
    public float edgeOffset = 2f; // Отступ от края крыши
    public float minHeight = 10f; // Минимальная высота для снайперских позиций
    public float minDistanceBetweenPositions = 10f; // Минимальное расстояние между снайперскими позициями
    public int maxPositionsPerRoof = 2; // Максимальное количество снайперских позиций на одной крыше
    public int visibilityCheckRays = 10; // Количество лучей для проверки видимости
    public float maxRayDistance = 50f; // Максимальная длина луча для проверки видимости
    public LayerMask visibilityLayerMask; // Слой, который используется для проверки видимости
    public float areaWidth = 10f; // Ширина области для снайперской позиции
    public float areaLength = 15f; // Длина области для снайперской позиции
    public int coversPerArea = 4; // Количество укрытий на одну область

    private List<GameObject> sniperPositions = new List<GameObject>();
    private List<Rect> sniperBounds = new List<Rect>(); // Список границ снайперских позиций для проверки пересечений

    public void GenerateSniperPositions(GameObject geometry)
    {
        if (geometry == null)
        {
            Debug.LogError("Geometry object is null");
            return;
        }

        sniperPositions.Clear();
        sniperBounds.Clear();

        List<Transform> roofTransforms = FindRoofTransforms(geometry);

        // Создаем объект-контейнер для снайперских позиций вне иерархии сгенерированного уровня
        GameObject sniperContainer = new GameObject("SniperPositionContainer");

        foreach (var roofTransform in roofTransforms)
        {
            if (IsSuitableForSniperPosition(roofTransform.position))
            {
                CreateSniperCover(roofTransform, sniperContainer.transform);
            }
        }
    }

    List<Transform> FindRoofTransforms(GameObject geometry)
    {
        List<Transform> roofTransforms = new List<Transform>();

        foreach (Transform building in geometry.transform)
        {
            if (building.CompareTag("Building"))
            {
                foreach (Transform child in building)
                {
                    if (child.CompareTag("BuildingRoof"))
                    {
                        // Проверяем высоту позиции крыши относительно здания
                        float roofHeight = building.position.y + child.localPosition.y;
                        if (roofHeight >= minHeight)
                        {
                            roofTransforms.Add(child);
                        }
                    }
                }
            }
        }

        return roofTransforms;
    }

    bool IsSuitableForSniperPosition(Vector3 position)
    {
        // Проверка на пересечение с существующими снайперскими позициями
        Rect newSniperBounds = new Rect(position.x - areaWidth / 2, position.z - areaLength / 2, areaWidth, areaLength);
        if (IsOverlappingWithExistingSniperPositions(newSniperBounds))
        {
            return false;
        }

        // Проверка минимального расстояния до существующих позиций
        foreach (var sniper in sniperPositions)
        {
            if (Vector3.Distance(position, sniper.transform.position) < minDistanceBetweenPositions)
            {
                return false;
            }
        }

        sniperBounds.Add(newSniperBounds);
        return true;
    }

    bool IsOverlappingWithExistingSniperPositions(Rect newSniperBounds)
    {
        foreach (Rect bounds in sniperBounds)
        {
            if (bounds.Overlaps(newSniperBounds))
            {
                return true;
            }
        }
        return false;
    }

    void CreateSniperCover(Transform roofTransform, Transform parent)
    {
        Vector3 roofPosition = roofTransform.position;

        // Создаем укрытия вокруг высокой точки, чтобы они находились вдоль границ позиции
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(-areaWidth / 2 + edgeOffset, 0, -areaLength / 2 + edgeOffset),
            new Vector3(areaWidth / 2 - edgeOffset, 0, -areaLength / 2 + edgeOffset),
            new Vector3(-areaWidth / 2 + edgeOffset, 0, areaLength / 2 - edgeOffset),
            new Vector3(areaWidth / 2 - edgeOffset, 0, areaLength / 2 - edgeOffset),
        };

        int positionsCreated = 0;

        foreach (var offset in offsets)
        {
            if (positionsCreated >= maxPositionsPerRoof)
            {
                break;
            }

            Vector3 coverPos = roofPosition + offset;
            coverPos.y = roofPosition.y; // Сохраняем высоту крыши

            // Проверка минимального расстояния до существующих позиций
            bool tooClose = false;
            foreach (var sniper in sniperPositions)
            {
                if (Vector3.Distance(coverPos, sniper.transform.position) < minDistanceBetweenPositions)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
            {
                continue;
            }

            if (!HasGoodVisibility(coverPos))
            {
                continue;
            }

            Quaternion coverRotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
            GameObject cover = Instantiate(coverPrefab, coverPos, coverRotation, parent);
            sniperPositions.Add(cover);
            positionsCreated++;

            // Добавляем дополнительные укрытия на область
            for (int i = 1; i < coversPerArea; i++)
            {
                Vector3 additionalCoverPos = coverPos + new Vector3(Random.Range(-areaWidth / 2, areaWidth / 2), 0, Random.Range(-areaLength / 2, areaLength / 2));
                if (!HasGoodVisibility(additionalCoverPos)) continue;

                GameObject additionalCover = Instantiate(coverPrefab, additionalCoverPos, coverRotation, parent);
                sniperPositions.Add(additionalCover);
            }
        }

        Debug.Log($"Sniper cover created around position: {roofPosition}");
    }

    bool HasGoodVisibility(Vector3 position)
    {
        int visibleRays = 0;

        for (int i = 0; i < visibilityCheckRays; i++)
        {
            float angle = i * (360f / visibilityCheckRays);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            if (Physics.Raycast(position, direction, maxRayDistance, visibilityLayerMask))
            {
                visibleRays++;
            }
        }

        return visibleRays >= visibilityCheckRays / 2; // Минимум половина лучей должна быть видимой
    }

    void OnDrawGizmos()
    {
        if (sniperPositions == null) return;

        Gizmos.color = Color.red;
        foreach (var sniper in sniperPositions)
        {
            if (sniper != null)
            {
                // Рисуем прямоугольник вокруг снайперской позиции
                Vector3 center = sniper.transform.position;
                Vector3 size = new Vector3(areaWidth, 1, areaLength);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}
