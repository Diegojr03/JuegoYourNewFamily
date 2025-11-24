using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueChoiceSystem : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(3, 5)] public string dialogueText;
        public bool leftSpeaker = true;
        public Sprite characterSprite;

        public bool endDialogueHere = false;  // 🔥 NUEVO

        public Choice[] choices;
    }

    [System.Serializable]
    public class Choice
    {
        public string choiceText;
        public int nextDialogueIndex = -1; // -1 = terminar diálogo
    }

    [Header("Diálogos")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();

    [Header("UI Principal")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public GameObject speakerContainer;

    [Header("UI de Opciones")]
    public GameObject choicePanel;          // Panel que contiene las opciones
    public Transform choicesContainer;      // Contenedor vertical (Layout Group)
    public GameObject choiceButtonPrefab;   // Prefab del botón

    [Header("Personajes")]
    public Transform characterLeft;
    public Transform characterRight;
    public SpriteRenderer spriteLeft;
    public SpriteRenderer spriteRight;
    public Sprite defaultLeftSprite;
    public Sprite defaultRightSprite;

    [Header("Configuración")]
    public float moveSpeed = 5f;
    public float dialogueCooldown = 0.05f;
    public float horizontalOffsetPercent = 0.3f;
    public float verticalOffsetPercent = 0.2f;

    // 🔥 NUEVO: Configuración avanzada del segundo script
    [Header("Configuración Avanzada")]
    public GameObject[] objectsToActivateAfter;
    public GameObject[] objectsToDestroyAfter;
    public bool destroyAfterDialogue = false;
    public float reuseDelay = 0.5f;

    private int currentIndex = 0;
    private bool isDialogueActive = false;
    private bool typing = false;
    private bool canInteract = false;
    private bool waitingForNextLine = false; // Control para esperar segunda pulsación
    private bool canReuse = true; // 🔥 NUEVO: Para evitar reactivaciones inmediatas

    private MovimientoPersonaje playerMovement;
    private Rigidbody2D playerRb;
    private Vector2 originalVelocity;
    private Camera mainCamera;

    private Vector2 leftCharacterTarget;
    private Vector2 rightCharacterTarget;
    private Vector2 hiddenPosition;

    private Coroutine typingCoroutine;

    void Start()
    {
        mainCamera = Camera.main;
        playerMovement = FindObjectOfType<MovimientoPersonaje>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerRb = player.GetComponent<Rigidbody2D>();

        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);

        HideCharacters();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    void Update()
    {
        // Espacio: primero completa typewriter, la segunda vez avanza (si no hay opciones mostradas)
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            if (typing)
            {
                SkipTyping();    // Espacio 1 -> completa texto
            }
            else if (waitingForNextLine && !choicePanel.activeSelf) // Solo avanzar si estamos esperando
            {
                // Espacio 2 -> avanzar (si no hay panel de opciones visible)
                waitingForNextLine = false; // Dejar de esperar
                AdvanceDialogue();
            }
        }

        // Activar diálogo con F
        if (!isDialogueActive && canInteract && Input.GetKeyDown(KeyCode.F) && canReuse) // 🔥 MODIFICADO: Añadido canReuse
        {
            StartDialogue();
        }
    }

    // -------------------------
    // INICIO DEL DIÁLOGO
    // -------------------------
    public void StartDialogue()
    {
        if (isDialogueActive || dialogueLines.Count == 0 || !canReuse) return; // 🔥 MODIFICADO: Añadido !canReuse

        InteractionPromptManager.Instance?.HidePrompt();

        currentIndex = 0;
        isDialogueActive = true;
        waitingForNextLine = false;
        canReuse = false; // 🔥 NUEVO: Evitar reactivación inmediata

        LockPlayer();

        dialoguePanel.SetActive(true);

        CalculateTargetPositions();
        StartCoroutine(MoveCharactersToPosition(true));

        ShowDialogueLine(currentIndex);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isDialogueActive && other.CompareTag("Player") && canReuse) // 🔥 MODIFICADO: Añadido canReuse
        {
            canInteract = true;

            // Mostrar la F
            InteractionPromptManager.Instance?.ShowPrompt(
                GetComponent<InteractionPoint>()
            );
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;

            // Ocultar la F
            InteractionPromptManager.Instance?.HidePrompt();
        }
    }

    // -------------------------
    // MOSTRAR UNA LÍNEA
    // -------------------------
    void ShowDialogueLine(int index)
    {
        DialogueLine line = dialogueLines[index];

        // UI speaker
        speakerText.text = line.speakerName;
        speakerContainer.SetActive(!string.IsNullOrEmpty(line.speakerName));

        // Backlog
        if (BacklogManager.Instance != null)
        {
            string owner = GetConversationOwner();
            BacklogManager.Instance.AddDialogueWithConversationOwner(
                line.speakerName,
                line.dialogueText,
                owner
            );
        }

        HighlightCharacter(line.leftSpeaker, line.characterSprite);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line.dialogueText));
        return;

        // Si hay opciones, se mostrarán después
    }

    // -------------------------
    // TYPEWRITER
    // -------------------------
    IEnumerator TypeText(string text)
    {
        typing = true;
        waitingForNextLine = false; // No estamos esperando todavía
        dialogueText.text = "";

        foreach (char c in text.ToCharArray())
        {
            dialogueText.text += c;

            // Si typing se pone a false desde SkipTyping, paramos el bucle
            if (!typing)
                break;

            yield return new WaitForSeconds(dialogueCooldown);
        }

        // Si el bucle terminó por skip (typing == false), aseguramos texto completo.
        DialogueLine currentLine = dialogueLines[currentIndex];
        dialogueText.text = currentLine.dialogueText;

        typing = false;

        // Si esta línea marca "terminar diálogo aquí", acabamos la conversación YA
        if (currentLine.endDialogueHere)
        {
            // Ocultar paneles y terminar (esperando movimiento de personajes)
            EndDialogue(); // Esto ahora llamará a la corrutina
            yield break;
        }

        // Si hay choices, mostrarlas; si no, esperar espacio para avanzar
        if (currentLine.choices != null && currentLine.choices.Length > 0)
        {
            ShowChoices(); // mostramos botones y esperamos interacción por botón
            yield break;
        }

        // No hay opciones -> esperar al siguiente espacio para avanzar
        waitingForNextLine = true; // Ahora sí estamos esperando la segunda pulsación
    }

    void ShowChoices()
    {
        DialogueLine line = dialogueLines[currentIndex];

        if (line.choices == null || line.choices.Length == 0)
            return;

        choicePanel.SetActive(true);

        // Borrar opciones anteriores
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        foreach (Choice choice in line.choices)
        {
            GameObject btnObj = Instantiate(choiceButtonPrefab, choicesContainer);

            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = choice.choiceText;

            Button btn = btnObj.GetComponent<Button>();
            int targetIndex = choice.nextDialogueIndex;

            btn.onClick.AddListener(() =>
            {
                // Ocultar panel y procesar la elección
                choicePanel.SetActive(false);
                SelectChoice(targetIndex);
            });
        }
    }

    void SkipTyping()
    {
        // Si ya no estamos escribiendo, no haga nada
        if (!typing) return;

        typing = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        DialogueLine line = dialogueLines[currentIndex];

        // Mostrar texto completo inmediatamente
        dialogueText.text = line.dialogueText;

        // Si la línea indica terminar diálogo -> terminar ya
        if (line.endDialogueHere)
        {
            EndDialogue(); // Esto ahora llamará a la corrutina
            return;
        }

        // Si hay opciones, mostrarlas; si no, esperar segunda pulsación de espacio
        if (line.choices != null && line.choices.Length > 0)
        {
            ShowChoices();
        }
        else
        {
            // Ahora esperamos la segunda pulsación explícitamente
            waitingForNextLine = true;
        }
    }

    IEnumerator WaitForNextLine()
    {
        // Esperamos hasta que el jugador pulse espacio
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        // Comprobamos si la línea actual pide terminar
        DialogueLine line = dialogueLines[currentIndex];
        if (line.endDialogueHere)
        {
            EndDialogue();
            yield break;
        }

        AdvanceDialogue();
    }

    void SelectChoice(int nextIndex)
    {
        if (nextIndex < 0 || nextIndex >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        currentIndex = nextIndex;
        ShowDialogueLine(currentIndex);
    }

    void AdvanceDialogue()
    {
        currentIndex++;

        if (currentIndex >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        ShowDialogueLine(currentIndex);
    }

    // -------------------------
    // FIN DEL DIÁLOGO
    // -------------------------
    void EndDialogue()
    {
        // Primero ocultamos la UI inmediatamente
        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);
        waitingForNextLine = false;

        // Iniciamos el movimiento de los personajes pero NO desactivamos todo aún
        StartCoroutine(EndDialogueSequence());
    }

    private IEnumerator EndDialogueSequence()
    {
        // Esperar a que los personajes se muevan fuera de pantalla
        yield return StartCoroutine(MoveCharactersToPosition(false));

        // Solo ahora desbloqueamos al jugador y finalizamos completamente
        UnlockPlayer();
        isDialogueActive = false;

        // 🔥 NUEVO: Ejecutar funcionalidades avanzadas
        ExecuteAdvancedFunctionality();
    }

    // 🔥 NUEVO: Método para ejecutar funcionalidades avanzadas
    void ExecuteAdvancedFunctionality()
    {
        // Activar objetos
        foreach (GameObject obj in objectsToActivateAfter)
        {
            if (obj != null) obj.SetActive(true);
        }

        // Destruir objetos
        foreach (GameObject obj in objectsToDestroyAfter)
        {
            if (obj != null) Destroy(obj);
        }

        // Destruir este objeto si está configurado
        if (destroyAfterDialogue)
        {
            Destroy(gameObject);
        }
        else
        {
            // Permitir reutilización después de un delay
            StartCoroutine(AllowReuseAfterDelay());
        }
    }

    // 🔥 NUEVO: Corrutina para permitir reutilización
    private IEnumerator AllowReuseAfterDelay()
    {
        yield return new WaitForSeconds(reuseDelay);
        canReuse = true;
    }

    // -------------------------
    // SISTEMA DE PERSONAJES
    // -------------------------
    void HighlightCharacter(bool left, Sprite custom)
    {
        if (left)
        {
            spriteLeft.color = Color.white;
            spriteRight.color = Color.gray;

            spriteLeft.sprite = custom != null ? custom : defaultLeftSprite;
            spriteRight.sprite = defaultRightSprite;
        }
        else
        {
            spriteLeft.color = Color.gray;
            spriteRight.color = Color.white;

            spriteRight.sprite = custom != null ? custom : defaultRightSprite;
            spriteLeft.sprite = defaultLeftSprite;
        }
    }

    void CalculateTargetPositions()
    {
        float height = mainCamera.orthographicSize * 2f;
        float width = height * mainCamera.aspect;

        float hOffset = width * horizontalOffsetPercent;
        float vOffset = height * verticalOffsetPercent;

        Vector3 center = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f));
        Vector3 bottom = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f));

        leftCharacterTarget = new Vector2(center.x - hOffset, bottom.y + vOffset);
        rightCharacterTarget = new Vector2(center.x + hOffset, bottom.y + vOffset);

        hiddenPosition = new Vector2(bottom.x, bottom.y - height);
    }

    IEnumerator MoveCharactersToPosition(bool enter)
    {
        Vector2 leftTarget = enter ? leftCharacterTarget : hiddenPosition;
        Vector2 rightTarget = enter ? rightCharacterTarget : hiddenPosition;

        while (Vector2.Distance(characterLeft.position, leftTarget) > 0.1f ||
               Vector2.Distance(characterRight.position, rightTarget) > 0.1f)
        {
            characterLeft.position = Vector2.Lerp(characterLeft.position, leftTarget, moveSpeed * Time.deltaTime);
            characterRight.position = Vector2.Lerp(characterRight.position, rightTarget, moveSpeed * Time.deltaTime);
            yield return null;
        }

        characterLeft.position = leftTarget;
        characterRight.position = rightTarget;
    }

    void HideCharacters()
    {
        if (mainCamera == null) return;

        Vector3 bottom = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f));
        float height = mainCamera.orthographicSize * 2f;

        hiddenPosition = new Vector2(bottom.x, bottom.y - height);
        characterLeft.position = hiddenPosition;
        characterRight.position = hiddenPosition;
    }

    // -------------------------
    // JUGADOR
    // -------------------------
    void LockPlayer()
    {
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerRb != null)
        {
            originalVelocity = playerRb.linearVelocity;
            playerRb.linearVelocity = Vector2.zero;
        }
    }

    void UnlockPlayer()
    {
        if (playerMovement != null)
            playerMovement.enabled = true;

        if (playerRb != null)
            playerRb.linearVelocity = originalVelocity;
    }

    // -------------------------
    // BACKLOG: determinar NPC propietario
    // -------------------------
    string GetConversationOwner()
    {
        foreach (var line in dialogueLines)
        {
            if (line.leftSpeaker)
                return line.speakerName;
        }
        return "Unknown";
    }
}
