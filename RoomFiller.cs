using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class RoomFiller : MonoBehaviour
{
    public Button fillRoomsButton;
    public GameObject roomFillerPrefab; // ������ ��� ���������� ������� (������ ���� �������� ��� ��� ��� ����� 1x1)
    public float unitSize = 50.0f;       // ������ ����� ������� ����� (������ � ����� ������)
    public float wallHeight = 300.0f;    // ������ ����� (���� ������ �� ������)
    public float floorHeight = 10.0f;    // ������ ����

    private int[,] gridData;           // ��������� ������ ��� �������� ������ �� �����

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

        // ������ ������ �� �����
        string[] lines = File.ReadAllLines(path);
        int width = lines[0].Split(' ').Length;
        int height = lines.Length;

        gridData = new int[width, height];

        // ���������� ���������� ������� ������� �� �����
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
                    gridData[x, y] = 0; // ���� �������� �����������, ������� ��� ��� 0
                }
            }
        }

        // �������� �� ������ ������ �, ���� �������� ����� 4 (���), ������ ��������� ���� 1x1
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (gridData[x, y] == 4)
                {
                    // ������ �������: ������� ������ ������ �� X � Z, Y - �������� �� ������ �����
                    Vector3 cellPosition = new Vector3(
                        x * unitSize,
                        floorHeight + (wallHeight - floorHeight) / 2,
                        y * unitSize
                    );


                    // ������� ���� � ������ �������:
                    // �� X � Z � ������ ������ unitSize, �� Y � ������ ����� (wallHeight - floorHeight)
                    GameObject roomBlock = Instantiate(roomFillerPrefab, cellPosition, Quaternion.identity);
                    roomBlock.transform.localScale = new Vector3(unitSize, wallHeight - floorHeight, unitSize);
                }
            }
        }
    }
}
