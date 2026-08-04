using System.Collections;
using TMPro;
using UnityEngine;

[System.Serializable]
public class DialogueSection
{
    public string speakerName = "";
    [TextArea(3, 5)]
    public string dialogueText;

    public bool giveItemAfterThisLine = false;
    public string itemIdToGive = "";
    public string itemNameToGive = "";
    public Sprite itemIconToGive = null;
}

public class SimpleDialogueSystem : MonoBehaviour
{
    [Header("Identificador de Diálogo")]
    public string dialogueId = ""; // 🔥 NUEVO

    [Header("Sistema de Inventario")]
    public GameObject itemToAddToInventory;
    public Sprite inventoryItemIcon;
    public string inventoryItemId = "item_default";
    public string inventoryItemName = "Nuevo Item";

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

        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                SkipTyping();
            }
            else if (waitingForNextLine)
            {
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

        // 🔥 Registrar diálogo completado
        if (!string.IsNullOrEmpty(dialogueId) && SaveManager.Instance != null)
            SaveManager.Instance.RegisterDialogueCompleted(dialogueId);

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

        if (dialogueSections[lineIndex].giveItemAfterThisLine)
        {
            GiveInventoryItem(
                dialogueSections[lineIndex].itemIdToGive,
                dialogueSections[lineIndex].itemNameToGive,
                dialogueSections[lineIndex].itemIconToGive
            );
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(dialogueSections[lineIndex].dialogueText));
    }

    private void GiveInventoryItem(string itemId, string itemName, Sprite itemIcon)
    {
        if (InventorySystem.Instance != null && !string.IsNullOrEmpty(itemId))
        {
            if (itemToAddToInventory != null)
            {
                InventorySystem.Instance.AddItemFromPrefab(itemToAddToInventory);
            }
            else if (itemIcon != null)
            {
                InventorySystem.Instance.AddSimpleItem(itemId, itemName, itemIcon);
            }
            else
            {
                InventorySystem.Instance.AddSimpleItem(
                    inventoryItemId,
                    inventoryItemName,
                    inventoryItemIcon
                );
            }
        }
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        waitingForNextLine = false;
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;

            if (!isTyping)
                break;

            yield return new WaitForSeconds(textSpeed);
        }

        dialogueText.text = text;

        isTyping = false;

        waitingForNextLine = true;
    }

    void SkipTyping()
    {
        if (!isTyping) return;

        isTyping = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogueText.text = dialogueSections[currentLine].dialogueText;

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

        // 🔥 Activar objetos y registrar estado
        foreach (GameObject obj in objectsToActivateAfter)
        {
            if (obj != null)
            {
                SaveableObject saveable = obj.GetComponent<SaveableObject>();
                if (saveable != null && SaveManager.Instance != null)
                    SaveManager.Instance.RegisterObjectState(saveable.objectId, true);
                obj.SetActive(true);
            }
        }

        // 🔥 Desactivar objetos (en lugar de destruir) y registrar estado
        foreach (GameObject obj in objectsToDestroyAfter)
        {
            if (obj != null)
            {
                SaveableObject saveable = obj.GetComponent<SaveableObject>();
                if (saveable != null && SaveManager.Instance != null)
                    SaveManager.Instance.RegisterObjectState(saveable.objectId, false);
                obj.SetActive(false);
            }
        }

        if (destroyAfterDialogue)
        {
            SaveableObject thisSaveable = GetComponent<SaveableObject>();
            if (thisSaveable != null && SaveManager.Instance != null)
                SaveManager.Instance.RegisterObjectState(thisSaveable.objectId, false);
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