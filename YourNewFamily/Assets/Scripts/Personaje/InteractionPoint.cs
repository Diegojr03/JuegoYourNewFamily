using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionPoint : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject interactPrompt; // Imagen de tecla F (diálogo)
    public float interactionDistance = 2f;
    public float promptOffsetY = 0.3f;

    [Header("Configuración")]
    public KeyCode interactionKey = KeyCode.F;
    public bool isDialogueTrigger = true;

    private Transform player;
    private bool canInteract = false;
    private MovimientoPersonaje playerMovement; // Cambiado a tu script

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerMovement = player.GetComponent<MovimientoPersonaje>();

        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }

        // Asegurar que tiene Collider (no trigger)
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }
        GetComponent<Collider2D>().isTrigger = true; // Importante: colisión normal
    }

    void Update()
    {
        if (canInteract && Input.GetKeyDown(interactionKey))
        {
            TriggerInteraction();
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

                // Mostrar el prompt justo encima del objeto
                Vector3 worldPos = transform.position + Vector3.up * promptOffsetY;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                interactPrompt.transform.position = screenPos;
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
        DialogueSystem dialogueSystem = FindObjectOfType<DialogueSystem>();
        if (dialogueSystem != null)
        {
            dialogueSystem.StartDialogue();
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
