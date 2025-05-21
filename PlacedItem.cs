using UnityEngine;
using UnityEngine.EventSystems;

public class PlacedItem : MonoBehaviour, IPointerClickHandler
{
    public int startX;
    public int startY;
    public int width;
    public int height;
    public Item item;
    public SchemePlacer schemePlacer;
    public GameObject inaccessibleArea; // ← добавь это поле
    public GameObject itemVisual; // UI объект самого элемента


    public void OnPointerClick(PointerEventData eventData)
    {
        // Если выбран Ластик, удаляем этот объект
        if (InventoryManager.selectedItem != null && InventoryManager.selectedItem.itemName == "Ластик")
        {
            schemePlacer.RemovePlacedItem(this);
        }
    }
}
