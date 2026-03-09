using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovimientoPersonaje : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float moveSpeed = 2.5f;

    [Header("Configuración de Sprint")]
    public float sprintSpeed = 5f;
    public KeyCode sprintKey = KeyCode.LeftShift;

    private Rigidbody2D rb;
    private Vector2 movement;
    private ControlSettings controlManager;
    private float movimientoX;
    private float movimientoY;
    private Animator animator;

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
        if (!enabled)
        {
            // 🔥 CORREGIDO: Solo intentar setear parámetros si el animator existe
            if (animator != null)
            {
                // Verificar si los parámetros existen antes de setearlos
                if (HasParameter("MovimientoX")) animator.SetFloat("MovimientoX", 0);
                if (HasParameter("MovimientoY")) animator.SetFloat("MovimientoY", 0);
                if (HasParameter("IsSprinting")) animator.SetBool("IsSprinting", false);
            }
            return;
        }

        movimientoX = Input.GetAxisRaw("Horizontal");
        movimientoY = Input.GetAxisRaw("Vertical");

        // 🔥 CORREGIDO: Verificar si los parámetros existen
        if (animator != null)
        {
            if (HasParameter("MovimientoX")) animator.SetFloat("MovimientoX", movimientoX);
            if (HasParameter("MovimientoY")) animator.SetFloat("MovimientoY", movimientoY);
        }

        movement = GetMovementInput();
        movement = movement.normalized;

        isSprinting = Input.GetKey(sprintKey);

        // 🔥 CORREGIDO: Verificar si el parámetro IsSprinting existe
        if (animator != null && HasParameter("IsSprinting"))
        {
            animator.SetBool("IsSprinting", isSprinting && movement != Vector2.zero);
        }
    }

    // 🔥 NUEVO: Método para verificar si un parámetro existe en el Animator
    bool HasParameter(string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    Vector2 GetMovementInput()
    {
        Vector2 input = Vector2.zero;

        if (controlManager != null)
        {
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
            input.x = movimientoX;
            input.y = movimientoY;
        }

        return input;
    }

    void FixedUpdate()
    {
        if (!enabled)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        if (rb != null)
        {
            float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
            rb.linearVelocity = movement * currentSpeed;
        }
        else
        {
            float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
            transform.Translate(movement * currentSpeed * Time.fixedDeltaTime);
        }
    }

    void OnDisable()
    {
        if (animator != null)
        {
            // 🔥 CORREGIDO: Verificar antes de setear
            if (HasParameter("MovimientoX")) animator.SetFloat("MovimientoX", 0);
            if (HasParameter("MovimientoY")) animator.SetFloat("MovimientoY", 0);
            if (HasParameter("IsSprinting")) animator.SetBool("IsSprinting", false);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        isSprinting = false;
    }

    void OnEnable()
    {
        // Puedes agregar lógica de reinicio aquí si es necesario
    }

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

    public bool IsSprinting()
    {
        return isSprinting;
    }

    public void SetSprintSpeed(float newSpeed)
    {
        sprintSpeed = newSpeed;
    }
}