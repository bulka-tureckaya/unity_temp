using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class RoomFiller : MonoBehaviour
{
    public Button fillRoomsButton;
    public GameObject roomFillerPrefab; // Префаб для заполнения комнаты (должен быть настроен как куб или плита 1x1)
    public float unitSize = 50.0f;       // Размер одной единицы сетки (ширина и длина клетки)
    public float wallHeight = 300.0f;    // Высота стены (всей клетки по высоте)
    public float floorHeight = 10.0f;    // Высота пола

    private int[,] gridData;           // Двумерный массив для хранения данных из файла

    void Start()
    {
        fillRoomsButton.onClick.AddListener(FillRooms);
    }

    void FillRooms()
    {
        string path = Application.dataPath + "/grid_data.txt";
        if (!File.Exists(path))
        {
            Debug.LogError("File grid_data.txt not found.");
            return;
        }

        // Чтение данных из файла
        string[] lines = File.ReadAllLines(path);
        int width = lines[0].Split(' ').Length;
        int height = lines.Length;

        gridData = new int[width, height];

        // Заполнение двумерного массива данными из файла
        for (int y = 0; y < height; y++)
        {
            string[] values = lines[y].Split(' ');
            for (int x = 0; x < width; x++)
            {
                if (int.TryParse(values[x], out int value))
                {
                    gridData[x, y] = value;
                }
                else
                {
                    gridData[x, y] = 0; // Если значение некорректно, считаем его как 0
                }
            }
        }

        // Проходим по каждой клетке и, если значение равно 4 (пол), создаём отдельный блок 1x1
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (gridData[x, y] == 4)
                {
                    // Расчёт позиции: позиция центра клетки по X и Z, Y - середина по высоте блока
                    Vector3 cellPosition = new Vector3(
                        x * unitSize,
                        floorHeight + (wallHeight - floorHeight) / 2,
                        y * unitSize
                    );


                    // Создаем блок и задаем масштаб:
                    // По X и Z – размер клетки unitSize, по Y – высота блока (wallHeight - floorHeight)
                    GameObject roomBlock = Instantiate(roomFillerPrefab, cellPosition, Quaternion.identity);
                    roomBlock.transform.localScale = new Vector3(unitSize, wallHeight - floorHeight, unitSize);
                }
            }
        }
    }
}
