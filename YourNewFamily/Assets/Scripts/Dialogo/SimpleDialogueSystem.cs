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

    [Header("Auto Avance")]
    public float autoAdvanceTime = 1f;

    [Header("Secciones de Diálogo")]
    public DialogueSection[] dialogueSections;

    [Header("Referencias UI")]
    public GameObject dialoguePanel;
    public GameObject speakerContainer;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    [Header("Prompt de Interacción (F)")]
    public GameObject interactPrompt;
    public Vector3 promptOffset = new Vector3(0, 1f, 0);
    public bool useWorldSpace = true; // <-- NUEVO: Opción para cambiar entre espacio mundo y UI

    [Header("Configuración Avanzada")]
    public AudioClip dialogueSound;
    public bool destroyAfterDialogue = true;
    public GameObject[] objectsToActivateAfter;

    [Header("Objetos a Destruir")]
    public GameObject[] objectsToDestroyAfter;

    private bool isDialogueActive = false;
    private bool canInteract = false;
    private int currentLine = 0;
    private AudioSource audioSource;
    private MovimientoPersonaje playerMovement;
    private Coroutine typingCoroutine;
    private Coroutine autoAdvanceCoroutine;
    private Rigidbody2D playerRigidbody;
    private Vector2 originalVelocity;
    private Camera mainCamera;
    private Canvas parentCanvas; // <-- NUEVO: Para detectar el tipo de canvas

    void Start()
    {
        playerMovement = FindObjectOfType<MovimientoPersonaje>();
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();

        // <-- NUEVO: Detectar el canvas padre
        if (interactPrompt != null)
        {
            parentCanvas = interactPrompt.GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                parentCanvas = FindObjectOfType<Canvas>();
            }
        }

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

        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    void Update()
    {
        UpdateInteractPromptPosition();

        if (!autoActivate && canInteract && Input.GetKeyDown(KeyCode.F) && !isDialogueActive)
        {
            StartDialogue();
        }

        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceDialogue();
        }
    }

    // <-- NUEVO: Método separado para actualizar la posición del prompt
    void UpdateInteractPromptPosition()
    {
        if (interactPrompt != null && interactPrompt.activeInHierarchy)
        {
            if (useWorldSpace && mainCamera != null)
            {
                // Usar espacio mundo (como en DialogueSystem)
                Vector3 worldPosition = transform.position + promptOffset;
                Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

                if (screenPosition.z > 0) // Solo si está frente a la cámara
                {
                    interactPrompt.transform.position = screenPosition;
                }
            }
            else
            {
                // Usar espacio UI relativo al objeto
                interactPrompt.transform.position = mainCamera.WorldToScreenPoint(transform.position + promptOffset);
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
                if (interactPrompt != null)
                {
                    interactPrompt.SetActive(true);
                    // <-- NUEVO: Forzar actualización inmediata
                    UpdateInteractPromptPosition();
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !autoActivate)
        {
            canInteract = false;
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
            }
        }
    }

    public void StartDialogue()
    {
        if (isDialogueActive || dialogueSections.Length == 0) return;

        speakerContainer.SetActive(true);
        isDialogueActive = true;
        currentLine = 0;

        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }

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

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(dialogueSections[lineIndex].dialogueText));
    }

    IEnumerator TypeText(string text)
    {
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }

        typingCoroutine = null;
        StartAutoAdvance();
    }

    void StartAutoAdvance()
    {
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
        }
        autoAdvanceCoroutine = StartCoroutine(AutoAdvance());
    }

    IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(autoAdvanceTime);
        AdvanceDialogue();
    }

    void AdvanceDialogue()
    {
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = dialogueSections[currentLine].dialogueText;
            typingCoroutine = null;

            StartAutoAdvance();
            return;
        }

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

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
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
        else
        {
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    // <-- NUEVO: Método para forzar reposicionamiento si es necesario
    public void ForceRepositionPrompt()
    {
        if (interactPrompt != null && interactPrompt.activeInHierarchy)
        {
            UpdateInteractPromptPosition();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = autoActivate ? Color.yellow : Color.blue;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>().bounds.size);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + promptOffset, 0.2f);
    }
}