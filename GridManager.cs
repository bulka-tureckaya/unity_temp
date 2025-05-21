using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GridManager : MonoBehaviour
{
    public Image gridImage;
    public int gridSize = 64; // Размер сетки (например, 64x64)
    public Color defaultColor = Color.white;
    public Color selectedColor = Color.black;
    public Button saveButton; // Кнопка для сохранения изображения
    public Button blackButton; // Кнопка для черного цвета
    public Button redButton; // Кнопка для красного цвета
    public Button greenButton; // Кнопка для зеленого цвета
    public Button whiteButton; // Кнопка для белого цвета (ластик)
    public Button fillButton; // Кнопка для заливки
    public Button fillWhiteButton; // Кнопка для заливки белым цветом
    public Button fillGrayButton; // Кнопка для заливки серым цветом
    public Button undoButton; // Кнопка для отмены последнего действия

    private RectTransform gridRectTransform;
    private Image[,] gridCells;
    private Image[,] tempGridCells;
    private Vector2Int startPoint;
    private Vector2Int endPoint;
    private bool isDrawing = false;
    private bool isErasing = false;
    private bool isFilling = false;
    private Color fillColor = Color.gray; // Цвет заливки по умолчанию

    private Stack<Color[,]> undoStack = new Stack<Color[,]>();

    void Start()
    {
        gridRectTransform = gridImage.GetComponent<RectTransform>();
        CreateGrid();
        CreateTempGrid();

        // Добавляем обработчики нажатия на кнопки
        saveButton.onClick.AddListener(SaveGridImage);
        blackButton.onClick.AddListener(() => ChangeColor(Color.black));
        redButton.onClick.AddListener(() => ChangeColor(Color.red));
        greenButton.onClick.AddListener(() => ChangeColor(Color.green));
        whiteButton.onClick.AddListener(EnableEraser);
        fillButton.onClick.AddListener(EnableFilling);
        fillWhiteButton.onClick.AddListener(() => SetFillColor(Color.white));
        fillGrayButton.onClick.AddListener(() => SetFillColor(Color.gray));
        undoButton.onClick.AddListener(UndoLastAction);
    }

    void CreateGrid()
    {
        gridCells = new Image[gridSize, gridSize];
        float cellSize = gridRectTransform.rect.width / gridSize;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                GameObject cell = new GameObject("Cell_" + x + "_" + y);
                cell.transform.SetParent(gridImage.transform);

                Image cellImage = cell.AddComponent<Image>();
                cellImage.color = defaultColor;

                RectTransform cellRectTransform = cell.GetComponent<RectTransform>();
                cellRectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                cellRectTransform.anchoredPosition = new Vector2(x * cellSize - gridRectTransform.rect.width / 2 + cellSize / 2,
                                                                    -y * cellSize + gridRectTransform.rect.height / 2 - cellSize / 2);

                gridCells[x, y] = cellImage;
            }
        }
    }

    void CreateTempGrid()
    {
        tempGridCells = new Image[gridSize, gridSize];
        float cellSize = gridRectTransform.rect.width / gridSize;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                GameObject cell = new GameObject("TempCell_" + x + "_" + y);
                cell.transform.SetParent(gridImage.transform);

                Image cellImage = cell.AddComponent<Image>();
                cellImage.color = Color.clear; // Прозрачный цвет для временных клеток

                RectTransform cellRectTransform = cell.GetComponent<RectTransform>();
                cellRectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                cellRectTransform.anchoredPosition = new Vector2(x * cellSize - gridRectTransform.rect.width / 2 + cellSize / 2,
                                                                    -y * cellSize + gridRectTransform.rect.height / 2 - cellSize / 2);

                tempGridCells[x, y] = cellImage;
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRectTransform, Input.mousePosition, null, out localPoint))
            {
                startPoint = new Vector2Int(
                    Mathf.Clamp(Mathf.FloorToInt((localPoint.x + gridRectTransform.rect.width / 2) / (gridRectTransform.rect.width / gridSize)), 0, gridSize - 1),
                    Mathf.Clamp(Mathf.FloorToInt((-localPoint.y + gridRectTransform.rect.height / 2) / (gridRectTransform.rect.height / gridSize)), 0, gridSize - 1)
                );

                if (isErasing)
                {
                    SaveGridState();
                    EraseCell(startPoint);
                }
                else if (isFilling)
                {
                    // Проверяем, что точка нажатия находится внутри изображения
                    if (startPoint.x >= 0 && startPoint.x < gridSize && startPoint.y >= 0 && startPoint.y < gridSize)
                    {
                        SaveGridState();
                        FillArea(startPoint);
                    }
                }
                else
                {
                    isDrawing = true;
                }
            }
        }

        if (Input.GetMouseButton(0) && isDrawing)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRectTransform, Input.mousePosition, null, out localPoint))
            {
                endPoint = new Vector2Int(
                    Mathf.Clamp(Mathf.FloorToInt((localPoint.x + gridRectTransform.rect.width / 2) / (gridRectTransform.rect.width / gridSize)), 0, gridSize - 1),
                    Mathf.Clamp(Mathf.FloorToInt((-localPoint.y + gridRectTransform.rect.height / 2) / (gridRectTransform.rect.height / gridSize)), 0, gridSize - 1)
                );
                DrawLine(startPoint, endPoint);
            }
        }

        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            isDrawing = false;
            SaveGridState();
            FixLine(startPoint, endPoint);
            ClearTempGrid();
        }
    }

    void DrawLine(Vector2Int start, Vector2Int end)
    {
        // Очистить предыдущую линию
        ClearTempGrid();

        // Нарисовать новую линию
        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < gridSize && y0 >= 0 && y0 < gridSize)
            {
                tempGridCells[x0, y0].color = selectedColor;
            }
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    void FixLine(Vector2Int start, Vector2Int end)
    {
        // Зафиксировать линию
        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < gridSize && y0 >= 0 && y0 < gridSize)
            {
                gridCells[x0, y0].color = selectedColor;
            }
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }

        // Делаем края изображения всегда белыми
        for (int x = 0; x < gridSize; x++)
        {
            gridCells[x, 0].color = Color.white;
            gridCells[x, gridSize - 1].color = Color.white;
        }
        for (int y = 0; y < gridSize; y++)
        {
            gridCells[0, y].color = Color.white;
            gridCells[gridSize - 1, y].color = Color.white;
        }
    }

    void ClearTempGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                tempGridCells[x, y].color = Color.clear;
            }
        }
    }

    void SaveGridImage()
    {
        // Создаем текстуру
        Texture2D texture = new Texture2D(gridSize, gridSize);

        // Заполняем текстуру цветами клеток
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                texture.SetPixel(x, y, gridCells[x, y].color);
            }
        }

        // Переворачиваем текстуру по вертикали
        Texture2D flippedTexture = new Texture2D(gridSize, gridSize);
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                flippedTexture.SetPixel(x, gridSize - 1 - y, texture.GetPixel(x, y));
            }
        }
        flippedTexture.Apply();

        // Применяем изменения и сохраняем текстуру в файл
        byte[] bytes = flippedTexture.EncodeToPNG();

        // Сохраняем текстуру в папку Assets/Resources
        string path = "Assets/Resources/grid_image.png";
        File.WriteAllBytes(path, bytes);

        // Обновляем базу данных активов Unity
