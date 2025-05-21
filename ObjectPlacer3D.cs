using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ObjectPlacer3D : MonoBehaviour
{
    [Header("2D Scheme Settings")]
    // Имя файла схемы (без расширения) из Assets/Resources
    public string schemeImageName = "scheme_image";
    private Texture2D schemeTexture;
    public float unitSize = 50f;          // 1 ячейка = 50 Unity-единиц
    public int gridResolution = 64;       // схема 64x64 пикселей
    public float floorHeight = 10f;       // высота пола здания
    public float objectYOffset = 25f;     // базовое смещение относительно пола
    public float tableHeight = 100f;      // высота стола (100 Unity-единиц)

    [Header("Mappings (все задается в коде)")]
    // Список соответствий: если цвет пикселя совпадает с mapping.color, то этот объект распознается
    public List<ObjectMapping> mappings = new List<ObjectMapping>();

    [Header("UI")]
    public Button placeObjectsButton;   // кнопка для запуска размещения объектов

    // Массив для отслеживания, какие пиксели уже обработаны
    private bool[,] visited;

    // Префабы – назначь их через инспектор или загрузи через Resources.Load (если лежат там)
    public GameObject serverPrefab;
    public GameObject switchPrefab;
    public GameObject routerPrefab;
    public GameObject accessPointPrefab;
    public GameObject firewallPrefab;
    public GameObject nasPrefab;
    public GameObject modemPrefab;
    public GameObject deskPrefab;
    public GameObject pcPrefab;
    public GameObject printerPrefab;

    void Awake()
    {
        // Увеличим ускорение свободного падения глобально (будьте осторожны, это влияет на всю физику сцены)
        Physics.gravity = new Vector3(0, -30f, 0);
    }

    void Start()
    {
        // Загружаем схему из Resources
        schemeTexture = Resources.Load<Texture2D>(schemeImageName);
        if (schemeTexture == null)
        {
            Debug.LogError("Не удалось загрузить схему: " + schemeImageName);
            return;
        }
        visited = new bool[schemeTexture.width, schemeTexture.height];

        // Инициализируем маппинги. Обратите внимание:
        // - Для объектов, которые должны стоять на столе, выставлен флаг onlyPlaceOnDeskTop
        // - Стол (объект с именем "Стол со стулом") генерируется без дополнительного подъема.
        mappings = new List<ObjectMapping>()
        {
            new ObjectMapping(new Color(0.95f, 0.5f, 0.1f), "Сервер", serverPrefab, 3, 3, 200, Vector3.zero, false),
            new ObjectMapping(new Color(0.3f, 0.8f, 1f), "Коммутатор", switchPrefab, 1, 1, 50, Vector3.zero, false),
            new ObjectMapping(new Color(0.2f, 1f, 0.7f), "Маршрутизатор", routerPrefab, 1, 1, 50, Vector3.zero, false),
            new ObjectMapping(new Color(1f, 0.85f, 0.1f), "Точка доступа", accessPointPrefab, 1, 1, 50, Vector3.zero, false),
            new ObjectMapping(new Color(0.8f, 0.3f, 1f), "Межсетевой экран", firewallPrefab, 2, 1, 50, Vector3.zero, false),
            new ObjectMapping(new Color(0.1f, 0.95f, 0.9f), "Сетевое хранилище", nasPrefab, 2, 2, 150, Vector3.zero, false),
            new ObjectMapping(new Color(1f, 0.6f, 0.8f), "Модем", modemPrefab, 1, 1, 50, Vector3.zero, false),
            new ObjectMapping(new Color(0.6f, 0.4f, 0.2f), "Стол со стулом", deskPrefab, 5, 3, 100, Vector3.zero, false),
            new ObjectMapping(new Color(0.8f, 0.8f, 1f), "ПК", pcPrefab, 2, 3, 50, Vector3.zero, true),
            new ObjectMapping(new Color(0.4f, 0.4f, 0.8f), "Принтер/сканер", printerPrefab, 2, 3, 50, Vector3.zero, false),
        };

        if (placeObjectsButton != null)
        {
            placeObjectsButton.onClick.AddListener(PlaceObjectsFromScheme);
        }
    }

    public void PlaceObjectsFromScheme()
    {
        int width = schemeTexture.width;
        int height = schemeTexture.height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (visited[x, y])
                    continue;

                Color pixelColor = schemeTexture.GetPixel(x, y);
                pixelColor.a = 1f; // игнорируем альфу

                if (IsReservedColor(pixelColor))
                {
                    visited[x, y] = true;
                    continue;
                }

                // Собираем группу смежных пикселей (flood fill)
                List<Vector2Int> region = FloodFill(x, y, pixelColor);
                if (region.Count == 0)
                    continue;

                // Вычисляем bounding box региона
                int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
                foreach (Vector2Int pos in region)
                {
                    if (pos.x < minX) minX = pos.x;
                    if (pos.x > maxX) maxX = pos.x;
                    if (pos.y < minY) minY = pos.y;
                    if (pos.y > maxY) maxY = pos.y;
                }
                int regionWidth = maxX - minX + 1;
                int regionHeight = maxY - minY + 1;

                // Получаем сопоставление по цвету
                ObjectMapping mapping = GetMappingForColor(pixelColor);
                if (mapping == null || mapping.prefab == null)
                    continue;

                // Определяем мировую позицию по X и Z (центр bounding box)
                float centerX = (minX + maxX + 1) / 2f - 0.5f;
                float centerY = (minY + maxY + 1) / 2f - 0.5f;
                float worldX = centerX * unitSize;
                float worldZ = centerY * unitSize;

                // Определяем желаемый уровень нижней границы объекта
                float desiredBottomLevel = floorHeight + objectYOffset;
                if (mapping.onlyPlaceOnDeskTop)
                {
                    desiredBottomLevel += tableHeight;
                }
                else if (mapping.objectName != "Стол со стулом")
                {
                    // Проверяем, есть ли в указанной области стол (с тегом "Table")
                    Vector3 halfExtents = new Vector3(regionWidth * unitSize, mapping.defaultHeight * 0.5f, regionHeight * unitSize) * 0.5f;
                    if (IsInTableArea(new Vector3(worldX, floorHeight + objectYOffset, worldZ), halfExtents))
                    {
                        desiredBottomLevel += tableHeight;
                    }
                }
                // Для стола ("Стол со стулом") desiredBottomLevel остаётся без изменений.

                // Вычисляем позицию объекта так, чтобы его нижняя граница совпадала с desiredBottomLevel.
                float spawnY = desiredBottomLevel + mapping.defaultHeight * 0.5f;
                Vector3 spawnPos = new Vector3(worldX, spawnY, worldZ);

                // Создаем объект
                GameObject obj = Instantiate(mapping.prefab, spawnPos, Quaternion.Euler(mapping.rotation));
                Vector3 targetScale = new Vector3(regionWidth * unitSize, mapping.defaultHeight, regionHeight * unitSize);
                obj.transform.localScale = targetScale;
                obj.transform.SetParent(transform);

                // Настраиваем Rigidbody, чтобы объект вел себя "тяжело":
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = obj.AddComponent<Rigidbody>();
                rb.mass = 100f;            // увеличиваем массу
                rb.drag = 1f;              // повышенное сопротивление для стабильного падения
                rb.angularDrag = 1f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                // Добавляем физический материал к коллайдеру с нулевой упругостью и высокой фрикцией:
                Collider col = obj.GetComponent<Collider>();
                if (col != null)
                {
                    PhysicMaterial mat = new PhysicMaterial();
                    mat.bounciness = 0f;
                    mat.dynamicFriction = 0.9f;
                    mat.staticFriction = 0.9f;
                    mat.frictionCombine = PhysicMaterialCombine.Multiply;
                    col.material = mat;
                }

                // Если это стол – назначаем ему тег "Table", чтобы другие объекты могли его обнаружить
                if (mapping.objectName == "Стол со стулом")
                    obj.tag = "Table";
            }
        }
        Debug.Log("3D объекты размещены согласно схеме.");
    }

    /// <summary>
    /// Проверяет, пересекается ли заданная область с объектом, имеющим тег "Table".
    /// </summary>
    bool IsInTableArea(Vector3 center, Vector3 halfExtents)
    {
        Collider[] cols = Physics.OverlapBox(center, halfExtents, Quaternion.identity);
        foreach (Collider col in cols)
        {
            if (col.CompareTag("Table"))
                return true;
        }
        return false;
    }

    List<Vector2Int> FloodFill(int startX, int startY, Color targetColor)
    {
        List<Vector2Int> region = new List<Vector2Int>();
        int w = schemeTexture.width;
        int h = schemeTexture.height;
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            region.Add(p);

            foreach (Vector2Int n in GetNeighbors(p, w, h))
            {
                if (!visited[n.x, n.y])
                {
                    Color nColor = schemeTexture.GetPixel(n.x, n.y);
                    nColor.a = 1f;
                    if (Approximately(nColor, targetColor))
                    {
                        visited[n.x, n.y] = true;
                        queue.Enqueue(n);
                    }
                }
            }
        }
        return region;
    }

    IEnumerable<Vector2Int> GetNeighbors(Vector2Int p, int w, int h)
    {
        int x = p.x, y = p.y;
        if (x > 0) yield return new Vector2Int(x - 1, y);
        if (x < w - 1) yield return new Vector2Int(x + 1, y);
        if (y > 0) yield return new Vector2Int(x, y - 1);
        if (y < h - 1) yield return new Vector2Int(x, y + 1);
    }

    bool Approximately(Color a, Color b, float tolerance = 0.05f)
    {
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance;
    }

    private bool IsReservedColor(Color c)
    {
        if (Approximately(c, Color.black)) return true;
        if (Approximately(c, Color.white)) return true;
        if (Approximately(c, Color.red)) return true;
        if (Approximately(c, Color.green)) return true;
        return false;
    }

    private ObjectMapping GetMappingForColor(Color c)
    {
        foreach (var mapping in mappings)
        {
            if (Approximately(mapping.color, c, 0.05f))
                return mapping;
        }
        return null;
    }
}

[System.Serializable]
public class ObjectMapping
{
    // Имя объекта (например, "Стол со стулом")
    public string objectName;
    // Цвет, соответствующий данному объекту в 2D-схеме
    public Color color;
    // 3D префаб, который будет создан
    public GameObject prefab;
    // Размеры объекта в ячейках (ширина по X и глубина по Z)
    public float sizeX;
    public float sizeZ;
    // Высота объекта в Unity-единицах (используется для расчёта spawn-позиции)
    public float defaultHeight;
    // Начальный поворот (Euler angles)
    public Vector3 rotation;
    // Флаг для объектов, которые должны ставиться только на стол (например, ПК)
    public bool onlyPlaceOnDeskTop;

    public ObjectMapping(Color color, string name, GameObject prefab, float sizeX, float sizeZ, float defaultHeight, Vector3 rotation, bool onlyPlaceOnDeskTop)
    {
        this.color = color;
        this.objectName = name;
        this.prefab = prefab;
        this.sizeX = sizeX;
        this.sizeZ = sizeZ;
        this.defaultHeight = defaultHeight;
        this.rotation = rotation;
        this.onlyPlaceOnDeskTop = onlyPlaceOnDeskTop;
    }
}
