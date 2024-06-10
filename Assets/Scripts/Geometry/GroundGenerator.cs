using UnityEngine;

public static class GroundCreator
{
    public static void CreateGround(GameObject groundPrefab, Vector3 mapCenter, float roadHeight, Vector2 minCoords, Vector2 maxCoords, float scaleFactor)
    {
        // Вычисляем размеры земли на основе координатной разницы
        float groundWidth = (maxCoords.x - minCoords.x) * 111320f * scaleFactor / 1000f;
        float groundHeight = (maxCoords.y - minCoords.y) * 111320f * scaleFactor / 1000f;

        // Создаем и настраиваем объект земли
        var ground = Object.Instantiate(groundPrefab, mapCenter + new Vector3(0, roadHeight, 0), Quaternion.identity);
        ground.transform.localScale = new Vector3(groundWidth, 1, groundHeight);

        Debug.Log("Ground created with size: " + groundWidth + " x " + groundHeight);
    }
}