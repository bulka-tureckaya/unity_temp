using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class BuildingGenerator : MonoBehaviour
{
    public Button generateButton;
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public GameObject windowPrefab;
    public GameObject floorPrefab;
    public float unitSize = 50.0f; // ������ ����� ������� ����� (������ 50 ������)
    public float wallHeight = 300.0f; // ������ �����
    public float floorHeight = 10.0f; // ������ ����
    public float offsetAbove = 10.0f; // �������� ����, ������ � ���� �� 10 ������ ����

    void Start()
    {
        generateButton.onClick.AddListener(GenerateBuilding);
    }

    void GenerateBuilding()
    {
        string path = Application.dataPath + "/grid_data.txt";
        if (!File.Exists(path))
        {
            Debug.LogError("File grid_data.txt not found.");
            return;
        }

        string[] lines = File.ReadAllLines(path);
        int width = 0;
        int height = lines.Length;

        for (int y = 0; y < height; y++)
        {
            string[] values = lines[y].Split(' ');
            if (y == 0)
            {
                width = values.Length;
            }

            for (int x = 0; x < width; x++)
            {
                // ���������, ��� �������� �� ������ � �������� ������
                if (string.IsNullOrWhiteSpace(values[x]) || !int.TryParse(values[x], out int colorValue))
                {
                    Debug.LogWarning($"Invalid value at ({x}, {y}): {values[x]}");
                    continue; // ���������� ������������ ��������
                }

                Vector3 position = new Vector3(x * unitSize, 0, y * unitSize);

                switch (colorValue)
                {
                    case 1:
                        // 1 ������ �����+���
                        BuildWall(position);
                        BuildFloor(position);
                        break;
                    case 2:
                        // 2 ������ �����+���+�����
                        BuildWall(position);
                        BuildDoor(position);
                        BuildFloor(position);
                        break;
                    case 3:
                        // 3 ������ �����+���+����
                        BuildWall(position);
                        BuildWindow(position);
                        BuildFloor(position);
                        break;
                    case 4:
                        // 4 ������ ���
                        BuildFloor(position);
                        break;
                    case 0:
                        // 0 ������ ������
                        break;
                    default:
                        Debug.LogWarning($"Invalid value at ({x}, {y}): {values[x]}");
                        break;
                }
            }
        }
    }

    void BuildWall(Vector3 position)
    {
        Vector3 wallPosition = new Vector3(position.x, wallHeight / 2 + offsetAbove, position.z); // ����� ����� �� ������ wallHeight / 2 + offsetAbove
        GameObject wall = Instantiate(wallPrefab, wallPosition, Quaternion.identity);
        wall.transform.localScale = new Vector3(unitSize, wallHeight, unitSize);
    }

    void BuildDoor(Vector3 position)
    {
        Vector3 doorPosition = new Vector3(position.x, wallHeight / 2 + offsetAbove, position.z); // ����� ����� �� ������ wallHeight / 2 + offsetAbove
        GameObject door = Instantiate(doorPrefab, doorPosition, Quaternion.identity);
        door.transform.localScale = new Vector3(unitSize * 1.2f, wallHeight * 0.8f, unitSize * 1.2f); // ����� �������� �� ���� X � Z
    }

    void BuildWindow(Vector3 position)
    {
        Vector3 windowPosition = new Vector3(position.x, wallHeight / 2 + offsetAbove, position.z); // ����� ���� �� ������ wallHeight / 2 + offsetAbove
        GameObject window = Instantiate(windowPrefab, windowPosition, Quaternion.identity);
        window.transform.localScale = new Vector3(unitSize * 1.2f, wallHeight * 0.6f, unitSize * 1.2f); // ���� �������� �� ���� X � Z
    }

    void BuildFloor(Vector3 position)
    {
        Vector3 floorPosition = new Vector3(position.x, floorHeight / 2, position.z); // ����� ���� �� ������ floorHeight / 2
        GameObject floor = Instantiate(floorPrefab, floorPosition, Quaternion.identity);
        floor.transform.localScale = new Vector3(unitSize, floorHeight, unitSize);
    }
}