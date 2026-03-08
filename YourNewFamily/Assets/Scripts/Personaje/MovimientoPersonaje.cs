using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovimientoPersonaje : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float moveSpeed = 2.5f;

    // 🔥 NUEVO: Variables para el sprint
    [Header("Configuración de Sprint")]
    public float sprintSpeed = 5f; // Velocidad al sprintar
    public KeyCode sprintKey = KeyCode.LeftShift; // Tecla de sprint

    private Rigidbody2D rb;
    private Vector2 movement;
    private ControlSettings controlManager;
    private float movimientoX;
    private float movimientoY;
    private Animator animator;

    // 🔥 NUEVO: Variable para saber si está sprintando
    private bool isSprinting = false;

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
        // 🔥 VERIFICAR SI ESTE SCRIPT ESTÁ DESACTIVADO
        if (!enabled)
        {
            // Si el script está desactivado, cancelar animación
            animator.SetFloat("MovimientoX", 0);
            animator.SetFloat("MovimientoY", 0);
            return; // Salir temprano, no procesar más input
        }

        movimientoX = Input.GetAxisRaw("Horizontal");
        movimientoY = Input.GetAxisRaw("Vertical");
        animator.SetFloat("MovimientoX", movimientoX);
        animator.SetFloat("MovimientoY", movimientoY);

        // Obtener input usando los controles personalizados
        movement = GetMovementInput();
        movement = movement.normalized;

        // 🔥 NUEVO: Detectar si está pulsando Shift izquierdo para sprintar
        isSprinting = Input.GetKey(sprintKey);

        // 🔥 NUEVO: Actualizar animator con el estado de sprint (si tienes animaciones de sprint)
        animator.SetBool("IsSprinting", isSprinting && movement != Vector2.zero);
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
        // 🔥 TAMBIÉN VERIFICAR EN FIXEDUPDATE
        if (!enabled)
        {
            // Si el script está desactivado, detener movimiento físico
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return; // Salir temprano, no procesar más movimiento
        }

        if (rb != null)
        {
            // 🔥 MODIFICADO: Usar velocidad diferente si está sprintando
            float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
            rb.linearVelocity = movement * currentSpeed;
        }
        else
        {
            // 🔥 MODIFICADO: Usar velocidad diferente si está sprintando
            float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
            transform.Translate(movement * currentSpeed * Time.fixedDeltaTime);
        }
    }

    // 🔥 NUEVO: Cuando se desactiva el script, también cancelar animación
    void OnDisable()
    {
        if (animator != null)
        {
            animator.SetFloat("MovimientoX", 0);
            animator.SetFloat("MovimientoY", 0);
            animator.SetBool("IsSprinting", false); // 🔥 NUEVO
        }

        // También detener movimiento físico al desactivar
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        isSprinting = false; // 🔥 NUEVO
    }

    // 🔥 NUEVO: Cuando se activa el script, restaurar valores si es necesario
    void OnEnable()
    {
        // Puedes agregar lógica de reinicio aquí si es necesario
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

    // 🔥 NUEVO: Método público para obtener si está sprintando (útil para otros scripts)
    public bool IsSprinting()
    {
        return isSprinting;
    }

    // 🔥 NUEVO: Método para cambiar la velocidad de sprint dinámicamente
    public void SetSprintSpeed(float newSpeed)
    {
        sprintSpeed = newSpeed;
    }
}