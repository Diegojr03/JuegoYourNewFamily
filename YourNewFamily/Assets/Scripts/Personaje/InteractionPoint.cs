using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionPoint : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject interactPrompt; // Imagen de tecla F (diálogo)
    public float interactionDistance = 2f;
    public Vector3 promptOffset = new Vector3(0, 1f, 0);
    public DialogueSystem dialogueSystem; // Referencia directa

    [Header("Configuración")]
    public KeyCode interactionKey = KeyCode.F;
    public bool isDialogueTrigger = true;

    private Transform player;
    private bool canInteract = false;
    private MovimientoPersonaje playerMovement;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerMovement = player.GetComponent<MovimientoPersonaje>();

        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }

        // Si no se asignó manualmente, buscar en el mismo objeto
        if (dialogueSystem == null)
        {
            dialogueSystem = GetComponent<DialogueSystem>();
        }
    }

    void Update()
    {
        if (canInteract && Input.GetKeyDown(interactionKey))
        {
            TriggerInteraction();
        }

        // Actualizar posición del prompt en cada frame mientras esté activo
        if (canInteract && interactPrompt != null && interactPrompt.activeInHierarchy)
        {
            UpdatePromptPosition();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            canInteract = true;

            if (interactPrompt != null)
            {
                interactPrompt.SetActive(true);
                UpdatePromptPosition();
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            canInteract = false;

            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
            }
        }
    }

    void UpdatePromptPosition()
    {
        // Convertir posición mundial a posición en pantalla
        Vector3 worldPosition = transform.position + promptOffset;
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

        // Asignar la posición al prompt
        interactPrompt.transform.position = screenPosition;
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
        if (dialogueSystem != null)
        {
            dialogueSystem.StartDialogue();
        }
        else
        {
            Debug.LogError("No hay DialogueSystem asignado en " + gameObject.name);
        }

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