#if UNITY_EDITOR
    AssetDatabase.Refresh();

    // Настраиваем текстуру
    TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
    if (textureImporter != null)
    {
        textureImporter.textureType = TextureImporterType.Default;
        textureImporter.isReadable = true;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.filterMode = FilterMode.Point;
        textureImporter.wrapMode = TextureWrapMode.Clamp;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
#endif

        Debug.Log("Изображение сохранено по пути: " + path);
    }

    void ChangeColor(Color newColor)
    {
        selectedColor = newColor;
        isErasing = false;
        isFilling = false;
    }

    void EnableEraser()
    {
        isErasing = true;
        isFilling = false;
    }

    void EraseCell(Vector2Int point)
    {
        if (point.x >= 0 && point.x < gridSize && point.y >= 0 && point.y < gridSize)
        {
            gridCells[point.x, point.y].color = Color.white;
        }
    }

    void EnableFilling()
    {
        isErasing = false;
        isFilling = true;
    }

    void SetFillColor(Color newFillColor)
    {
        fillColor = newFillColor;
    }

    void FillArea(Vector2Int start)
    {
        if (start.x >= 0 && start.x < gridSize && start.y >= 0 && start.y < gridSize)
        {
            Color targetColor = gridCells[start.x, start.y].color;
            if (targetColor == fillColor)
            {
                return; // Не заполняем, если начальная клетка уже цвета заливки
            }

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            gridCells[start.x, start.y].color = fillColor;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int x = current.x;
                int y = current.y;

                // Проверяем, что заливка не выходит за пределы второго ряда крайних пикселей
                if (x > 1 && gridCells[x - 1, y].color == targetColor)
                {
                    gridCells[x - 1, y].color = fillColor;
                    queue.Enqueue(new Vector2Int(x - 1, y));
                }
                if (x < gridSize - 2 && gridCells[x + 1, y].color == targetColor)
                {
                    gridCells[x + 1, y].color = fillColor;
                    queue.Enqueue(new Vector2Int(x + 1, y));
                }
                if (y > 1 && gridCells[x, y - 1].color == targetColor)
                {
                    gridCells[x, y - 1].color = fillColor;
                    queue.Enqueue(new Vector2Int(x, y - 1));
                }
                if (y < gridSize - 2 && gridCells[x, y + 1].color == targetColor)
                {
                    gridCells[x, y + 1].color = fillColor;
                    queue.Enqueue(new Vector2Int(x, y + 1));
                }
            }
        }
    }

    void SaveGridState()
    {
        Color[,] currentState = new Color[gridSize, gridSize];
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                currentState[x, y] = gridCells[x, y].color;
            }
        }
        undoStack.Push(currentState);
    }

    void UndoLastAction()
    {
        if (undoStack.Count > 0)
        {
            Color[,] previousState = undoStack.Pop();
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    gridCells[x, y].color = previousState[x, y];
                }
            }
        }
    }
}
