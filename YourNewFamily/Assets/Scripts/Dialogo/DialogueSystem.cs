using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    [Header("Personajes del Diálogo")]
    public Transform characterLeft;
    public Transform characterRight;
    public SpriteRenderer spriteLeft;
    public SpriteRenderer spriteRight;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText; // Texto separado para el nombre
    public TextMeshProUGUI dialogueText;
    public GameObject speakerContainer; // Contenedor del speaker (opcional)

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
    }

    void HideCharacters()
    {
        if (characterLeft != null && characterRight != null && mainCamera != null)
        {
            CalculateHiddenPosition();
            characterLeft.position = hiddenPosition;
            characterRight.position = hiddenPosition;
            charactersHidden = true; // <-- Añadido
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
        if (!isDialogueActive && dialogues.Count > 0)
        {
            CalculateTargetPositions();
            StartCoroutine(DialogueSequence());
        }
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

        if (gameObject.name == "PeterYLilith")
        {
            // Opción A: Si usas GameManager
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.SetTieneNieve(true);
            }

            // Opción B: Si prefieres solución simple
            PlayerPrefs.SetInt("TieneNieve", 1);
            PlayerPrefs.Save();

            Debug.Log("¡Ahora tienes nieve!");
        }

        isDialogueActive = false;
    }

    private IEnumerator MoveCharactersToPosition(bool enter)
    {
        Vector2 leftTarget = enter ? leftCharacterTarget : hiddenPosition;
        Vector2 rightTarget = enter ? rightCharacterTarget : hiddenPosition;

        charactersHidden = !enter; // <-- NUEVO

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

    private void HighlightCharacter(bool leftSpeaking)
    {
        if (spriteLeft != null && spriteRight != null)
        {
            spriteLeft.color = leftSpeaking ? Color.white : Color.gray;
            spriteRight.color = leftSpeaking ? Color.gray : Color.white;
        }
    }

    private IEnumerator ShowDialogue(Dialogue dialogue)
    {
        // Configurar speaker name en contenedor separado
        if (speakerText != null)
        {
            speakerText.text = dialogue.speakerName;
        }

        // Mostrar/ocultar contenedor del speaker
        if (speakerContainer != null)
        {
            speakerContainer.SetActive(!string.IsNullOrEmpty(dialogue.speakerName));
        }

        // Resaltar personaje que habla
        HighlightCharacter(dialogue.leftSpeaker);

        // Mostrar texto del diálogo
        dialogueText.text = "";
        string fullText = dialogue.dialogueText;

        // Iniciar escritura del texto
        Coroutine typingCoroutine = StartCoroutine(TypeText(fullText));

        // Esperar avance del diálogo
        yield return StartCoroutine(WaitForDialogueAdvance(typingCoroutine, fullText));
    }

    private IEnumerator WaitForDialogueAdvance(Coroutine typingCoroutine, string fullText)
    {
        bool typingCompleted = false;
        bool skipRequested = false;

        // Fase 1: mientras se escribe el texto, permitir saltar con espacio
        while (!typingCompleted)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Saltar la escritura
                skipRequested = true;
                StopCoroutine(typingCoroutine);
                dialogueText.text = fullText;
                typingCompleted = true;
            }

            // Verificar si ya terminó la escritura (el texto está completo)
            if (dialogueText.text == fullText)
            {
                typingCompleted = true;
            }

            yield return null;
        }

        // Fase 2: esperar a que el jugador pulse espacio o pase el tiempo automático
        float timer = 0f;
        bool inputReceived = false;

        while (timer < autoAdvanceTime && !inputReceived)
        {
            timer += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                inputReceived = true;
            }

            yield return null;
        }

        if (inputReceived)
        {
            yield return new WaitForSeconds(0.1f);
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
        // Si los personajes están ocultos, que sigan la cámara
        if (charactersHidden && mainCamera != null)
        {
            CalculateHiddenPosition();
            characterLeft.position = hiddenPosition;
            characterRight.position = hiddenPosition;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isDialogueActive)
        {
            if (autoActivate)
            {
                StartDialogue();
            }
        }
    }

    // Método para debug visual en el editor
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
