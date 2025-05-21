using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SchemeLoader : MonoBehaviour
{
    public Image schemeImage; // ������ Image ��� ����������� �����
    public int gridSize = 64; // ������ ����� (��������, 64x64)
    private Color[,] gridColors; // ������ ������ ��� ������ ������
    private RectTransform gridRectTransform;
    private Image[,] gridCells; // ������ �������� Image ��� ������

    void Start()
    {
        LoadScheme();
        if (schemeImage.sprite != null) // ���������, ��� ������ ��������
        {
            CreateGrid();
        }
        else
        {
            Debug.LogError("����� �� ���������. ��������� ����� Resources � ���������.");
        }
    }

    // ��������� ���������� ����������� ����� �� ������������ ����
    void LoadScheme()
    {
        string path = "Assets/Resources/grid_image.png"; // ����������� ���� � �����
        if (!File.Exists(path))
        {
            Debug.LogError($"���� �� ������ �� ����: {path}");
            return;
        }

        // ������ ����� �����������
        byte[] bytes = File.ReadAllBytes(path);

        // ������ ��������
        Texture2D texture = new Texture2D(2, 2); // ������ ��������� ��������
        if (!texture.LoadImage(bytes))
        {
            Debug.LogError("�� ������� ��������� �����������!");
            return;
        }

        // ������������� ���������� ��� ��������
        texture.filterMode = FilterMode.Point; // �������� ����������
        texture.wrapMode = TextureWrapMode.Clamp;

        // ������ ������ �� ��������
        Sprite schemeSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        schemeImage.sprite = schemeSprite;

        Debug.Log("����� ������� ���������.");
    }

    // ������ ����� ������ ������ ����������� �����
    void CreateGrid()
    {
        gridRectTransform = schemeImage.GetComponent<RectTransform>();
        gridCells = new Image[gridSize, gridSize];
        gridColors = new Color[gridSize, gridSize];

        float cellSize = gridRectTransform.rect.width / gridSize;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                // ������ ������ ��� ������
                GameObject cell = new GameObject($"Cell_{x}_{y}");
                cell.transform.SetParent(schemeImage.transform);

                // ��������� ��������� Image
                Image cellImage = cell.AddComponent<Image>();
                cellImage.color = Color.clear; // ���������� ���� �� ���������

                // ��������� ������� � ������� ������
                RectTransform cellRectTransform = cell.GetComponent<RectTransform>();
                cellRectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                cellRectTransform.anchoredPosition = new Vector2(
                    x * cellSize - gridRectTransform.rect.width / 2 + cellSize / 2,
                    -y * cellSize + gridRectTransform.rect.height / 2 - cellSize / 2
                );

                // ��������� ������ �� ������
                gridCells[x, y] = cellImage;

                // ���������� ���� ������ �� ������ �����������
                gridColors[x, y] = GetPixelColor(x, y);
            }
        }
    }

    // �������� ���� ������� �� ������������ �����������
    Color GetPixelColor(int x, int y)
    {
        if (schemeImage.sprite == null || schemeImage.sprite.texture == null)
        {
            Debug.LogError("�������� �� ��������� ��� ���������� ��� ������!");
            return Color.clear;
        }

        Texture2D texture = schemeImage.sprite.texture;
        int pixelX = Mathf.FloorToInt((float)x / gridSize * texture.width);
        int pixelY = Mathf.FloorToInt((float)y / gridSize * texture.height);
        return texture.GetPixel(pixelX, texture.height - 1 - pixelY); // ��������� �� ���������
    }

    // ������������ ����� �� �������
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRectTransform, Input.mousePosition, null, out localPoint))
            {
                int x = Mathf.Clamp(Mathf.FloorToInt((localPoint.x + gridRectTransform.rect.width / 2) / (gridRectTransform.rect.width / gridSize)), 0, gridSize - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt((-localPoint.y + gridRectTransform.rect.height / 2) / (gridRectTransform.rect.height / gridSize)), 0, gridSize - 1);

                OnCellClicked(x, y);
            }
        }
    }

    // ���������� ����� �� ������
    void OnCellClicked(int x, int y)
    {
        Debug.Log($"������ [{x}, {y}] ���� �������. ����: {gridColors[x, y]}");

        // ����� ����� �������� ������ ��� ���������� �������� �� ���������
        // ��������, ���������, ��� ������ �������� "��������" (�� ������)
        if (IsRoomCell(gridColors[x, y]))
        {
            Debug.Log("��� �������. ����� ���������� ������.");
        }
        else
        {
            Debug.Log("��� ����� ��� ������ ����������� �������.");
        }
    }

    // ���������, �������� �� ������ ������ ������� (�� �����)
    bool IsRoomCell(Color cellColor)
    {
        // ���������, ��� ���� ������ � ������ (RGB �������� �������� �����)
        float threshold = 0.1f; // ����� ��� ���������
        return Mathf.Abs(cellColor.r - 0.5f) < threshold &&
               Mathf.Abs(cellColor.g - 0.5f) < threshold &&
               Mathf.Abs(cellColor.b - 0.5f) < threshold;
    }
}