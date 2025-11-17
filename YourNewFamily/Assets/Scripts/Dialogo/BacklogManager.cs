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
    public Button[] characterButtons; // Array de botones existentes

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

        // Configurar los botones existentes
        SetupCharacterButtons();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleBacklog();
        }
    }

    private void SetupCharacterButtons()
    {
        if (characterButtons == null || characterButtons.Length == 0) return;

        foreach (Button button in characterButtons)
        {
            if (button != null)
            {
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    string characterName = buttonText.text;

                    // Configurar el evento click
                    button.onClick.RemoveAllListeners(); // Limpiar listeners previos
                    button.onClick.AddListener(() => SelectCharacter(characterName));

                    // Inicializar la lista para este personaje si no existe
                    if (!dialoguesByCharacter.ContainsKey(characterName))
                    {
                        dialoguesByCharacter[characterName] = new List<DialogueEntry>();
                    }
                }
            }
        }
    }


    // Método para añadir nuevos botones dinámicamente
    public void AddCharacterButton(Button newButton, string characterName)
    {
        // Agregar el botón al array (necesitarías redimensionar el array desde el Inspector)
        // O usar una lista en su lugar (te recomiendo cambiar a List<Button>)

        // Configurar el botón
        newButton.onClick.RemoveAllListeners();
        newButton.onClick.AddListener(() => SelectCharacter(characterName));

        // Inicializar la lista para el nuevo personaje
        if (!dialoguesByCharacter.ContainsKey(characterName))
        {
            dialoguesByCharacter[characterName] = new List<DialogueEntry>();
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

    // Método para SimpleDialogueSystem
    /*public void AddDialogueFromSimpleSystem(string speakerName, string dialogueText)
    {
        DialogueEntry entry = new DialogueEntry
        {
            speakerName = speakerName,
            dialogueText = dialogueText,
            timestamp = System.DateTime.Now.ToString("HH:mm:ss")
        };

        AddDialogueEntry(entry, speakerName);
    }*/



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
                // No creamos botón automáticamente, se añaden desde el Inspector
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

    public void SelectCharacter(string characterName)
    {
        selectedCharacter = characterName;

        // ACTUALIZAR EL TEXTO - asegúrate de que esta línea se ejecuta
        if (selectedCharacterText != null)
        {
            selectedCharacterText.text = $"Conversación con: {characterName}";
            Debug.Log($"Cambiando a personaje: {characterName}"); // Para debug
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
                // 🎯 SELECCIONAR AUTOMÁTICAMENTE A LILITH AL ABRIR
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

    private List<DialogueEntry> GetFilteredMessages(string characterName)
    {
        if (characterName == "Todos")
        {
            return allDialogueHistory;
        }

        // Para un personaje específico, mostramos SOLO sus mensajes (ya que cada DialogueSystem
        // solo guarda los diálogos del NPC correspondiente)
        if (dialoguesByCharacter.ContainsKey(characterName))
        {
            return dialoguesByCharacter[characterName];
        }

        return new List<DialogueEntry>();
    }

    private bool IsInConversationWith(DialogueEntry entry, string characterName)
    {
        // Esto es una aproximación simple. 
        // Consideramos que son de la misma conversación si están cerca en el tiempo
        // y alternan entre personajes

        // Buscar el índice de este mensaje
        int currentIndex = allDialogueHistory.IndexOf(entry);
        if (currentIndex == -1) return false;

        // Verificar mensajes adyacentes
        for (int i = Mathf.Max(0, currentIndex - 3); i <= Mathf.Min(allDialogueHistory.Count - 1, currentIndex + 3); i++)
        {
            if (allDialogueHistory[i].speakerName == characterName)
            {
                return true;
            }
        }

        return false;
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
}
