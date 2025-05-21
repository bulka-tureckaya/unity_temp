using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SchemePlacer : MonoBehaviour
{
    public Image schemeImage;
    public int gridSize = 64;

    private bool[,] occupied;
    private RectTransform schemeRect;

    public static List<PlacedItem> placedItems = new List<PlacedItem>();

    void Start()
    {
        schemeRect = schemeImage.GetComponent<RectTransform>();
        occupied = new bool[gridSize, gridSize];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && InventoryManager.selectedItem != null)
        {
            InventoryManager.selectedItem.rotation = (InventoryManager.selectedItem.rotation + 90) % 360;
            Debug.Log("Rotation: " + InventoryManager.selectedItem.rotation);
        }


        if (Input.GetMouseButtonDown(0))
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(schemeRect, Input.mousePosition, null, out localPoint))
            {
                float cellSize = schemeRect.rect.width / gridSize;
                int gridX = Mathf.FloorToInt((localPoint.x + schemeRect.rect.width / 2) / cellSize);
                int gridY = Mathf.FloorToInt((schemeRect.rect.height / 2 - localPoint.y) / cellSize);

                if (InventoryManager.selectedItem != null)
                {
                    TryPlaceItem(InventoryManager.selectedItem, gridX, gridY, cellSize, localPoint);
                }
            }
        }
    }

    public void TryPlaceItem(Item item, int clickX, int clickY, float cellSize, Vector2 localClickPoint)
    {
        int width = item.width;
        int height = item.height;
        int rotation = item.rotation;

        int startX = clickX;
        int startY = clickY;

        // Смещение стартовой точки в зависимости от поворота
        switch (rotation)
        {
            case 0:
                // ничего не менять
                break;
            case 90:
                startX = clickX - width + 1;
                break;
            case 180:
                startX = clickX - width + 1;
                startY = clickY - height + 1;
                break;
            case 270:
                startY = clickY - height + 1;
                break;
        }

        // Учет поворота размеров
        if (rotation == 90 || rotation == 270)
        {
            int temp = width;
            width = height;
            height = temp;
        }

        // Проверка границ
        if (startX < 0 || startY < 0 || startX + width > gridSize || startY + height > gridSize)
        {
            Debug.Log("Объект выходит за границы здания.");
            return;
        }

        // Проверка занятых клеток
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int gridX = startX + x;
                int gridY = startY + y;

                if (!IsRoomCell(gridX, gridY) || occupied[gridX, gridY])
                {
                    Debug.Log("Объект нельзя разместить здесь.");
                    return;
                }
            }
        }

        // Создание объекта
        GameObject placedItem = new GameObject("Placed_" + item.itemName);
        placedItem.transform.SetParent(schemeImage.transform, false);
        Image itemImage = placedItem.AddComponent<Image>();

        if (item.itemTexture is Texture2D tex2D)
        {
            itemImage.sprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0.5f, 0.5f));
        }
        itemImage.color = item.displayColor;

        RectTransform rt = placedItem.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(cellSize * width, cellSize * height);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(
            (startX + width / 2f) * cellSize - schemeRect.rect.width / 2f,
            schemeRect.rect.height / 2f - (startY + height / 2f) * cellSize
        );
        rt.localEulerAngles = new Vector3(0, 0, -rotation);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                occupied[startX + x, startY + y] = true;
            }
        }

        PlacedItem newPlaced = placedItem.AddComponent<PlacedItem>();
        newPlaced.startX = startX;
        newPlaced.startY = startY;
        newPlaced.width = width;
        newPlaced.height = height;
        newPlaced.item = item;
        newPlaced.schemePlacer = this;
        placedItems.Add(newPlaced);

        Debug.Log($"Размещён: {item.itemName} ({rotation}°) в {startX},{startY}");
    }



    bool IsRoomCell(int gridX, int gridY)
    {
        if (schemeImage.sprite == null || schemeImage.sprite.texture == null)
        {
            Debug.LogWarning("У схемы отсутствует спрайт или текстура.");
            return false;
        }
        Texture2D texture = schemeImage.sprite.texture;
        int pixelX = Mathf.FloorToInt((float)gridX / gridSize * texture.width);
        int pixelY = Mathf.FloorToInt((float)gridY / gridSize * texture.height);
        Color cellColor = texture.GetPixel(pixelX, texture.height - 1 - pixelY);
        float threshold = 0.1f;
        return Mathf.Abs(cellColor.r - 0.5f) < threshold &&
               Mathf.Abs(cellColor.g - 0.5f) < threshold &&
               Mathf.Abs(cellColor.b - 0.5f) < threshold;
    }

    public void RemovePlacedItem(PlacedItem pi)
    {
        for (int x = pi.startX; x < pi.startX + pi.width; x++)
        {
            for (int y = pi.startY; y < pi.startY + pi.height; y++)
            {
                occupied[x, y] = false;
            }
        }
        placedItems.Remove(pi);
        Destroy(pi.gameObject);
        Debug.Log("Удалён объект: " + pi.gameObject.name);
    }

    public void SaveSchemeImage()
    {
        StartCoroutine(CaptureSchemeImage());
    }

    private IEnumerator CaptureSchemeImage()
    {
        yield return new WaitForEndOfFrame();

        Vector3[] worldCorners = new Vector3[4];
        schemeImage.rectTransform.GetWorldCorners(worldCorners);

        Vector2 min = RectTransformUtility.WorldToScreenPoint(null, worldCorners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(null, worldCorners[2]);
        Rect rect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);

        Texture2D fullTex = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
        fullTex.ReadPixels(rect, 0, 0);
        fullTex.Apply();

        Texture2D resizedTex = new Texture2D(64, 64, TextureFormat.RGB24, false);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float sourceX = (x + 0.5f) * fullTex.width / 64f;
                float sourceY = (y + 0.5f) * fullTex.height / 64f;
                Color pixel = fullTex.GetPixel((int)sourceX, (int)sourceY);
                resizedTex.SetPixel(x, y, pixel);
            }
        }
        resizedTex.Apply();

        string path = Application.dataPath + "/Resources/scheme_image.png";
        File.WriteAllBytes(path, resizedTex.EncodeToPNG());

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        Debug.Log("Схема сохранена: " + path);
    }

    public RectTransform GetSchemeRect() => schemeRect;
    public bool[,] GetOccupiedGrid() => occupied;
    public void SetOccupied(int x, int y, bool value)
    {
        if (x >= 0 && x < gridSize && y >= 0 && y < gridSize)
            occupied[x, y] = value;
    }
}
