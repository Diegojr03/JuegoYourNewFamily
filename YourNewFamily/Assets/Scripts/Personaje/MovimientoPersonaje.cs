using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovimientoPersonaje : MonoBehaviour
{
    [Header("Configuraci�n de Movimiento")]
    public float moveSpeed = 2.5f;

    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.freezeRotation = true;
        }
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;
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

    // Detectar colisi�n con los objetos de transici�n
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
