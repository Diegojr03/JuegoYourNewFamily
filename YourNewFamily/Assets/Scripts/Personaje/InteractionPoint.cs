using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionPoint : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject interactPrompt; // Imagen de tecla F (diálogo)
    public float interactionDistance = 2f;

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
        GetComponent<Collider2D>().isTrigger = false; // Importante: colisión normal
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
                interactPrompt.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1f);
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
