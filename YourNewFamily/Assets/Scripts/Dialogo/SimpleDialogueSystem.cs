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
    private Coroutine autoAdvanceCoroutine;
    private Rigidbody2D playerRigidbody;
    private Vector2 originalVelocity;
    private Camera mainCamera;

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

        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceDialogue();
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

        // LLAMADA AL BACKLOG MANAGER - NUEVA
        if (BacklogManager.Instance != null)
        {
            BacklogManager.Instance.AddDialogueFromSimpleSystem(
                dialogueSections[lineIndex].speakerName,
                dialogueSections[lineIndex].dialogueText
            );
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
            // Antes aquí se deshabilitaba el collider, provocando que no se pudiera volver a
            // activar el diálogo. Para permitir repeticiones infinitas, NO tocamos el collider.
            // Si en el futuro quieres impedir re-triggers inmediatos, podemos añadir un
            // small cooldown (reuseDelay) aquí.
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