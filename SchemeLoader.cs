using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SchemeLoader : MonoBehaviour
{
    public Image schemeImage; // Объект Image для отображения схемы
    public int gridSize = 64; // Размер сетки (например, 64x64)
    private Color[,] gridColors; // Массив цветов для каждой клетки
    private RectTransform gridRectTransform;
    private Image[,] gridCells; // Массив объектов Image для клеток

    void Start()
    {
        LoadScheme();
        if (schemeImage.sprite != null) // Проверяем, что спрайт загружен
        {
            CreateGrid();
        }
        else
        {
            Debug.LogError("Схема не загружена. Проверьте папку Resources и настройки.");
        }
    }

    // Загружает сохранённое изображение схемы по статическому пути
    void LoadScheme()
    {
        string path = "Assets/Resources/grid_image.png"; // Статический путь к файлу
        if (!File.Exists(path))
        {
            Debug.LogError($"Файл не найден по пути: {path}");
            return;
        }

        // Читаем байты изображения
        byte[] bytes = File.ReadAllBytes(path);

        // Создаём текстуру
        Texture2D texture = new Texture2D(2, 2); // Создаём временную текстуру
        if (!texture.LoadImage(bytes))
        {
            Debug.LogError("Не удалось загрузить изображение!");
            return;
        }

        // Устанавливаем фильтрацию для текстуры
        texture.filterMode = FilterMode.Point; // Точечная фильтрация
        texture.wrapMode = TextureWrapMode.Clamp;

        // Создаём спрайт из текстуры
        Sprite schemeSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        schemeImage.sprite = schemeSprite;

        Debug.Log("Схема успешно загружена.");
    }

    // Создаёт сетку клеток поверх изображения схемы
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
                // Создаём объект для клетки
                GameObject cell = new GameObject($"Cell_{x}_{y}");
                cell.transform.SetParent(schemeImage.transform);

                // Добавляем компонент Image
                Image cellImage = cell.AddComponent<Image>();
                cellImage.color = Color.clear; // Прозрачный цвет по умолчанию

                // Настройка размера и позиции клетки
                RectTransform cellRectTransform = cell.GetComponent<RectTransform>();
                cellRectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                cellRectTransform.anchoredPosition = new Vector2(
                    x * cellSize - gridRectTransform.rect.width / 2 + cellSize / 2,
                    -y * cellSize + gridRectTransform.rect.height / 2 - cellSize / 2
                );

                // Сохраняем ссылку на клетку
                gridCells[x, y] = cellImage;

                // Определяем цвет клетки на основе изображения
                gridColors[x, y] = GetPixelColor(x, y);
            }
        }
    }

    // Получает цвет пикселя из загруженного изображения
    Color GetPixelColor(int x, int y)
    {
        if (schemeImage.sprite == null || schemeImage.sprite.texture == null)
        {
            Debug.LogError("Текстура не загружена или недоступна для чтения!");
            return Color.clear;
        }

        Texture2D texture = schemeImage.sprite.texture;
        int pixelX = Mathf.FloorToInt((float)x / gridSize * texture.width);
        int pixelY = Mathf.FloorToInt((float)y / gridSize * texture.height);
        return texture.GetPixel(pixelX, texture.height - 1 - pixelY); // Переворот по вертикали
    }

    // Обрабатывает клики по клеткам
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

    // Обработчик клика по клетке
    void OnCellClicked(int x, int y)
    {
        Debug.Log($"Клетка [{x}, {y}] была выбрана. Цвет: {gridColors[x, y]}");

        // Здесь можно добавить логику для размещения объектов из инвентаря
        // Например, проверить, что клетка является "комнатой" (не стеной)
        if (IsRoomCell(gridColors[x, y]))
        {
            Debug.Log("Это комната. Можно разместить объект.");
        }
        else
        {
            Debug.Log("Это стена или другая запрещённая область.");
        }
    }

    // Проверяет, является ли клетка частью комнаты (по цвету)
    bool IsRoomCell(Color cellColor)
    {
        // Проверяем, что цвет близок к серому (RGB значения примерно равны)
        float threshold = 0.1f; // Порог для сравнения
        return Mathf.Abs(cellColor.r - 0.5f) < threshold &&
               Mathf.Abs(cellColor.g - 0.5f) < threshold &&
               Mathf.Abs(cellColor.b - 0.5f) < threshold;
    }
}