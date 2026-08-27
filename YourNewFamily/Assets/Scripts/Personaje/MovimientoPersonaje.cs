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
    private float stepTimer = 0f;
    private bool isMoving = false;

    // Referencia al gestor de pisadas
    private FootstepManager footstepManager;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        controlManager = FindObjectOfType<ControlSettings>();

        // Obtener o crear el FootstepManager
        footstepManager = GetComponent<FootstepManager>();
        if (footstepManager == null)
        {
            footstepManager = gameObject.AddComponent<FootstepManager>();
        }

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
            if (animator != null)
            {
                if (HasParameter("MovimientoX")) animator.SetFloat("MovimientoX", 0);
                if (HasParameter("MovimientoY")) animator.SetFloat("MovimientoY", 0);
                if (HasParameter("IsSprinting")) animator.SetBool("IsSprinting", false);
            }
            return;
        }

        movimientoX = Input.GetAxisRaw("Horizontal");
        movimientoY = Input.GetAxisRaw("Vertical");

        if (animator != null)
        {
            if (HasParameter("MovimientoX")) animator.SetFloat("MovimientoX", movimientoX);
            if (HasParameter("MovimientoY")) animator.SetFloat("MovimientoY", movimientoY);
        }

        movement = GetMovementInput();
        movement = movement.normalized;

        isSprinting = Input.GetKey(sprintKey);
        isMoving = movement.magnitude > 0.1f;

        if (animator != null && HasParameter("IsSprinting"))
        {
            animator.SetBool("IsSprinting", isSprinting && isMoving);
        }

        // Manejo de pasos (usando la configuración del FootstepManager)
        HandleFootsteps();
    }

    void HandleFootsteps()
    {
        if (!isMoving)
        {
            stepTimer = 0f;
            if (footstepManager != null)
            {
                footstepManager.StopFootsteps();
            }
            return;
        }

        // Obtener el intervalo actual del manager (con multiplicador de sprint)
        float currentInterval = footstepManager.GetCurrentInterval(isSprinting);

        stepTimer += Time.deltaTime;

        if (stepTimer >= currentInterval)
        {
            stepTimer = 0f;
            if (footstepManager != null)
            {
                footstepManager.PlayFootstep();
            }
        }
        else
        {
            if (footstepManager != null)
            {
                footstepManager.EnsureFootstepsActive();
            }
        }
    }

    // Resto de métodos (HasParameter, GetMovementInput, FixedUpdate, etc.) se mantienen igual...
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
            if (HasParameter("MovimientoX")) animator.SetFloat("MovimientoX", 0);
            if (HasParameter("MovimientoY")) animator.SetFloat("MovimientoY", 0);
            if (HasParameter("IsSprinting")) animator.SetBool("IsSprinting", false);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        isSprinting = false;
        stepTimer = 0f;

        if (footstepManager != null)
        {
            footstepManager.StopFootsteps();
        }
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