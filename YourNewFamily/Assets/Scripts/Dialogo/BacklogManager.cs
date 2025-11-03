using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BacklogManager : MonoBehaviour
{
    [Header("UI del Backlog")]
    public GameObject backlogPanel;
    public KeyCode toggleKey = KeyCode.B;

    [Header("Panel de Personajes")]
    public Transform charactersPanel;
    public GameObject characterButtonPrefab;

    [Header("Panel de Mensajes")]
    public Transform messagesContent;
    public GameObject messageEntryPrefab;
    public TextMeshProUGUI selectedCharacterText;

    [Header("Configuración")]
    public int maxEntries = 100;

    private List<DialogueEntry> dialogueHistory = new List<DialogueEntry>();
    private Dictionary<string, List<DialogueEntry>> dialoguesByCharacter = new Dictionary<string, List<DialogueEntry>>();
    private string selectedCharacter = "Todos";
    private bool isBacklogOpen = false;

    [System.Serializable]
    public class DialogueEntry
    {
        public string speakerName;
        public string dialogueText;
        public bool leftSpeaker;
        public Sprite characterSprite;
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
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleBacklog();
        }
    }

    public void AddDialogueToBacklog(DialogueSystem.Dialogue dialogue)
    {
        DialogueEntry entry = new DialogueEntry
        {
            speakerName = dialogue.speakerName,
            dialogueText = dialogue.dialogueText,
            leftSpeaker = dialogue.leftSpeaker,
            characterSprite = dialogue.characterSprite,
            timestamp = System.DateTime.Now.ToString("HH:mm:ss")
        };

        // Agregar al historial general
        dialogueHistory.Add(entry);

        // Limitar tamaño del historial
        if (dialogueHistory.Count > maxEntries)
        {
            dialogueHistory.RemoveAt(0);
        }

        // Agregar al diccionario por personaje
        if (!string.IsNullOrEmpty(dialogue.speakerName))
        {
            if (!dialoguesByCharacter.ContainsKey(dialogue.speakerName))
            {
                dialoguesByCharacter[dialogue.speakerName] = new List<DialogueEntry>();
                CreateCharacterButton(dialogue.speakerName);
            }
            dialoguesByCharacter[dialogue.speakerName].Add(entry);
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
            selectedCharacterText.text = $"Mensajes de: {characterName}";
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
            ? dialogueHistory
            : dialoguesByCharacter.ContainsKey(selectedCharacter)
                ? dialoguesByCharacter[selectedCharacter]
                : new List<DialogueEntry>();

        // Crear entradas para cada mensaje
        for (int i = 0; i < messagesToShow.Count; i++)
        {
            var entry = messagesToShow[i];
            GameObject messageObj = Instantiate(messageEntryPrefab, messagesContent);

            // Configurar la entrada del mensaje
            SetupMessageEntry(messageObj, entry);
        }

        // Hacer scroll al final
        ScrollRect scrollRect = messagesContent.parent.GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void SetupMessageEntry(GameObject messageObj, DialogueEntry entry)
    {
        // Buscar los componentes (ajusta los nombres según tu prefab)
        TextMeshProUGUI speakerText = messageObj.transform.Find("SpeakerText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI messageText = messageObj.transform.Find("MessageText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI timeText = messageObj.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
        Image speakerBackground = messageObj.transform.Find("SpeakerBackground")?.GetComponent<Image>();
        Image characterIcon = messageObj.transform.Find("CharacterIcon")?.GetComponent<Image>();

        // Configurar los textos
        if (speakerText != null) speakerText.text = entry.speakerName;
        if (messageText != null) messageText.text = entry.dialogueText;
        if (timeText != null) timeText.text = entry.timestamp;

        // Configurar colores según el lado del speaker
        if (speakerBackground != null)
        {
            speakerBackground.color = entry.leftSpeaker ?
                new Color(0.2f, 0.4f, 0.8f, 0.3f) : // Azul para izquierda
                new Color(0.8f, 0.3f, 0.3f, 0.3f);  // Rojo para derecha
        }

        // Configurar icono del personaje si existe
        if (characterIcon != null && entry.characterSprite != null)
        {
            characterIcon.sprite = entry.characterSprite;
            characterIcon.gameObject.SetActive(true);
        }
        else if (characterIcon != null)
        {
            characterIcon.gameObject.SetActive(false);
        }
    }

    public void ShowAllMessages()
    {
        SelectCharacter("Todos");
    }

    public void ClearBacklog()
    {
        dialogueHistory.Clear();
        dialoguesByCharacter.Clear();
        dialoguesByCharacter["Todos"] = new List<DialogueEntry>();

        // Limpiar botones de personajes (excepto "Todos")
        foreach (Transform child in charactersPanel)
        {
            if (child.GetComponentInChildren<TextMeshProUGUI>()?.text != "Todos")
            {
                Destroy(child.gameObject);
            }
        }

        RefreshMessagesUI();
    }
}
