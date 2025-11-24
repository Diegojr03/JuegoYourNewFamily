using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    private BacklogManager backlogManager;

    [Header("Personajes del Diálogo")]
    public Transform characterLeft;
    public Transform characterRight;
    public SpriteRenderer spriteLeft;
    public SpriteRenderer spriteRight;

    [Header("Sprites por Defecto")]
    public Sprite defaultLeftSprite;
    public Sprite defaultRightSprite;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public GameObject speakerContainer;

    [Header("Configuración")]
    public float moveSpeed = 5f;
    public float dialogueCooldown = 0.05f;
    public float horizontalOffsetPercent = 0.3f;
    public float verticalOffsetPercent = 0.2f;
    public float autoAdvanceTime = 1.5f;

    [Header("Diálogos")]
    public List<Dialogue> dialogues = new List<Dialogue>();

    [System.Serializable]
    public class Dialogue
    {
        public string speakerName;
        [TextArea(3, 5)]
        public string dialogueText;
        public bool leftSpeaker;
        public Sprite characterSprite;
    }

    [Header("Configuración Avanzada")]
    public GameObject[] objectsToActivateAfter;
    public GameObject[] objectsToDestroyAfter;
    public bool destroyAfterDialogue = false;

    [Header("Activación")]
    public bool autoActivate = false;

    private bool isDialogueActive = false;
    private MovimientoPersonaje playerMovement;
    private Camera mainCamera;
    private Vector2 leftCharacterTarget;
    private Vector2 rightCharacterTarget;
    private Vector2 hiddenPosition;
    private Rigidbody2D playerRigidbody;
    private Vector2 originalVelocity;
    private bool charactersHidden = true;

    // NUEVO: Para evitar reactivaciones inmediatas
    public float reuseDelay = 0.5f;
    private bool canReuse = true;

    void Start()
    {
        playerMovement = FindObjectOfType<MovimientoPersonaje>();
        mainCamera = Camera.main;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody2D>();
        }

        // Ocultar UI al inicio
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (speakerContainer != null)
        {
            speakerContainer.SetActive(false);
        }

        HideCharacters();
        backlogManager = FindObjectOfType<BacklogManager>();
    }

    void HideCharacters()
    {
        if (characterLeft != null && characterRight != null && mainCamera != null)
        {
            CalculateHiddenPosition();
            characterLeft.position = hiddenPosition;
            characterRight.position = hiddenPosition;
            charactersHidden = true;
        }
    }

    void CalculateTargetPositions()
    {
        if (mainCamera == null) return;

        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        float horizontalOffset = cameraWidth * horizontalOffsetPercent;
        float verticalOffset = cameraHeight * verticalOffsetPercent;

        Vector3 cameraCenter = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, mainCamera.nearClipPlane));
        Vector3 cameraBottom = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, mainCamera.nearClipPlane));

        leftCharacterTarget = new Vector2(
            cameraCenter.x - horizontalOffset,
            cameraBottom.y + verticalOffset
        );

        rightCharacterTarget = new Vector2(
            cameraCenter.x + horizontalOffset,
            cameraBottom.y + verticalOffset
        );

        CalculateHiddenPosition();
    }

    void CalculateHiddenPosition()
    {
        if (mainCamera == null) return;

        Vector3 cameraBottom = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, mainCamera.nearClipPlane));
        float cameraHeight = mainCamera.orthographicSize * 2f;

        hiddenPosition = new Vector2(
            cameraBottom.x,
            cameraBottom.y - cameraHeight
        );
    }

    public void StartDialogue()
    {
        if (isDialogueActive || dialogues.Count == 0 || !canReuse)
            return;

        CalculateTargetPositions();
        StartCoroutine(DialogueSequence());
    }

    private IEnumerator DialogueSequence()
    {
        isDialogueActive = true;

        // Bloquear movimiento del jugador
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (playerRigidbody != null)
        {
            originalVelocity = playerRigidbody.linearVelocity;
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        // Mover personajes a posición si existen
        if (characterLeft != null && characterRight != null)
        {
            yield return StartCoroutine(MoveCharactersToPosition(true));
        }

        // Mostrar UI
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        // Mostrar cada diálogo
        foreach (Dialogue dialogue in dialogues)
        {
            yield return StartCoroutine(ShowDialogue(dialogue));
        }

        // Ocultar UI
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (speakerContainer != null)
        {
            speakerContainer.SetActive(false);
        }

        // Mover personajes fuera de escena si existen
        if (characterLeft != null && characterRight != null)
        {
            yield return StartCoroutine(MoveCharactersToPosition(false));
        }

        // Reactivar movimiento del jugador
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = originalVelocity;
        }

        // Activar y destruir objetos
        foreach (GameObject obj in objectsToActivateAfter)
        {
            if (obj != null) obj.SetActive(true);
        }

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
            // 🔁 NUEVO: Permitir repetir diálogos infinitamente.
            // En lugar de destruir o desactivar, ponemos un pequeño delay para evitar
            // que el jugador lo reactive inmediatamente.
            isDialogueActive = false;
            StartCoroutine(AllowReuseAfterDelay());
        }
    }

    private IEnumerator AllowReuseAfterDelay()
    {
        canReuse = false;
        yield return new WaitForSeconds(reuseDelay);
        canReuse = true;
    }

    private IEnumerator MoveCharactersToPosition(bool enter)
    {
        Vector2 leftTarget = enter ? leftCharacterTarget : hiddenPosition;
        Vector2 rightTarget = enter ? rightCharacterTarget : hiddenPosition;

        charactersHidden = !enter;

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

    private void HighlightCharacter(bool leftSpeaking, Sprite customSprite = null)
    {
        if (spriteLeft != null && spriteRight != null)
        {
            Vector3 targetScale = new Vector3(0.3885461f, 0.3996474f, 0.2466959f);

            if (leftSpeaking)
            {
                spriteLeft.sprite = customSprite != null ? customSprite : defaultLeftSprite;
                spriteLeft.color = Color.white;
                characterLeft.localScale = targetScale;

                spriteRight.sprite = defaultRightSprite;
                spriteRight.color = Color.gray;
                characterRight.localScale = targetScale;
            }
            else
            {
                spriteRight.sprite = customSprite != null ? customSprite : defaultRightSprite;
                spriteRight.color = Color.white;
                characterRight.localScale = targetScale;

                spriteLeft.sprite = defaultLeftSprite;
                spriteLeft.color = Color.gray;
                characterLeft.localScale = targetScale;
            }
        }
    }

    private IEnumerator ShowDialogue(Dialogue dialogue)
    {
        if (speakerText != null)
        {
            speakerText.text = dialogue.speakerName;
        }

        if (speakerContainer != null)
        {
            speakerContainer.SetActive(!string.IsNullOrEmpty(dialogue.speakerName));
        }

        // 🎯 GUARDAR TODOS los diálogos asignados al NPC de la conversación
        if (BacklogManager.Instance != null)
        {
            string conversationOwner = GetConversationOwner();

            BacklogManager.Instance.AddDialogueWithConversationOwner(
                dialogue.speakerName,
                dialogue.dialogueText,
                conversationOwner
            );
        }

        HighlightCharacter(dialogue.leftSpeaker, dialogue.characterSprite);

        dialogueText.text = "";
        string fullText = dialogue.dialogueText;

        Coroutine typingCoroutine = StartCoroutine(TypeText(fullText));
        yield return StartCoroutine(WaitForDialogueAdvance(typingCoroutine, fullText));

    }

    private string GetConversationOwner()
    {
        // Buscar el primer NPC que habló en esta conversación
        foreach (var dialogue in dialogues)
        {
            if (dialogue.leftSpeaker) // leftSpeaker = true significa NPC
            {
                return dialogue.speakerName;
            }
        }
        return "Unknown";
    }

    private IEnumerator WaitForDialogueAdvance(Coroutine typingCoroutine, string fullText)
    {
        bool typingCompleted = false;

        // Esperar a que termine el typing o se pulse espacio
        while (!typingCompleted)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StopCoroutine(typingCoroutine);
                dialogueText.text = fullText;
                typingCompleted = true;
            }

            if (dialogueText.text == fullText)
            {
                typingCompleted = true;
            }

            yield return null;
        }

        // 🔥 MODIFICADO: Esperar segunda pulsación de espacio (igual que DialogueChoiceSystem)
        bool nextLineRequested = false;
        while (!nextLineRequested)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                nextLineRequested = true;
            }
            yield return null;
        }
    }

    private IEnumerator TypeText(string text)
    {
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueCooldown);
        }
    }

    void Update()
    {
        if (charactersHidden && mainCamera != null)
        {
            CalculateHiddenPosition();
            characterLeft.position = hiddenPosition;
            characterRight.position = hiddenPosition;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isDialogueActive && canReuse)
        {
            if (autoActivate)
            {
                StartDialogue();
            }
            else
            {
                InteractionPromptManager.Instance?.ShowPrompt(this.GetComponent<InteractionPoint>());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            InteractionPromptManager.Instance?.HidePrompt();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (mainCamera != null && Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(leftCharacterTarget, 0.3f);
            Gizmos.DrawWireSphere(rightCharacterTarget, 0.3f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hiddenPosition, 0.3f);
        }
    }
}
