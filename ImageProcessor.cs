using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ImageProcessor : MonoBehaviour
{
    public Button processButton;

    void Start()
    {
        processButton.onClick.AddListener(ProcessImage);
    }

    void ProcessImage()
    {
        Texture2D texture = Resources.Load<Texture2D>("grid_image");
        if (texture == null)
        {
            Debug.LogError("Image not found in Resources folder.");
            return;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = texture.GetPixels();
        int width = texture.width;
        int height = texture.height;

        string data = "";
        string colorData = "";

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = pixels[y * width + x];
                string colorCode = ColorToHex(pixel);
                colorData += colorCode + " ";

                int colorValue = GetColorValue(pixel);
                data += colorValue + " ";

                // Отладочное сообщение для проверки цвета
                // Debug.Log($"Pixel at ({x}, {y}): {colorCode} -> {colorValue}");
            }
            data = data.TrimEnd(' ') + "\n"; // Убираем лишние пробелы в конце строки
            colorData = colorData.TrimEnd(' ') + "\n"; // Убираем лишние пробелы в конце строки
        }

        // Отладочное сообщение для проверки данных
        Debug.Log("Data to be saved: " + data);
        Debug.Log("Color data to be saved: " + colorData);

        string path = Application.dataPath + "/grid_data.txt";
        File.WriteAllText(path, data);
        Debug.Log("Data saved to " + path);

        string colorPath = Application.dataPath + "/test_grid_data.txt";
        File.WriteAllText(colorPath, colorData);
        Debug.Log("Color data saved to " + colorPath);
    }

    string ColorToHex(Color color)
    {
        return "#" + ((int)(color.r * 255)).ToString("X2") + ((int)(color.g * 255)).ToString("X2") + ((int)(color.b * 255)).ToString("X2");
    }

    int GetColorValue(Color color)
    {
        if (color == Color.white) return 0;
        if (color == Color.black) return 1;
        if (color == Color.red) return 2;
        if (color == Color.green) return 3;
        return 4;
    }
}