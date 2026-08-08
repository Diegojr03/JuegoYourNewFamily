
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
    public List<Button> characterButtons;

    [Header("Panel de Mensajes (RIGHT)")]
    public Transform messagesContent;
    public GameObject messageEntryPrefab;
    public TextMeshProUGUI selectedCharacterText;

    [Header("Configuración")]
    public int maxEntries = 100;
    public Color playerColor = Color.cyan;
    public Color npcColor = Color.white;

    // =========================================================
    // DATOS DEL BACKLOG
    // =========================================================

    private List<DialogueEntry> allDialogueHistory =
        new List<DialogueEntry>();

    private Dictionary<string, List<DialogueEntry>> dialoguesByCharacter =
        new Dictionary<string, List<DialogueEntry>>();

    private string selectedCharacter = "Todos";
    private bool isBacklogOpen = false;

    // =========================================================
    // CLASE DE DIALOGO
    // =========================================================

    [System.Serializable]
    public class DialogueEntry
    {
        public string speakerName;
        public string dialogueText;
        public string timestamp;
    }

    // =========================================================
    // SINGLETON
    // =========================================================

    public static BacklogManager Instance { get; private set; }

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // PRIMER BACKLOG MANAGER
        // -----------------------------------------------------

        if (Instance == null)
        {
            Instance = this;

            // Este BacklogManager será el único que sobreviva
            DontDestroyOnLoad(gameObject);

            Debug.Log(
                "[BacklogManager] Instancia persistente creada: "
                + gameObject.name
            );
        }
        else if (Instance != this)
        {
            // -------------------------------------------------
            // BACKLOG MANAGER DE UNA NUEVA ESCENA
            // -------------------------------------------------

            Debug.Log(
                "[BacklogManager] Nueva escena detectada. "
                + "Actualizando referencias de UI..."
            );

            // El Manager persistente recibe las referencias
            // del Canvas de la nueva escena.
            Instance.UpdateUIReferences(this);

            // Este Manager NO debe sobrevivir.
            Destroy(gameObject);
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // Solo inicializamos datos en la instancia persistente.
        if (Instance != this)
            return;

        // Crear "Todos" si todavía no existe.
        if (!dialoguesByCharacter.ContainsKey("Todos"))
        {
            dialoguesByCharacter["Todos"] =
                new List<DialogueEntry>();
        }

        // Detectar y configurar botones.
        RefreshCharacterButtons();
        SetupCharacterButtons();

        // Debug
        DebugButtons();

        // Configurar raycast.
        FixRaycastOrder();

        // Ocultar backlog al iniciar.
        if (backlogPanel != null)
        {
            backlogPanel.SetActive(false);
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Seguridad.
        if (Instance != this)
            return;

        if (Input.GetKeyDown(toggleKey))
        {
            ToggleBacklog();
        }
    }

    // =========================================================
    // DETECTAR BOTONES
    // =========================================================

    public void RefreshCharacterButtons()
    {
        if (charactersPanel == null)
        {
            Debug.LogError(
                "[BacklogManager] charactersPanel es NULL."
            );

            return;
        }

        Button[] foundButtons =
            charactersPanel.GetComponentsInChildren<Button>(true);

        if (characterButtons == null)
        {
            characterButtons = new List<Button>();
        }

        characterButtons.Clear();
        characterButtons.AddRange(foundButtons);

        Debug.Log(
            "[BacklogManager] Se encontraron "
            + foundButtons.Length
            + " botones en "
            + charactersPanel.name
        );

        foreach (Button button in foundButtons)
        {
            if (button != null)
            {
                Debug.Log(
                    "[BacklogManager] Botón encontrado: "
                    + button.name
                );
            }
        }
    }

    // =========================================================
    // CONFIGURAR BOTONES
    // =========================================================

    private void SetupCharacterButtons()
    {
        if (characterButtons == null ||
            characterButtons.Count == 0)
        {
            Debug.LogWarning(
                "[BacklogManager] No hay botones de personajes."
            );

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

    // =========================================================
    // CONFIGURAR UN BOTÓN
    // =========================================================

    private void ConfigureCharacterButton(Button button)
    {
        if (button == null)
            return;

        // El nombre del GameObject del botón es el nombre del personaje
        string characterName = button.gameObject.name.Trim();

        if (string.IsNullOrEmpty(characterName))
        {
            Debug.LogWarning(
                "[BacklogManager] Un botón tiene el nombre vacío."
            );

            return;
        }

        // Eliminar listeners anteriores para evitar duplicados
        button.onClick.RemoveAllListeners();

        // Asegurarnos de que está activo
        button.interactable = true;

        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            image.raycastTarget = true;
        }

        // Crear la lista del personaje si todavía no existe
        if (characterName != "Todos" &&
            !dialoguesByCharacter.ContainsKey(characterName))
        {
            dialoguesByCharacter[characterName] =
                new List<DialogueEntry>();
        }

        // =====================================================
        // CREAR LISTENER
        // =====================================================

        button.onClick.AddListener(() =>
        {
            Debug.Log(
                "========== BOTÓN PULSADO =========="
            );

            Debug.Log(
                "Botón: " + button.gameObject.name
            );

            Debug.Log(
                "Personaje: " + characterName
            );

            if (BacklogManager.Instance == null)
            {
                Debug.LogError(
                    "[BacklogManager] Instance es NULL."
                );

                return;
            }

            Debug.Log(
                "BacklogManager.Instance encontrado: " +
                BacklogManager.Instance.gameObject.name
            );

            BacklogManager.Instance.SelectCharacter(
                characterName
            );
        });

        Debug.Log(
            "[BacklogManager] Listener configurado para: "
            + characterName
        );
    }

    // =========================================================
    // RAYCAST
    // =========================================================

    private void FixRaycastOrder()
    {
        if (backlogPanel == null)
            return;

        Canvas canvas =
            backlogPanel.GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10;
        }
    }

    // =========================================================
    // AÑADIR BOTÓN DINÁMICAMENTE
    // =========================================================

    public void AddCharacterButton(
        Button newButton,
        string characterName)
    {
        if (newButton == null)
            return;

        if (characterButtons == null)
        {
            characterButtons = new List<Button>();
        }

        if (!characterButtons.Contains(newButton))
        {
            characterButtons.Add(newButton);
        }

        TextMeshProUGUI buttonText =
            newButton.GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null &&
            string.IsNullOrEmpty(buttonText.text))
        {
            buttonText.text = characterName;
        }

        ConfigureCharacterButton(newButton);

        Debug.Log(
            "[BacklogManager] Botón añadido: "
            + characterName
        );
    }

    // =========================================================
    // AÑADIR DIÁLOGO DESDE DIALOGUESYSTEM
    // =========================================================

    public void AddDialogueFromDialogueSystem(
        string speakerName,
        string dialogueText,
        bool leftSpeaker)
    {
        DialogueEntry entry = new DialogueEntry
        {
            speakerName = speakerName,
            dialogueText = dialogueText,
            timestamp =
                System.DateTime.Now.ToString("HH:mm:ss")
        };

        AddDialogueEntry(entry, speakerName);
    }

    // =========================================================
    // AÑADIR DIÁLOGO CON DUEÑO DE CONVERSACIÓN
    // =========================================================

    public void AddDialogueWithConversationOwner(
        string speakerName,
        string dialogueText,
        string conversationOwner)
    {
        DialogueEntry entry = new DialogueEntry
        {
            speakerName = speakerName,
            dialogueText = dialogueText,
            timestamp =
                System.DateTime.Now.ToString("HH:mm:ss")
        };

        AddDialogueEntry(
            entry,
            conversationOwner
        );
    }

    // =========================================================
    // AÑADIR ENTRADA
    // =========================================================

    private void AddDialogueEntry(
        DialogueEntry entry,
        string speakerName)
    {
        allDialogueHistory.Add(entry);

        // Limitar historial.
        if (allDialogueHistory.Count > maxEntries)
        {
            allDialogueHistory.RemoveAt(0);
        }

        // Añadir al personaje.
        if (!string.IsNullOrEmpty(speakerName))
        {
            if (!dialoguesByCharacter.ContainsKey(speakerName))
            {
                dialoguesByCharacter[speakerName] =
                    new List<DialogueEntry>();
            }

            dialoguesByCharacter[speakerName].Add(entry);
        }

        // Añadir también a "Todos".
        if (!dialoguesByCharacter.ContainsKey("Todos"))
        {
            dialoguesByCharacter["Todos"] =
                new List<DialogueEntry>();
        }

        dialoguesByCharacter["Todos"].Add(entry);

        // Si está abierto, actualizar.
        if (isBacklogOpen)
        {
            RefreshMessagesUI();
        }
    }

    // =========================================================
    // SELECCIONAR PERSONAJE
    // =========================================================

    public void SelectCharacter(string characterName)
    {
        if (Instance != this)
        {
            Debug.LogWarning(
                "[BacklogManager] Se intentó usar un BacklogManager " +
                "que no es la instancia persistente."
            );

            return;
        }

        Debug.Log(
            "[BacklogManager] SelectCharacter llamado con: " +
            characterName
        );

        selectedCharacter = characterName;

        if (selectedCharacterText != null)
        {
            selectedCharacterText.text =
                "Conversación con: " + characterName;
        }
        else
        {
            Debug.LogError(
                "[BacklogManager] selectedCharacterText es NULL."
            );
        }

        RefreshMessagesUI();
    }

    // =========================================================
    // ABRIR / CERRAR BACKLOG
    // =========================================================

    public void ToggleBacklog()
    {
        if (Instance != this)
            return;

        isBacklogOpen = !isBacklogOpen;

        if (backlogPanel == null)
        {
            Debug.LogError("[BacklogManager] backlogPanel es NULL.");
            return;
        }

        backlogPanel.SetActive(isBacklogOpen);

        if (isBacklogOpen)
        {
            // NO volver a configurar los botones aquí.
            // Ya se configuraron al entrar en la escena.

            // Selección inicial
            SelectCharacter("Lilith");

            Time.timeScale = 0f;

            Debug.Log("[BacklogManager] Backlog abierto.");
        }
        else
        {
            Time.timeScale = 1f;

            Debug.Log("[BacklogManager] Backlog cerrado.");
        }
    }

    // =========================================================
    // ACTUALIZAR UI DE MENSAJES
    // =========================================================

    private void RefreshMessagesUI()
    {
        if (messagesContent == null)
        {
            Debug.LogError(
                "[BacklogManager] messagesContent es NULL."
            );

            return;
        }

        if (messageEntryPrefab == null)
        {
            Debug.LogError(
                "[BacklogManager] messageEntryPrefab es NULL."
            );

            return;
        }

        // -----------------------------------------------------
        // BORRAR MENSAJES ANTERIORES
        // -----------------------------------------------------

        foreach (Transform child in messagesContent)
        {
            Destroy(child.gameObject);
        }

        // -----------------------------------------------------
        // OBTENER MENSAJES
        // -----------------------------------------------------

        List<DialogueEntry> messagesToShow =
            GetFilteredMessages(selectedCharacter);

        // -----------------------------------------------------
        // CREAR MENSAJES
        // -----------------------------------------------------

        foreach (DialogueEntry entry in messagesToShow)
        {
            GameObject messageObj =
                Instantiate(
                    messageEntryPrefab,
                    messagesContent
                );

            SetupMessageEntry(
                messageObj,
                entry
            );
        }

        // Scroll.
        ScrollToBottom();
    }

    // =========================================================
    // FILTRAR MENSAJES
    // =========================================================

    private List<DialogueEntry> GetFilteredMessages(
        string characterName)
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

    // =========================================================
    // CONFIGURAR ENTRADA DE MENSAJE
    // =========================================================

    private void SetupMessageEntry(
        GameObject messageObj,
        DialogueEntry entry)
    {
        MessageEntryUI messageUI =
            messageObj.GetComponent<MessageEntryUI>();

        if (messageUI != null)
        {
            messageUI.Setup(
                entry.speakerName,
                entry.dialogueText,
                entry.timestamp
            );
        }
        else
        {
            TextMeshProUGUI speakerText =
                messageObj.transform
                    .Find("SpeakerName")
                    ?.GetComponent<TextMeshProUGUI>();

            TextMeshProUGUI messageText =
                messageObj.transform
                    .Find("ContainerText/DialogueText")
                    ?.GetComponent<TextMeshProUGUI>();

            TextMeshProUGUI timeText =
                messageObj.transform
                    .Find("TimeText")
                    ?.GetComponent<TextMeshProUGUI>();

            if (speakerText != null)
            {
                speakerText.text =
                    entry.speakerName;

                speakerText.color =
                    entry.speakerName == "Lilith"
                        ? playerColor
                        : npcColor;
            }

            if (messageText != null)
            {
                messageText.text =
                    entry.dialogueText;
            }

            if (timeText != null)
            {
                timeText.text =
                    entry.timestamp;
            }
        }
    }

    // =========================================================
    // SCROLL
    // =========================================================

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();

        if (messagesContent == null)
            return;

        ScrollRect scrollRect =
            messagesContent.parent
                .GetComponent<ScrollRect>();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // =========================================================
    // LIMPIAR BACKLOG
    // =========================================================

    public void ClearBacklog()
    {
        allDialogueHistory.Clear();

        dialoguesByCharacter.Clear();

        dialoguesByCharacter["Todos"] =
            new List<DialogueEntry>();

        RefreshMessagesUI();
    }

    // =========================================================
    // DEBUG BOTONES
    // =========================================================

    private void DebugButtons()
    {
        Debug.Log(
            "=== BACKLOG DEBUG: BOTONES ==="
        );

        if (characterButtons == null)
        {
            Debug.LogError(
                "[BacklogManager] characterButtons es NULL."
            );

            return;
        }

        Debug.Log(
            "Total botones: "
            + characterButtons.Count
        );

        foreach (Button button in characterButtons)
        {
            if (button == null)
            {
                Debug.LogWarning(
                    "Botón NULL encontrado."
                );

                continue;
            }

            TextMeshProUGUI text =
                button.GetComponentInChildren<TextMeshProUGUI>();

            string buttonName =
                text != null
                    ? text.text
                    : "SIN TEXTO";

            Image image =
                button.GetComponent<Image>();

            Debug.Log(
                "Botón: "
                + buttonName
                + " | Interactable: "
                + button.interactable
                + " | Raycast: "
                + (
                    image != null
                        ? image.raycastTarget.ToString()
                        : "NO IMAGE"
                  )
                + " | PersistentListeners: "
                + button.onClick.GetPersistentEventCount()
            );
        }

        Debug.Log(
            "=== FIN BACKLOG DEBUG ==="
        );
    }

    // =========================================================
    // ACTUALIZAR BOTONES MANUALMENTE
    // =========================================================

    [ContextMenu("Forzar Actualización de Botones")]
    public void ForceRefreshButtons()
    {
        RefreshCharacterButtons();
        SetupCharacterButtons();
        DebugButtons();

        Debug.Log(
            "[BacklogManager] Actualización forzada completada."
        );
    }

    // =========================================================
    // CAMBIO DE ESCENA
    // =========================================================

    public void UpdateUIReferences(
        BacklogManager newSceneManager)
    {
        if (newSceneManager == null)
        {
            Debug.LogError(
                "[BacklogManager] newSceneManager es NULL."
            );

            return;
        }

        Debug.Log(
            "[BacklogManager] Actualizando referencias "
            + "con la UI de la nueva escena."
        );

        // -----------------------------------------------------
        // REFERENCIAS DEL NUEVO CANVAS
        // -----------------------------------------------------

        backlogPanel =
            newSceneManager.backlogPanel;

        charactersPanel =
            newSceneManager.charactersPanel;

        messagesContent =
            newSceneManager.messagesContent;

        messageEntryPrefab =
            newSceneManager.messageEntryPrefab;

        selectedCharacterText =
            newSceneManager.selectedCharacterText;

        // -----------------------------------------------------
        // CERRAR BACKLOG
        // -----------------------------------------------------

        isBacklogOpen = false;

        if (backlogPanel != null)
        {
            backlogPanel.SetActive(false);
        }

        // -----------------------------------------------------
        // MUY IMPORTANTE:
        // DETECTAR LOS BOTONES DEL NUEVO CANVAS
        // -----------------------------------------------------

        RefreshCharacterButtons();

        // -----------------------------------------------------
        // CREAR LOS LISTENERS SOBRE LOS NUEVOS BOTONES
        // -----------------------------------------------------

        SetupCharacterButtons();

        // -----------------------------------------------------
        // RAYCAST
        // -----------------------------------------------------

        FixRaycastOrder();

        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        DebugButtons();

        Debug.Log(
            "[BacklogManager] Referencias de UI actualizadas "
            + "correctamente."
        );
    }
}

