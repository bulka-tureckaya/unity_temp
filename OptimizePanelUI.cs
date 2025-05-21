using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class OptimizePanelUI : MonoBehaviour
{
    [System.Serializable]
    public class OptimizeRow
    {
        public string itemName;
        public Image icon;
        public TMP_Text counterText;
        public Button plusButton;
        public Button minusButton;
        public int count = 0;
        public Button resetButton;
    }

    public List<OptimizeRow> rows = new List<OptimizeRow>();
    public Button applyButton;
    public SchemePlacer schemePlacer;
    public Button resetButton;



    void Start()
    {
        foreach (var row in rows)
        {
            row.counterText.text = "0";
            var localRow = row;

            localRow.plusButton.onClick.AddListener(() => {
                localRow.count++;
                localRow.counterText.text = localRow.count.ToString();
            });

            localRow.minusButton.onClick.AddListener(() => {
                if (localRow.count > 0) localRow.count--;
                localRow.counterText.text = localRow.count.ToString();
            });
        }

        applyButton.onClick.AddListener(ApplyOptimize);
        resetButton.onClick.AddListener(ResetAllCounts);
    }

    void ResetAllCounts()
    {
        foreach (var row in rows)
        {
            row.count = 0;
            row.counterText.text = "0";
        }
        ClearScheme();
    }

    void ApplyOptimize()
    {
        AnalyzeRoomsFromImage();
        AutoPlaceItems();
    }

    void ClearScheme()
    {
        bool[,] occupied = schemePlacer.GetOccupiedGrid();
        int gridSize = occupied.GetLength(0);
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                occupied[x, y] = false;

        foreach (Transform child in schemePlacer.schemeImage.transform)
            Destroy(child.gameObject);

        SchemePlacer.placedItems.Clear();
    }

    void AutoPlaceItems()
    {
        float cellSize = schemePlacer.schemeImage.rectTransform.rect.width / schemePlacer.gridSize;
        ClearScheme();

        // 1. Размещаем все КРОМЕ ПК
        foreach (var row in rows)
        {
            if (row.count <= 0 || row.itemName == "ПК") continue;

            Item item = InventoryManager.Instance?.GetItemByName(row.itemName);
            if (item == null)
            {
                Debug.LogError("Item не найден: " + row.itemName);
                continue;
            }

            int placedCount = 0;
            for (int i = 0; i < schemePlacer.gridSize && placedCount < row.count; i++)
            {
                for (int j = 0; j < schemePlacer.gridSize && placedCount < row.count; j++)
                {
                    if (TryAutoPlaceItem(item, i, j, cellSize))
                        placedCount++;
                }
            }

            if (placedCount < row.count)
                Debug.LogWarning($"Не удалось разместить все объекты {item.itemName}. Размещено: {placedCount} из {row.count}");
        }

        // 2. Отдельно размещаем ПК
        var pcRow = rows.Find(r => r.itemName == "ПК");
        if (pcRow != null && pcRow.count > 0)
        {
            Item pcItem = InventoryManager.Instance?.GetItemByName("ПК");
            if (pcItem == null)
            {
                pcItem.onlyPlaceOnDeskTop = true;
                Debug.LogError("ПК не найден в инвентаре");
                return;
            }

            int placedCount = 0;
            for (int i = 0; i < schemePlacer.gridSize && placedCount < pcRow.count; i++)
            {
                for (int j = 0; j < schemePlacer.gridSize && placedCount < pcRow.count; j++)
                {
                    if (TryAutoPlaceItem(pcItem, i, j, cellSize))
                    {
                        placedCount++;
                        Debug.Log($"ПК размещён на позиции: {i}, {j}");
                    }
                }
            }

            if (placedCount < pcRow.count)
                Debug.LogWarning($"Не удалось разместить ПК. Размещено: {placedCount} из {pcRow.count}");
        }
    }

    bool TryAutoPlaceItem(Item item, int startX, int startY, float cellSize)
    {
        var rules = GetPlacementRules(item);

        if (!CanPlaceItem(item, startX, startY, rules))
            return false;

        int before = SchemePlacer.placedItems.Count;
        schemePlacer.TryPlaceItem(item, startX, startY, cellSize, new Vector2(startX, startY));

        return SchemePlacer.placedItems.Count > before;
    }

    Dictionary<string, int> GetPlacementRules(Item item)
    {
        var rules = new Dictionary<string, int>();

        if (item.itemName == "Сервер")
        {
            rules.Add("front", 3);
            rules.Add("sides", 2);
            rules.Add("back", 2);
        }
        else if (item.itemName == "ПК")
        {
            rules.Add("front", 1);
            rules.Add("sides", 1);
            rules.Add("back", 0);
        }
        else
        {
            rules.Add("front", 2);
            rules.Add("sides", 2);
            rules.Add("back", 0);
        }

        return rules;
    }

    bool CanPlaceItem(Item item, int startX, int startY, Dictionary<string, int> rules)
    {
        int actualWidth = (item.rotation % 180 == 0) ? item.width : item.height;
        int actualHeight = (item.rotation % 180 == 0) ? item.height : item.width;

        for (int x = startX; x < startX + actualWidth; x++)
        {
            for (int y = startY; y < startY + actualHeight; y++)
            {
                if (corridor_coordinates.Contains(new Vector2Int(x, y)))
                    return false;
            }
        }

        if (item.itemName == "ПК")
        {
            // Спецлогика для ПК
            return CanPlacePC(item, startX, startY, actualWidth, actualHeight);
        }

        if (!IsAreaFree(startX, startY, actualWidth, actualHeight, item))
            return false;

        foreach (PlacedItem placed in SchemePlacer.placedItems)
        {
            if (IsTooClose(item, startX, startY, actualWidth, actualHeight, placed, rules))
                return false;
        }

        return true;
    }

    bool CanPlacePC(Item item, int startX, int startY, int width, int height)
    {
        foreach (PlacedItem pi in SchemePlacer.placedItems)
        {
            if (pi.item.itemName == "Стол со стулом")
            {
                int tableLeft = pi.startX;
                int tableRight = pi.startX + pi.width;
                int tableTop = pi.startY;
                int tableBottom = pi.startY + pi.height;

                bool isInsideTable =
                    startX >= tableLeft &&
                    startY >= tableTop &&
                    startX + width <= tableRight &&
                    startY + height <= tableBottom;

                if (!isInsideTable)
                    continue;

                // Новая проверка: ПК должен быть строго по центру стола по ширине
                int tableCenterX = tableLeft + pi.width / 2;
                int pcCenterX = startX + width / 2;

                if (Mathf.Abs(tableCenterX - pcCenterX) > 0)  // разрешаем только точный центр
                    continue;

                // Проверка занятости клеток
                for (int x = startX; x < startX + width; x++)
                {
                    for (int y = startY; y < startY + height; y++)
                    {
                        if (schemePlacer.GetOccupiedGrid()[x, y])
                        {
                            bool isPartOfThisTable = (x >= pi.startX && x < pi.startX + pi.width &&
                                                      y >= pi.startY && y < pi.startY + pi.height);
                            if (!isPartOfThisTable)
                                return false;
                        }
                    }
                }

                return true;
            }
        }

        return false;
    }



    bool HasDeskBorderPixels(int startX, int startY, int width, int height)
    {
        Texture2D tex = schemePlacer.schemeImage.sprite.texture;
        Rect rect = schemePlacer.schemeImage.sprite.rect;
        float texWidth = rect.width;
        float texHeight = rect.height;

        float cellSize = schemePlacer.schemeImage.rectTransform.rect.width / schemePlacer.gridSize;

        Color expectedColor = new Color(0.6f, 0.4f, 0.2f);

        bool CheckPixel(int gridX, int gridY)
        {
            float uiX = gridX * cellSize;
            float uiY = gridY * cellSize;

            int px = Mathf.FloorToInt(rect.x + uiX);
            int py = Mathf.FloorToInt(rect.y + uiY);

            if (px < 0 || px >= texWidth || py < 0 || py >= texHeight) return false;

            Color color = tex.GetPixel(px, py);
            return Approximately(color, expectedColor);
        }

        // Проверяем перед (вниз по Y)
        int frontY = startY + height;
        for (int x = startX; x < startX + width; x++)
            if (!CheckPixel(x, frontY))
                return false;

        // Проверяем левый бок
        int leftX = startX - 1;
        if (leftX >= 0)
        {
            for (int y = startY; y < startY + height; y++)
                if (!CheckPixel(leftX, y))
                    return false;
        }

        // Проверяем правый бок
        int rightX = startX + width;
        if (rightX < schemePlacer.gridSize)
        {
            for (int y = startY; y < startY + height; y++)
                if (!CheckPixel(rightX, y))
                    return false;
        }

        return true;
    }

    bool Approximately(Color a, Color b, float tolerance = 0.05f)
    {
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance;
    }


    bool IsAreaFree(int startX, int startY, int width, int height, Item item = null)
    {
        if (startX < 0 || startY < 0 ||
            startX + width > schemePlacer.gridSize ||
            startY + height > schemePlacer.gridSize)
            return false;

        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                bool cellOccupied = schemePlacer.GetOccupiedGrid()[x, y];

                if (cellOccupied)
                {
                    // Проверим, не "Стол со стулом" ли это
                    bool isTable = false;
                    foreach (PlacedItem pi in SchemePlacer.placedItems)
                    {
                        if (pi.item.itemName == "Стол со стулом")
                        {
                            if (x >= pi.startX && x < pi.startX + pi.width &&
                                y >= pi.startY && y < pi.startY + pi.height)
                            {
                                isTable = true;
                                break;
                            }
                        }
                    }

                    // Если предмет разрешён для размещения на столе — и это действительно стол — всё ок
                    if (item != null && item.onlyPlaceOnDeskTop && isTable)
                    {
                        continue;
                    }

                    // В остальных случаях — клетка занята
                    return false;
                }
            }
        }

        return true;
    }




    bool IsTooClose(Item newItem, int x, int y, int width, int height, PlacedItem other, Dictionary<string, int> rules)
    {
        int padding = Mathf.Max(rules["front"], rules["sides"], rules["back"]);

        int xMin = x - padding;
        int yMin = y - padding;
        int xMax = x + width + padding;
        int yMax = y + height + padding;

        int oxMin = other.startX;
        int oyMin = other.startY;
        int oxMax = other.startX + other.width;
        int oyMax = other.startY + other.height;

        bool overlap = xMin < oxMax && xMax > oxMin && yMin < oyMax && yMax > oyMin;

        return overlap;
    }

    bool IsOnTable(int startX, int startY, int width, int height)
    {
        foreach (PlacedItem pi in SchemePlacer.placedItems)
        {
            if (pi.item.itemName == "Стол со стулом")
            {
                if (startX >= pi.startX &&
                    startY >= pi.startY &&
                    startX + width <= pi.startX + pi.width &&
                    startY + height <= pi.startY + pi.height)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // ========== Система анализа комнат ==========

    class RoomInfo
    {
        public int area = 0;
        public int doors = 0;
        public int windows = 0;
        public List<Vector2Int> tiles = new List<Vector2Int>();
        public List<Vector2Int> coordinates = new List<Vector2Int>();
    }
    List<Vector2Int> corridor_coordinates = new List<Vector2Int>();

    void AnalyzeRoomsFromImage()
    {
        Texture2D tex = schemePlacer.schemeImage.sprite.texture;
        int texWidth = tex.width;
        int texHeight = tex.height;

        int gridSize = schemePlacer.gridSize; // например 64
        bool[,] visited = new bool[gridSize, gridSize];
        List<RoomInfo> rooms = new List<RoomInfo>();

        for (int gx = 0; gx < gridSize; gx++)
        {
            for (int gy = 0; gy < gridSize; gy++)
            {
                if (!visited[gx, gy] && IsRoomPixel(GetGridPixel(tex, gx, gy)))
                {
                    RoomInfo room = new RoomInfo();
                    FloodFillRoomGrid(gx, gy, visited, room, tex);
                    rooms.Add(room);
                }
            }
        }

        Debug.Log($"Найдено комнат: {rooms.Count}");
        for (int i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            string type = (room.doors >= 1 && (float)room.doors / (room.doors + room.windows + 0.01f) >= 0.6f) ? "Коридор" : "Комната";
            Debug.Log($"Комната #{i + 1}: Тип = {type}, Площадь = {room.area}, Дверей = {room.doors}, Окон = {room.windows}");

            if (type == "Коридор")
            {
                corridor_coordinates.AddRange(room.coordinates);
            }
        }

        SaveCorridorMapToFile();
    }

    Color GetGridPixel(Texture2D tex, int gridX, int gridY)
    {
        int gridSize = schemePlacer.gridSize;
        float px = (float)gridX / gridSize * tex.width;
        float py = (float)gridY / gridSize * tex.height;

        // Переворачиваем по Y, если нужно (снизу вверх → сверху вниз)
        int flippedY = tex.height - Mathf.FloorToInt(py) - 1;

        return tex.GetPixel(Mathf.FloorToInt(px), flippedY);
    }

    void FloodFillRoomGrid(int startGX, int startGY, bool[,] visited, RoomInfo room, Texture2D tex)
    {
        int gridSize = schemePlacer.gridSize;
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(new Vector2Int(startGX, startGY));
        visited[startGX, startGY] = true;

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            room.tiles.Add(curr);
            room.coordinates.Add(curr);
            room.area++;

            foreach (Vector2Int dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                int nx = curr.x + dir.x;
                int ny = curr.y + dir.y;

                if (nx >= 0 && ny >= 0 && nx < gridSize && ny < gridSize && !visited[nx, ny])
                {
                    Color color = GetGridPixel(tex, nx, ny);
                    if (IsRoomPixel(color))
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                    else if (IsDoorPixel(color))
                    {
                        room.doors++;
                    }
                    else if (IsWindowPixel(color))
                    {
                        room.windows++;
                    }
                }
            }
        }
    }

    void SaveCorridorMapToFile()
    {
        string filePath = Application.dataPath + "/corridor_map.txt";
        string output = "";

        // Создаем матрицу 64x64
        for (int y = 0; y < 64; y++)
        {
            string line = "";
            for (int x = 0; x < 64; x++)
            {
                // Проверяем есть ли координата в списке
                bool isCorridor = corridor_coordinates.Contains(new Vector2Int(x, y));
                line += isCorridor ? "1 " : "0 ";
            }
            output += line.Trim() + "\n"; // Убираем последний пробел в строке
        }

        // Записываем в файл
        System.IO.File.WriteAllText(filePath, output);
        Debug.Log("Карта сохранена: " + filePath);
    }

    void FloodFillRoom(int startX, int startY, bool[,] visited, RoomInfo room, Texture2D tex)
    {
        int width = tex.width;
        int height = tex.height;
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> borderPixels = new HashSet<Vector2Int>();

        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            room.tiles.Add(curr);
            room.coordinates.Add(curr); // Добавляем Vector2Int вместо кортежа
            room.area++;

            foreach (Vector2Int dir in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                int nx = curr.x + dir.x;
                int ny = curr.y + dir.y;

                if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                {
                    Color c = tex.GetPixel(nx, ny);
                    if (!visited[nx, ny] && IsRoomPixel(c))
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                    else if (!IsRoomPixel(c))
                    {
                        borderPixels.Add(new Vector2Int(nx, ny));
                    }
                }
            }
        }

        CountWindowAndDoorClusters(borderPixels, tex, room);
    }

    void CountWindowAndDoorClusters(HashSet<Vector2Int> border, Texture2D tex, RoomInfo room)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        foreach (var pos in border)
        {
            if (visited.Contains(pos)) continue;
            Color color = tex.GetPixel(pos.x, pos.y);

            if (IsColor(color, Color.red))
            {
                FloodBorderCluster(pos, visited, border, tex, Color.red);
                room.doors++;
            }
            else if (IsColor(color, Color.green))
            {
                FloodBorderCluster(pos, visited, border, tex, Color.green);
                room.windows++;
            }
        }
    }

    void FloodBorderCluster(Vector2Int start, HashSet<Vector2Int> visited, HashSet<Vector2Int> border, Texture2D tex, Color targetColor)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();

            foreach (var dir in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int next = curr + dir;
                if (border.Contains(next) && !visited.Contains(next))
                {
                    Color c = tex.GetPixel(next.x, next.y);
                    if (IsColor(c, targetColor))
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }
        }
    }

    bool IsRoomPixel(Color color)
    {
        return Mathf.Abs(color.r - 0.5f) < 0.1f &&
               Mathf.Abs(color.g - 0.5f) < 0.1f &&
               Mathf.Abs(color.b - 0.5f) < 0.1f;
    }
    //bool IsRoomPixel(Color color)
    //{
    //    return Approximately(color, new Color(1f, 1f, 1f)); // Белый
    //}

    // ТЕПЕРЬ ОН ВЕСЬ ПОЛ СЧИТАЕТ КОРИДОРОМ БАЛБЕС!!!
    // НЕТ ОН ТЕПЕРЬ ДВЕРИ И ОКНА НЕВЕРНО СЧИТАЕТ!!!

    bool IsDoorPixel(Color color)
    {
        return Approximately(color, new Color(1f, 0f, 0f)); // Красный
    }

    bool IsWindowPixel(Color color)
    {
        return Approximately(color, new Color(0f, 1f, 0f)); // Зеленый
    }


    bool IsColor(Color c, Color target, float tol = 0.1f)
    {
        return Mathf.Abs(c.r - target.r) < tol &&
               Mathf.Abs(c.g - target.g) < tol &&
               Mathf.Abs(c.b - target.b) < tol;
    }

}
