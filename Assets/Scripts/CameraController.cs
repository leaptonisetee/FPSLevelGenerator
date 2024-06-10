using UnityEngine;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    public float speed = 10.0f; // Скорость движения
    public float sensitivity = 2.0f; // Чувствительность мыши

    private float rotationY = 0.0f;
    private float rotationX = 0.0f;

    void Update()
    {
        // Управление движением камеры с помощью клавиатуры
        float translationX = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        float translationZ = Input.GetAxis("Vertical") * speed * Time.deltaTime;

        transform.Translate(translationX, 0, translationZ);

        // Управление вращением камеры с помощью мыши
        if (Input.GetMouseButton(1)) // Нажата правая кнопка мыши
        {
            rotationX += Input.GetAxis("Mouse X") * sensitivity;
            rotationY -= Input.GetAxis("Mouse Y") * sensitivity;
            rotationY = Mathf.Clamp(rotationY, -90, 90); // Ограничение угла поворота по оси Y

            transform.localEulerAngles = new Vector3(rotationY, rotationX, 0);
        }
    }

    // Новый метод для позиционирования камеры в начале
    public void SetupCamera(List<Vector3> generatedPositions)
    {
        if (generatedPositions.Count > 0)
        {
            Vector3 center = GetCenterPosition(generatedPositions);
            transform.position = center + new Vector3(0, 100, -100); // Устанавливаем камеру выше и позади центра
            transform.LookAt(center);
        }
    }

    // Метод для нахождения центра всех сгенерированных позиций
    private Vector3 GetCenterPosition(List<Vector3> positions)
    {
        Vector3 sum = Vector3.zero;
        foreach (var pos in positions)
        {
            sum += pos;
        }
        return sum / positions.Count;
    }
}