using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("UI de la Mochila")]
    public GameObject backpackIcon; // Icono pequeño en la esquina
    public GameObject backpackPanel; // Panel desplegable completo
    public Transform itemsContainer; // Vertical Layout Group donde van los items
    public KeyCode toggleKey = KeyCode.M;
    public int maxSlots = 3; // Máximo de huecos en la mochila

    [Header("Prefabs")]
    public GameObject inventoryItemPrefab; // Prefab para mostrar items en la mochila

    [Header("Configuración")]
    public bool autoOpenOnItemGet = true; // Abrir automáticamente al obtener un item
    public float autoCloseDelay = 3f; // Tiempo antes de cerrar automáticamente

    private List<InventoryItemData> inventoryItems = new List<InventoryItemData>();
    private bool isBackpackOpen = false;
    private Coroutine autoCloseCoroutine;

    // Clase para los datos del item
    [System.Serializable]
    public class InventoryItemData
    {
        public string itemId; // Identificador único
        public string itemName; // Nombre para mostrar
        public Sprite itemIcon; // Icono del item
        public GameObject itemPrefab; // Prefab del item (si es necesario)
        public string description; // Descripción opcional
        public bool isUsable = true; // Si se puede usar
    }

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Inicializar UI
        if (backpackPanel != null)
            backpackPanel.SetActive(false);

        if (backpackIcon != null)
            backpackIcon.SetActive(true);
    }

    void Update()
    {
        // Abrir/cerrar con tecla M
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleBackpack();
        }

        // Abrir/cerrar haciendo click en el icono
        if (Input.GetMouseButtonDown(0) && backpackIcon != null)
        {
            // Verificar si se hizo click en el icono
            if (RectTransformUtility.RectangleContainsScreenPoint(
                backpackIcon.GetComponent<RectTransform>(),
                Input.mousePosition))
            {
                ToggleBackpack();
            }
        }
    }

    // Método principal para añadir items
    public bool AddItem(InventoryItemData newItem)
    {
        // Verificar si ya tenemos el máximo de items
        if (inventoryItems.Count >= maxSlots)
        {
            Debug.LogWarning("La mochila está llena!");
            return false;
        }

        // Verificar si ya existe el item (opcional, depende si quieres stacks)
        if (inventoryItems.Exists(item => item.itemId == newItem.itemId))
        {
            Debug.Log($"Ya tienes {newItem.itemName} en la mochila");
            return false;
        }

        // Añadir a la lista
        inventoryItems.Add(newItem);
        Debug.Log($"Añadido a la mochila: {newItem.itemName}");

        // Actualizar UI
        UpdateInventoryUI();

        // Mostrar notificación (opcional)
        ShowItemNotification(newItem.itemName);

        // Abrir automáticamente si está configurado
        if (autoOpenOnItemGet && !isBackpackOpen)
        {
            OpenBackpack();
            StartAutoClose();
        }

        return true;
    }

    // Método simplificado para añadir items desde diálogos
    public bool AddSimpleItem(string itemId, string itemName, Sprite itemIcon)
    {
        InventoryItemData newItem = new InventoryItemData
        {
            itemId = itemId,
            itemName = itemName,
            itemIcon = itemIcon
        };

        return AddItem(newItem);
    }

    // Método para añadir prefab desde diálogos (como solicitaste)
    public bool AddItemFromPrefab(GameObject itemPrefab)
    {
        if (itemPrefab == null)
        {
            Debug.LogError("El prefab del item es nulo!");
            return false;
        }

        // Obtener componente InventoryItem del prefab (opcional, lo crearemos después)
        InventoryItem itemComponent = itemPrefab.GetComponent<InventoryItem>();

        InventoryItemData newItem = new InventoryItemData
        {
            itemId = itemPrefab.name,
            itemName = itemComponent != null ? itemComponent.itemName : itemPrefab.name,
            itemIcon = itemComponent != null ? itemComponent.itemIcon : null,
            itemPrefab = itemPrefab,
            description = itemComponent != null ? itemComponent.description : ""
        };

        return AddItem(newItem);
    }

    // Actualizar la UI del inventario
    private void UpdateInventoryUI()
    {
        // Limpiar contenedor
        foreach (Transform child in itemsContainer)
        {
            Destroy(child.gameObject);
        }

        // Crear slots para cada item
        foreach (var itemData in inventoryItems)
        {
            GameObject itemUI = Instantiate(inventoryItemPrefab, itemsContainer);

            // Configurar UI del item
            InventoryItemUI itemUIComponent = itemUI.GetComponent<InventoryItemUI>();
            if (itemUIComponent != null)
            {
                itemUIComponent.Setup(itemData);
            }
            else
            {
                // Configuración manual si no hay componente
                Image iconImage = itemUI.transform.Find("Icon")?.GetComponent<Image>();
                if (iconImage != null && itemData.itemIcon != null)
                {
                    iconImage.sprite = itemData.itemIcon;
                }
            }
        }

        // Crear slots vacíos si no están llenos
        for (int i = inventoryItems.Count; i < maxSlots; i++)
        {
            GameObject emptySlot = Instantiate(inventoryItemPrefab, itemsContainer);

            // Marcar como slot vacío
            Image iconImage = emptySlot.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.color = new Color(1, 1, 1, 0.2f); // Transparente
                iconImage.sprite = null;
            }

            // Opcional: añadir texto "Vacío"
            TMPro.TextMeshProUGUI text = emptySlot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null)
            {
                text.text = "Vacío";
                text.color = Color.gray;
            }
        }
    }

    // Mostrar notificación de item obtenido
    private void ShowItemNotification(string itemName)
    {
        // Aquí puedes implementar un sistema de notificaciones en pantalla
        Debug.Log($"¡Has obtenido: {itemName}!");

        // Opcional: mostrar texto en pantalla (necesitarías un UI Text en el Canvas)
        // StartCoroutine(ShowNotificationText($"¡Has obtenido: {itemName}!"));
    }

    // Control de apertura/cierre
    public void ToggleBackpack()
    {
        if (isBackpackOpen)
            CloseBackpack();
        else
            OpenBackpack();
    }

    public void OpenBackpack()
    {
        if (backpackPanel != null)
        {
            backpackPanel.SetActive(true);
            isBackpackOpen = true;

            // Actualizar UI al abrir
            UpdateInventoryUI();

            // Cancelar cierre automático si está abierto manualmente
            if (autoCloseCoroutine != null)
            {
                StopCoroutine(autoCloseCoroutine);
                autoCloseCoroutine = null;
            }
        }
    }

    public void CloseBackpack()
    {
        if (backpackPanel != null)
        {
            backpackPanel.SetActive(false);
            isBackpackOpen = false;
        }
    }

    // Cierre automático después de tiempo
    private void StartAutoClose()
    {
        if (autoCloseCoroutine != null)
            StopCoroutine(autoCloseCoroutine);

        autoCloseCoroutine = StartCoroutine(AutoClose());
    }

    private System.Collections.IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        if (isBackpackOpen)
        {
            CloseBackpack();
        }
    }

    // Verificar si hay espacio en la mochila
    public bool HasSpace()
    {
        return inventoryItems.Count < maxSlots;
    }

    // Verificar si tiene un item específico
    public bool HasItem(string itemId)
    {
        return inventoryItems.Exists(item => item.itemId == itemId);
    }

    // Obtener item por ID
    public InventoryItemData GetItem(string itemId)
    {
        return inventoryItems.Find(item => item.itemId == itemId);
    }

    // Quitar item del inventario
    public bool RemoveItem(string itemId)
    {
        InventoryItemData item = GetItem(itemId);
        if (item != null)
        {
            inventoryItems.Remove(item);
            UpdateInventoryUI();
            return true;
        }
        return false;
    }

    // Limpiar inventario completo
    public void ClearInventory()
    {
        inventoryItems.Clear();
        UpdateInventoryUI();
    }
}

