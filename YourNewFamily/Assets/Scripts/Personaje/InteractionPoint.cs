using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionPoint : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    public float interactionDistance = 2f;
    public Vector3 promptOffset = new Vector3(0, 1f, 0);

    [Header("Sistemas de Diálogo")]
    public DialogueSystem dialogueSystem;
    public DialogueChoiceSystem dialogueChoiceSystem;

    [Header("Configuración")]
    public KeyCode interactionKey = KeyCode.F;
    public bool isDialogueTrigger = true;

    private Transform player;
    private bool canInteract = false;
    private MovimientoPersonaje playerMovement;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
            playerMovement = player.GetComponent<MovimientoPersonaje>();

        // Si no se asignó manualmente, buscar en el mismo objeto
        if (dialogueSystem == null)
            dialogueSystem = GetComponent<DialogueSystem>();

        if (dialogueChoiceSystem == null)
            dialogueChoiceSystem = GetComponent<DialogueChoiceSystem>();
    }

    void Update()
    {
        if (canInteract && Input.GetKeyDown(interactionKey))
        {
            TriggerInteraction();
        }
    }

    // --- IMPORTANTE: USAS OnCollision, pero deberías usar Trigger si es diálogo ---
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            canInteract = true;
            InteractionPromptManager.Instance?.ShowPrompt(this);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            canInteract = false;
            InteractionPromptManager.Instance?.HidePrompt();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            canInteract = true;
            InteractionPromptManager.Instance?.ShowPrompt(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            canInteract = false;
            InteractionPromptManager.Instance?.HidePrompt();
        }
    }

    void TriggerInteraction()
    {
        if (isDialogueTrigger)
        {
            StartDialogue();
        }
        else
        {
            Debug.Log("Activando puzzle...");
        }
    }

    void StartDialogue()
    {
        InteractionPromptManager.Instance?.HidePrompt();

        // 1) PRIORIDAD: First DialogueChoiceSystem
        if (dialogueChoiceSystem != null)
        {
            dialogueChoiceSystem.StartDialogue();
            return;
        }

        // 2) Si no hay choice, usa el DialogueSystem normal
        if (dialogueSystem != null)
        {
            dialogueSystem.StartDialogue();
            return;
        }

        Debug.LogError("No hay ningún sistema de diálogo asignado en " + gameObject.name);
    }

    // Métodos públicos para configuración
    public void SetInteractionKey(KeyCode newKey)
    {
        interactionKey = newKey;
    }

    public void SetInteractionType(bool isForDialogue)
    {
        isDialogueTrigger = isForDialogue;
    }
}
