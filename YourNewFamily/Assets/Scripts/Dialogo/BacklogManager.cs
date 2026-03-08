using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BacklogManager : MonoBehaviour
{
    [Header("UI del Backlog")]
    public GameObject backlogPanel;
    public KeyCode toggleKey = KeyCode.B;

    [Header("Panel de Personajes (LEFT)")]
    public Transform charactersPanel;
    public List<Button> characterButtons; // Cambiado a List para mejor manejo

    [Header("Panel de Mensajes (RIGHT)")]
    public Transform messagesContent;
    public GameObject messageEntryPrefab;
    public TextMeshProUGUI selectedCharacterText;

    [Header("Configuración")]
    public int maxEntries = 100;
    public Color playerColor = Color.cyan;
    public Color npcColor = Color.white;

    private List<DialogueEntry> allDialogueHistory = new List<DialogueEntry>();
    private Dictionary<string, List<DialogueEntry>> dialoguesByCharacter = new Dictionary<string, List<DialogueEntry>>();
    private string selectedCharacter = "Todos";
    private bool isBacklogOpen = false;

    [System.Serializable]
    public class DialogueEntry
    {
        public string speakerName;
        public string dialogueText;
        public string timestamp;
    }

    // Singleton
    public static BacklogManager Instance { get; private set; }

    void Awake()
    {
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
        if (backlogPanel != null)
        {
            backlogPanel.SetActive(false);
        }

        // Inicializar con la opción "Todos"
        dialoguesByCharacter["Todos"] = new List<DialogueEntry>();

        // IMPORTANTE: Forzar la búsqueda de todos los botones en el panel
        RefreshCharacterButtons();

        // Configurar los botones
        SetupCharacterButtons();

        // Debug para verificar que todos los botones se encontraron
        DebugButtons();

        FixRaycastOrder();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleBacklog();
        }
    }

    // NUEVO: Método para refrescar la lista de botones desde el panel
    public void RefreshCharacterButtons()
    {
        if (charactersPanel != null)
        {
            // Obtener TODOS los botones hijos (incluyendo los no activos)
            Button[] foundButtons = charactersPanel.GetComponentsInChildren<Button>(true);

            // Inicializar la lista si es null
            if (characterButtons == null)
                characterButtons = new List<Button>();

            // Limpiar la lista actual
            characterButtons.Clear();

            // Añadir todos los botones encontrados
            characterButtons.AddRange(foundButtons);

            Debug.Log($"Se encontraron {foundButtons.Length} botones en el panel de personajes");
        }
    }

    private void SetupCharacterButtons()
    {
        if (characterButtons == null || characterButtons.Count == 0)
        {
            Debug.LogWarning("No hay botones de personajes configurados");
            return;
        }

        foreach (Button button in characterButtons)
        {
            if (button != null)
            {
                ConfigureCharacterButton(button);
            }
        }
    }

    private void ConfigureCharacterButton(Button button)
    {
        if (button == null) return;

        // IMPORTANTE: Asegurar que el botón es interactuable
        button.interactable = true;

        // Asegurar que la imagen tiene Raycast Target activado
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.raycastTarget = true;
        }

        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            string characterName = buttonText.text.Trim(); // Limpiar espacios

            // Limpiar listeners previos para evitar duplicados
            button.onClick.RemoveAllListeners();

            // Añadir el nuevo listener
            button.onClick.AddListener(() => SelectCharacter(characterName));

            // Inicializar la lista para este personaje si no existe
            if (!dialoguesByCharacter.ContainsKey(characterName) && characterName != "Todos")
            {
                dialoguesByCharacter[characterName] = new List<DialogueEntry>();
                Debug.Log($"Lista inicializada para personaje: {characterName}");
            }
        }
        else
        {
            Debug.LogWarning($"Botón {button.name} no tiene texto asignado");
        }
    }

    private void FixRaycastOrder()
    {
        // Asegurar que los botones están por encima en el orden de raycast
        Canvas canvas = backlogPanel.GetComponent<Canvas>();
        if (canvas != null)
        {
            // Esto fuerza que los hijos del panel tengan prioridad
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10; // Un número alto
        }

        // Si usas paneles con imágenes, asegúrate que los botones están físicamente
        // encima en la jerarquía o ajusta sus posiciones Z
    }


    // Método para añadir nuevos botones dinámicamente en tiempo de ejecución
    public void AddCharacterButton(Button newButton, string characterName)
    {
        if (newButton == null) return;

        // Añadir a la lista
        if (characterButtons == null)
            characterButtons = new List<Button>();

        if (!characterButtons.Contains(newButton))
        {
            characterButtons.Add(newButton);

            // Asignar nombre si el texto está vacío
            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null && string.IsNullOrEmpty(buttonText.text))
            {
                buttonText.text = characterName;
            }

            ConfigureCharacterButton(newButton);
            Debug.Log($"Botón añadido dinámicamente para: {characterName}");
        }
    }

    // Método para añadir diálogo desde DialogueSystem
    public void AddDialogueFromDialogueSystem(string speakerName, string dialogueText, bool leftSpeaker)
    {
        DialogueEntry entry = new DialogueEntry
        {
            speakerName = speakerName,
            dialogueText = dialogueText,
            timestamp = System.DateTime.Now.ToString("HH:mm:ss")
        };

        AddDialogueEntry(entry, speakerName);
    }

    // Método para añadir diálogo con dueño de conversación
    public void AddDialogueWithConversationOwner(string speakerName, string dialogueText, string conversationOwner)
    {
        DialogueEntry entry = new DialogueEntry
        {
            speakerName = speakerName,
            dialogueText = dialogueText,
            timestamp = System.DateTime.Now.ToString("HH:mm:ss")
        };

        AddDialogueEntry(entry, conversationOwner);
    }

    // Método común para agregar entradas
    private void AddDialogueEntry(DialogueEntry entry, string speakerName)
    {
        // Agregar al historial general
        allDialogueHistory.Add(entry);

        // Limitar tamaño del historial
        if (allDialogueHistory.Count > maxEntries)
        {
            allDialogueHistory.RemoveAt(0);
        }

        // Agregar al diccionario por personaje
        if (!string.IsNullOrEmpty(speakerName))
        {
            if (!dialoguesByCharacter.ContainsKey(speakerName))
            {
                dialoguesByCharacter[speakerName] = new List<DialogueEntry>();
            }
            dialoguesByCharacter[speakerName].Add(entry);
        }

        // También agregar a "Todos"
        if (dialoguesByCharacter.ContainsKey("Todos"))
        {
            dialoguesByCharacter["Todos"].Add(entry);
        }

        // Si el backlog está abierto, actualizar la UI
        if (isBacklogOpen)
        {
            RefreshMessagesUI();
        }
    }

    public void SelectCharacter(string characterName)
    {
        selectedCharacter = characterName;

        // ACTUALIZAR EL TEXTO
        if (selectedCharacterText != null)
        {
            selectedCharacterText.text = $"Conversación con: {characterName}";
            Debug.Log($"Cambiando a personaje: {characterName}");
        }

        RefreshMessagesUI();
    }

    public void ToggleBacklog()
    {
        isBacklogOpen = !isBacklogOpen;

        if (backlogPanel != null)
        {
            backlogPanel.SetActive(isBacklogOpen);

            if (isBacklogOpen)
            {
                // Antes de seleccionar personaje, refrescar botones
                RefreshCharacterButtons();
                SetupCharacterButtons();

                SelectCharacter("Lilith");
                Time.timeScale = 0f; // Pausar el juego
            }
            else
            {
                Time.timeScale = 1f; // Reanudar el juego
            }
        }
    }

    private void RefreshMessagesUI()
    {
        // Limpiar contenido anterior de mensajes
        foreach (Transform child in messagesContent)
        {
            Destroy(child.gameObject);
        }

        // Obtener mensajes según el personaje seleccionado
        List<DialogueEntry> messagesToShow = GetFilteredMessages(selectedCharacter);

        // Crear entradas para cada mensaje
        foreach (var entry in messagesToShow)
        {
            GameObject messageObj = Instantiate(messageEntryPrefab, messagesContent);
            SetupMessageEntry(messageObj, entry);
        }

        // Hacer scroll al final
        ScrollToBottom();
    }

    private List<DialogueEntry> GetFilteredMessages(string characterName)
    {
        if (characterName == "Todos")
        {
            return allDialogueHistory;
        }

        if (dialoguesByCharacter.ContainsKey(characterName))
        {
            return dialoguesByCharacter[characterName];
        }

        return new List<DialogueEntry>();
    }

    private void SetupMessageEntry(GameObject messageObj, DialogueEntry entry)
    {
        // Intentar usar el script MessageEntryUI primero
        MessageEntryUI messageUI = messageObj.GetComponent<MessageEntryUI>();
        if (messageUI != null)
        {
            messageUI.Setup(entry.speakerName, entry.dialogueText, entry.timestamp);
        }
        else
        {
            // Fallback: configuración manual
            TextMeshProUGUI speakerText = messageObj.transform.Find("SpeakerName")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI messageText = messageObj.transform.Find("ContainerText/DialogueText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI timeText = messageObj.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();

            if (speakerText != null)
            {
                speakerText.text = entry.speakerName;
                speakerText.color = (entry.speakerName == "Lilith") ? playerColor : npcColor;
            }
            if (messageText != null) messageText.text = entry.dialogueText;
            if (timeText != null) timeText.text = entry.timestamp;
        }
    }

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        ScrollRect scrollRect = messagesContent.parent.GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ClearBacklog()
    {
        allDialogueHistory.Clear();
        dialoguesByCharacter.Clear();
        dialoguesByCharacter["Todos"] = new List<DialogueEntry>();

        RefreshMessagesUI();
    }

    // NUEVO: Método de debug para verificar botones
    private void DebugButtons()
    {
        Debug.Log("=== DEBUG DE BOTONES DE PERSONAJES ===");

        if (characterButtons == null)
        {
            Debug.LogError("characterButtons es null");
            return;
        }

        Debug.Log($"Total botones en lista: {characterButtons.Count}");

        foreach (Button btn in characterButtons)
        {
            if (btn == null)
            {
                Debug.LogWarning("Botón null encontrado en la lista");
                continue;
            }

            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            string buttonName = txt != null ? txt.text : "SIN TEXTO";
            Image img = btn.GetComponent<Image>();

            Debug.Log($"Botón: {buttonName} | " +
                     $"Interactable: {btn.interactable} | " +
                     $"RaycastTarget: {(img != null ? img.raycastTarget.ToString() : "NO IMAGE")} | " +
                     $"Listeners: {btn.onClick.GetPersistentEventCount()}");
        }

        Debug.Log("=== FIN DEBUG ===");
    }

    // NUEVO: Método para forzar la actualización desde el Inspector
    [ContextMenu("Forzar Actualización de Botones")]
    public void ForceRefreshButtons()
    {
        RefreshCharacterButtons();
        SetupCharacterButtons();
        DebugButtons();
        Debug.Log("Actualización de botones forzada completada");
    }
}