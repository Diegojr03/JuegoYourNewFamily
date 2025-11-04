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
    public GameObject characterButtonPrefab;

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
        CreateCharacterButton("Todos");
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleBacklog();
        }
    }

    // Método para DialogueSystem
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

    // Método para SimpleDialogueSystem - CADA línea individual  
    public void AddDialogueFromSimpleSystem(string speakerName, string dialogueText)
    {
        DialogueEntry entry = new DialogueEntry
        {
            speakerName = speakerName,
            dialogueText = dialogueText,
            timestamp = System.DateTime.Now.ToString("HH:mm:ss")
        };

        AddDialogueEntry(entry, speakerName);
    }

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
                CreateCharacterButton(speakerName);
            }
            dialoguesByCharacter[speakerName].Add(entry);
        }

        // También agregar a "Todos"
        dialoguesByCharacter["Todos"].Add(entry);

        // Si el backlog está abierto, actualizar la UI
        if (isBacklogOpen)
        {
            RefreshMessagesUI();
        }
    }

    private void CreateCharacterButton(string characterName)
    {
        if (charactersPanel == null || characterButtonPrefab == null) return;

        GameObject buttonObj = Instantiate(characterButtonPrefab, charactersPanel);
        Button button = buttonObj.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null)
        {
            buttonText.text = characterName;
        }

        if (button != null)
        {
            button.onClick.AddListener(() => SelectCharacter(characterName));
        }
    }

    public void SelectCharacter(string characterName)
    {
        selectedCharacter = characterName;

        if (selectedCharacterText != null)
        {
            selectedCharacterText.text = $"Conversación con: {characterName}";
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
                RefreshMessagesUI();
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
        List<DialogueEntry> messagesToShow = selectedCharacter == "Todos"
            ? allDialogueHistory
            : (dialoguesByCharacter.ContainsKey(selectedCharacter)
                ? dialoguesByCharacter[selectedCharacter]
                : new List<DialogueEntry>());

        // Crear entradas para cada mensaje (del más antiguo al más reciente)
        foreach (var entry in messagesToShow)
        {
            GameObject messageObj = Instantiate(messageEntryPrefab, messagesContent);
            SetupMessageEntry(messageObj, entry);
        }

        // Hacer scroll al final (mensajes más recientes)
        ScrollToBottom();
    }

    private void SetupMessageEntry(GameObject messageObj, DialogueEntry entry)
    {
        // Buscar los componentes
        TextMeshProUGUI speakerText = messageObj.transform.Find("SpeakerName")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI messageText = messageObj.transform.Find("ContainerText/DialogueText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI timeText = messageObj.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();

        // Configurar los textos básicos
        if (speakerText != null) speakerText.text = entry.speakerName;
        if (messageText != null) messageText.text = entry.dialogueText;
        if (timeText != null) timeText.text = entry.timestamp;

        // Usar el script de colores si existe
        MessageEntryUI messageUI = messageObj.GetComponent<MessageEntryUI>();
        if (messageUI != null)
        {
            messageUI.Setup(entry.speakerName, entry.dialogueText, entry.timestamp);
        }
        else
        {
            // Fallback: colores manuales
            if (speakerText != null)
            {
                speakerText.color = (entry.speakerName == "Lilith") ? playerColor : npcColor;
            }
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

        // Limpiar botones de personajes (excepto "Todos")
        foreach (Transform child in charactersPanel)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null && buttonText.text != "Todos")
                {
                    Destroy(child.gameObject);
                }
            }
        }

        RefreshMessagesUI();
    }
}
