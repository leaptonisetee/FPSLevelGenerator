using UnityEngine;
using System;

public static class CoordinateNormalizer
{
    public static Vector3 NormalizeCoordinates(float lon, float lat, Vector3 mapCenter)
    {
        // Проекция Меркатора
        float mercX = lon * 20037508.34f / 180f;
        float mercY = (float)(Math.Log(Math.Tan((90f + lat) * Math.PI / 360f)) / (Math.PI / 180f));
        mercY = mercY * 20037508.34f / 180f;

        // Проекция Меркатора для центра bounding box
        float centerX = (mapCenter.x * 20037508.34f / 180f);
        float centerY = (float)(Math.Log(Math.Tan((90f + mapCenter.z) * Math.PI / 360f)) / (Math.PI / 180f)) * 20037508.34f / 180f;

        float normalizedX = (mercX - centerX) / 1000f * 5.0f; // Используем scaleFactor = 5.0f
        float normalizedZ = (mercY - centerY) / 1000f * 5.0f;

        return new Vector3(normalizedX, 0, normalizedZ) + mapCenter;
    }
}