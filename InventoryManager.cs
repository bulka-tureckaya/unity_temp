using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    // 12 ����� ��������� (��������� 12, ����� �� ��������� ���)
    public InventorySlot[] inventorySlots = new InventorySlot[12];
    // UI-�������� ����� (����������� � ����������, �� ������ ���� 12)
    public GameObject[] slotUIElements;

    // ��������� ������ ��� ����������
    public static Item selectedItem = null;
    // ������� ���� �������� (� ��������)
    public static int selectedRotation = 0;

    // ������ ������-�������, ������� ����� ������������ �� Canvas (UI Image � RectTransform)
    public GameObject cursorItemPrefab;
    // ������� ������-�������
    private GameObject cursorItemInstance;
    // �������� ������ �� �������
    public Vector2 cursorOffset = new Vector2(30, -30);

    // �������� ��� ��������
    public Texture serverTexture;
    public Texture switchTexture;
    public Texture routerTexture;
    public Texture accessPointTexture;
    public Texture firewallTexture;
    public Texture nasTexture;
    public Texture modemTexture;
    public Texture deskTexture;
    public Texture pcTexture;
    public Texture printerTexture;
    public Texture eraserTexture; // ��� �������

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // ���������� ��������� 11 ��������� (������ � 10 �� 11 ����� �������� �������)
        // ���������: Item(name, width, height, texture, displayColor, onlyPlaceOnDeskTop)
        inventorySlots[0] = new InventorySlot(new Item("������", 3, 3, serverTexture, new Color(0.95f, 0.5f, 0.1f)), 1);
        inventorySlots[1] = new InventorySlot(new Item("����������", 1, 1, switchTexture, new Color(0.3f, 0.8f, 1f)), 1);
        inventorySlots[2] = new InventorySlot(new Item("�������������", 1, 1, routerTexture, new Color(0.2f, 1f, 0.7f)), 1);
        inventorySlots[3] = new InventorySlot(new Item("����� �������", 1, 1, accessPointTexture, new Color(1f, 0.85f, 0.1f)), 1);
        inventorySlots[4] = new InventorySlot(new Item("���������� �����", 2, 1, firewallTexture, new Color(0.8f, 0.3f, 1f)), 1);
        inventorySlots[5] = new InventorySlot(new Item("������� ���������", 2, 2, nasTexture, new Color(0.1f, 0.95f, 0.9f)), 1);
        inventorySlots[6] = new InventorySlot(new Item("�����", 1, 1, modemTexture, new Color(1f, 0.6f, 0.8f)), 1);
        inventorySlots[7] = new InventorySlot(new Item("���� �� ������", 5, 3, deskTexture, new Color(0.6f, 0.4f, 0.2f)), 2);
        inventorySlots[8] = new InventorySlot(new Item("��", 3, 2, pcTexture, new Color(0.8f, 0.8f, 1f), true), 7);
        inventorySlots[9] = new InventorySlot(new Item("�������/������", 2, 3, printerTexture, new Color(0.4f, 0.4f, 0.8f)), 3);
        inventorySlots[10] = new InventorySlot(new Item("������", 0, 0, eraserTexture, Color.clear), 1);

        UpdateInventoryUI();
    }

    void Update()
    {
        // ���������� ������� ������-������� (������������ �� ��������)
        if (cursorItemInstance != null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRT = canvas.GetComponent<RectTransform>();
                Vector2 pos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, Input.mousePosition, canvas.worldCamera, out pos);
                cursorItemInstance.GetComponent<RectTransform>().anchoredPosition = pos + cursorOffset;
            }
        }

        // ������� ������ ��� ������� ������ ������ ����
        if (selectedItem != null && Input.GetMouseButtonDown(1))
        {
            RotateSelectedItem();
        }
    }

    public void UpdateInventoryUI()
    {
        for (int i = 0; i < slotUIElements.Length; i++)
        {
            if (inventorySlots[i] != null)
            {
                Image img = slotUIElements[i].GetComponentInChildren<Image>();
                Text txt = slotUIElements[i].GetComponentInChildren<Text>();

                if (img != null && inventorySlots[i].item.itemTexture != null)
                {
                    Texture2D tex2D = inventorySlots[i].item.itemTexture as Texture2D;
                    if (tex2D != null)
                    {
                        Sprite sp = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0.5f, 0.5f));
                        img.sprite = sp;
                        img.color = inventorySlots[i].item.displayColor;
                    }
                }
                if (txt != null)
                {
                    txt.text = inventorySlots[i].count.ToString();
                }
            }
        }
    }

    // �����, ���������� ��� ����� �� ������ ��������� (���������� ������ ������)
    public void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Length)
            return;

        if (inventorySlots[slotIndex] != null)
        {
            Image slotImage = slotUIElements[slotIndex].GetComponentInChildren<Image>();
            Sprite slotSprite = (slotImage != null) ? slotImage.sprite : null;

            // ���� ��� �� ������ ��� ������ � �������� �����
            if (selectedItem == inventorySlots[slotIndex].item)
            {
                DeselectItem();
            }
            else
            {
                SelectItem(inventorySlots[slotIndex].item, slotSprite);
            }
        }
    }

    // ����� �������: previewSprite ������� �� ������ (Source Image)
    public void SelectItem(Item item, Sprite previewSprite)
    {
        selectedItem = item;
        selectedRotation = 0; // ���������� �������
        Debug.Log("������ ������: " + item.itemName);

        if (cursorItemInstance == null && cursorItemPrefab != null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                cursorItemInstance = Instantiate(cursorItemPrefab, canvas.transform);
            }
            else
            {
                Debug.LogError("Canvas �� ������ � �����!");
            }
        }

        if (cursorItemInstance != null)
        {
            Image img = cursorItemInstance.GetComponent<Image>();
            if (img != null && previewSprite != null)
            {
                img.sprite = previewSprite;
                img.color = item.displayColor;
            }
            RectTransform rt = cursorItemInstance.GetComponent<RectTransform>();
            rt.localRotation = Quaternion.Euler(0, 0, 0);
            rt.localScale = Vector3.one * 0.8f;
        }
    }

    public void DeselectItem()
    {
        selectedItem = null;
        selectedRotation = 0;
        Debug.Log("������ ���� � ������.");
        if (cursorItemInstance != null)
        {
            Destroy(cursorItemInstance);
            cursorItemInstance = null;
        }
    }

    public void RotateSelectedItem()
    {
        if (selectedItem != null && selectedItem.isRotatable)
        {
            selectedItem.rotation = (selectedItem.rotation + 90) % 360;

            // ���������� ��������� ������
            if (cursorItemInstance != null)
            {
                cursorItemInstance.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, -selectedItem.rotation);
            }

            // ������������ �������� ��� �������� �� 90 ��� 270
            if (selectedItem.rotation == 90 || selectedItem.rotation == 270)
            {
                selectedItem.width = selectedItem.originalHeight;
                selectedItem.height = selectedItem.originalWidth;
            }
            else
            {
                selectedItem.width = selectedItem.originalWidth;
                selectedItem.height = selectedItem.originalHeight;
            }

            Debug.Log($"������ ��������. ����� �������: {selectedItem.width}x{selectedItem.height}, Rotation: {selectedItem.rotation}");
        }
    }


    public Item GetItemByName(string name)
    {
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot != null && slot.item != null && slot.item.itemName == name)
            {
                return slot.item;
            }
        }
        Debug.LogWarning("Item with name \"" + name + "\" not found in inventory.");
        return null;
    }


}


[System.Serializable]
public class Item
{
    public string itemName;
    public int width;
    public int height;
    public int originalWidth;
    public int originalHeight;

    public Texture itemTexture;
    public Color displayColor;
    public bool onlyPlaceOnDeskTop;
    public int rotation = 0;
    public string cableType = "Ethernet";
    public bool requiresMaintenanceAccess = true;
    public bool isRotatable = true;

    public Item(string name, int w, int h, Texture tex, Color color, bool onlyPlaceOnDeskTop = false)
    {
        itemName = name;
        width = originalWidth = w;
        height = originalHeight = h;
        itemTexture = tex;
        displayColor = color;
        this.onlyPlaceOnDeskTop = onlyPlaceOnDeskTop;
    }
}



[System.Serializable]
public class InventorySlot
{
    public Item item;
    public int count;

    public InventorySlot(Item newItem, int count)
    {
        item = newItem;
        this.count = count;
    }
}
