using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovimientoPersonaje : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float moveSpeed = 2.5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private ControlSettings controlManager;
    private float movimientoX;
    private float movimientoY;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        controlManager = FindObjectOfType<ControlSettings>();

        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.freezeRotation = true;
        }

        if (controlManager == null)
        {
            Debug.LogWarning("ControlManager no encontrado. Usando controles por defecto.");
        }
    }

    void Update()
    {
        movimientoX = Input.GetAxisRaw("Horizontal");
        movimientoY = Input.GetAxisRaw("Vertical");
        animator.SetFloat("MovimientoX",movimientoX);
        animator.SetFloat("MovimientoY", movimientoY);
        // Obtener input usando los controles personalizados
        movement = GetMovementInput();
        movement = movement.normalized;
    }

    Vector2 GetMovementInput()
    {
        Vector2 input = Vector2.zero;

        if (controlManager != null)
        {
            // Usar controles personalizados del ControlManager
            if (Input.GetKey(controlManager.GetKeyForAction("Arriba")))
                input.y += 1f;

            if (Input.GetKey(controlManager.GetKeyForAction("Abajo")))
                input.y -= 1f;

            if (Input.GetKey(controlManager.GetKeyForAction("Izquierda")))
                input.x -= 1f;

            if (Input.GetKey(controlManager.GetKeyForAction("Derecha")))
                input.x += 1f;
        }
        else
        {
            // Fallback a controles por defecto si no hay ControlManager
            input.x = movimientoX;
            input.y = movimientoY;
        }

        return input;
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = movement * moveSpeed;
        }
        else
        {
            transform.Translate(movement * moveSpeed * Time.fixedDeltaTime);
        }
    }

    // Detectar colisión con los objetos de transición
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Transition"))
        {
            TransitionPoint transition = other.GetComponent<TransitionPoint>();
            if (transition != null)
            {
                transition.InitiateTransition(gameObject);
            }
        }
    }
}
