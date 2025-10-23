using System.Collections;
using TMPro;
using UnityEngine;

public class SimpleDialogueSystem : MonoBehaviour
{
    [Header("Configuración del Diálogo")]
    public string speakerName = "";
    [TextArea(3, 5)]
    public string[] dialogueLines;
    public float textSpeed = 0.05f;
    public bool autoActivate = true;

    [Header("Auto Avance")]
    public float autoAdvanceTime = 1f; // Tiempo para auto-skip (1 segundo)

    [Header("Referencias UI")]
    public GameObject dialoguePanel;
    public GameObject SpeakerContainer;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    [Header("Prompt de Interacción (F)")]
    public GameObject interactPrompt;
    public Vector3 promptOffset = new Vector3(0, 1f, 0);

    [Header("Configuración Avanzada")]
    public AudioClip dialogueSound;
    public bool destroyAfterDialogue = true;
    public GameObject[] objectsToActivateAfter;

    private bool isDialogueActive = false;
    private bool canInteract = false;
    private int currentLine = 0;
    private AudioSource audioSource;
    private MovimientoPersonaje playerMovement;
    private Coroutine typingCoroutine;
    private Coroutine autoAdvanceCoroutine;
    private Rigidbody2D playerRigidbody;
    private Vector2 originalVelocity;

    void Start()
    {
        playerMovement = FindObjectOfType<MovimientoPersonaje>();
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
        if (interactPrompt != null && interactPrompt.activeInHierarchy)
        {
            interactPrompt.transform.position = Camera.main.WorldToScreenPoint(transform.position + promptOffset);
        }

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
                if (interactPrompt != null)
                {
                    interactPrompt.SetActive(true);
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
        if (isDialogueActive || dialogueLines.Length == 0) return;

        SpeakerContainer.SetActive(true);
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
            originalVelocity = playerRigidbody.linearVelocity; // Guardar velocidad original
            playerRigidbody.linearVelocity = Vector2.zero; // Poner velocidad a cero
                                                     // NO usar isKinematic = true porque afecta a las colisiones
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
        if (lineIndex >= dialogueLines.Length) return;

        if (speakerText != null)
        {
            speakerText.text = speakerName;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(dialogueLines[lineIndex]));
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

        // Iniciar auto-avance después de terminar de escribir
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
        // Esperar el tiempo configurado (1 segundo por defecto)
        yield return new WaitForSeconds(autoAdvanceTime);

        // Avanzar automáticamente al siguiente diálogo
        AdvanceDialogue();
    }

    void AdvanceDialogue()
    {
        // Detener auto-avance si estaba activo
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        // Si se está escribiendo el texto, completarlo inmediatamente
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = dialogueLines[currentLine];
            typingCoroutine = null;

            // Reiniciar auto-avance con el texto completo
            StartAutoAdvance();
            return;
        }

        // Pasar a la siguiente línea
        currentLine++;

        if (currentLine < dialogueLines.Length)
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

        // Detener todas las corrutinas
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

        foreach (GameObject obj in objectsToActivateAfter)
        {
            if (obj != null)
            {
                obj.SetActive(true);
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

    void OnDrawGizmos()
    {
        Gizmos.color = autoActivate ? Color.yellow : Color.blue;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>().bounds.size);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + promptOffset, 0.2f);
    }
}
