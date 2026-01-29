using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Referencias UI")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemCountText; // Para stacks
    public GameObject highlightFrame;

    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipName;
    public TextMeshProUGUI tooltipDescription;

    private InventorySystem.InventoryItemData itemData;
    private bool isSelected = false;

    public void Setup(InventorySystem.InventoryItemData data)
    {
        itemData = data;

        // Configurar icono
        if (itemIcon != null && data.itemIcon != null)
        {
            itemIcon.sprite = data.itemIcon;
            itemIcon.color = Color.white;
        }

        // Configurar nombre
        if (itemNameText != null)
        {
            itemNameText.text = data.itemName;
        }

        // Ocultar highlight por defecto
        if (highlightFrame != null)
            highlightFrame.SetActive(false);

        // Ocultar tooltip por defecto
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    // Mostrar información al pasar el ratón
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightFrame != null)
            highlightFrame.SetActive(true);

        if (tooltipPanel != null && itemData != null)
        {
            tooltipPanel.SetActive(true);
            if (tooltipName != null) tooltipName.text = itemData.itemName;
            if (tooltipDescription != null && !string.IsNullOrEmpty(itemData.description))
                tooltipDescription.text = itemData.description;
        }
    }

    // Ocultar información al quitar el ratón
    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightFrame != null && !isSelected)
            highlightFrame.SetActive(false);

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    // Al hacer click en el item
    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemData == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Usar el item
            UseItem();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Mostrar información detallada o menú contextual
            ShowItemDetails();
        }
    }

    private void UseItem()
    {
        Debug.Log($"Intentando usar item: {itemData.itemName}");

        // Intentar usar el componente InventoryItem si existe el prefab
        if (itemData.itemPrefab != null)
        {
            InventoryItem itemComponent = itemData.itemPrefab.GetComponent<InventoryItem>();
            if (itemComponent != null && itemComponent.isUsable)
            {
                itemComponent.UseItem();
            }
        }

        // También puedes añadir lógica específica según el itemId
        switch (itemData.itemId)
        {
            case "nieve_negra":
                Debug.Log("Usando nieve negra...");
                // Lógica específica para nieve negra
                break;
            case "pocion_salud":
                Debug.Log("Usando poción de salud...");
                // Lógica específica para poción
                break;
        }
    }

    private void ShowItemDetails()
    {
        Debug.Log($"Mostrando detalles de: {itemData.itemName}");
        // Aquí puedes abrir un panel más grande con información detallada
    }

    // Para selección con teclado/gamepad
    public void SelectItem()
    {
        isSelected = true;
        if (highlightFrame != null)
            highlightFrame.SetActive(true);
    }

    public void DeselectItem()
    {
        isSelected = false;
        if (highlightFrame != null)
            highlightFrame.SetActive(false);
    }
}