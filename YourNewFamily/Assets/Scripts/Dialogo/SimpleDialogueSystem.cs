using System.Collections;
using TMPro;
using UnityEngine;

[System.Serializable]
public class DialogueSection
{
    public string speakerName = "";
    [TextArea(3, 5)]
    public string dialogueText;
}

public class SimpleDialogueSystem : MonoBehaviour
{
    [Header("Configuración del Diálogo")]
    public float textSpeed = 0.05f;
    public bool autoActivate = true;

    [Header("Secciones de Diálogo")]
    public DialogueSection[] dialogueSections;

    [Header("Referencias UI")]
    public GameObject dialoguePanel;
    public GameObject speakerContainer;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    [Header("Prompt de Interacción (F)")]
    public Vector3 promptOffset = new Vector3(0, 1f, 0);

    [Header("Configuración Avanzada")]
    public AudioClip dialogueSound;
    public bool destroyAfterDialogue = true;
    public GameObject[] objectsToActivateAfter;
    public GameObject[] objectsToDestroyAfter;

    private bool isDialogueActive = false;
    private bool canInteract = false;
    private int currentLine = 0;
    private AudioSource audioSource;
    private MovimientoPersonaje playerMovement;
    private Coroutine typingCoroutine;
    private Rigidbody2D playerRigidbody;
    private Vector2 originalVelocity;
    private Camera mainCamera;

    // 🔥 NUEVO: Variables para control de teclas
    private bool isTyping = false;
    private bool waitingForNextLine = false;

    void Start()
    {
        playerMovement = FindObjectOfType<MovimientoPersonaje>();
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody2D>();
        }
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    void Update()
    {
        if (!autoActivate && canInteract && Input.GetKeyDown(KeyCode.F) && !isDialogueActive)
        {
            StartDialogue();
        }

        // 🔥 MODIFICADO: Comportamiento igual que DialogueChoiceSystem
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                SkipTyping();    // Espacio 1 -> completa texto
            }
            else if (waitingForNextLine) // Solo avanzar si estamos esperando
            {
                // Espacio 2 -> avanzar
                waitingForNextLine = false;
                AdvanceDialogue();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (autoActivate)
            {
                StartDialogue();
            }
            else
            {
                canInteract = true;
                InteractionPromptManager.Instance?.ShowPrompt(this);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !autoActivate)
        {
            canInteract = false;
            InteractionPromptManager.Instance?.HidePrompt();
        }
    }

    public void StartDialogue()
    {
        if (isDialogueActive || dialogueSections.Length == 0) return;

        InteractionPromptManager.Instance?.HidePrompt();

        speakerContainer.SetActive(true);
        isDialogueActive = true;
        currentLine = 0;
        waitingForNextLine = false;
        isTyping = false;

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (playerRigidbody != null)
        {
            originalVelocity = playerRigidbody.linearVelocity;
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        if (dialogueSound != null)
        {
            audioSource.PlayOneShot(dialogueSound);
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        ShowLine(currentLine);
    }

    void ShowLine(int lineIndex)
    {
        if (lineIndex >= dialogueSections.Length) return;

        if (speakerText != null)
        {
            speakerText.text = dialogueSections[lineIndex].speakerName;
        }

        if (speakerContainer != null)
        {
            speakerContainer.SetActive(!string.IsNullOrEmpty(dialogueSections[lineIndex].speakerName));
        }

        /*// LLAMADA AL BACKLOG MANAGER - NUEVA
        if (BacklogManager.Instance != null)
        {
            BacklogManager.Instance.AddDialogueFromSimpleSystem(
                dialogueSections[lineIndex].speakerName,
                dialogueSections[lineIndex].dialogueText
            );
        }*/

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(dialogueSections[lineIndex].dialogueText));
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        waitingForNextLine = false;
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;

            // Si se skipea el typing, paramos el bucle
            if (!isTyping)
                break;

            yield return new WaitForSeconds(textSpeed);
        }

        // Si se skipeó, aseguramos texto completo
        dialogueText.text = text;

        isTyping = false;

        // Esperar segunda pulsación de espacio
        waitingForNextLine = true;
    }

    // 🔥 NUEVO: Método para saltar el typing
    void SkipTyping()
    {
        if (!isTyping) return;

        isTyping = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // Mostrar texto completo inmediatamente
        dialogueText.text = dialogueSections[currentLine].dialogueText;

        // Esperar segunda pulsación de espacio
        waitingForNextLine = true;
    }

    void AdvanceDialogue()
    {
        currentLine++;

        if (currentLine < dialogueSections.Length)
        {
            ShowLine(currentLine);
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        isTyping = false;
        waitingForNextLine = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = originalVelocity;
        }

        foreach (GameObject obj in objectsToActivateAfter)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        foreach (GameObject obj in objectsToDestroyAfter)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        if (destroyAfterDialogue)
        {
            Destroy(gameObject);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = autoActivate ? Color.yellow : Color.blue;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + promptOffset, 0.2f);
    }
}