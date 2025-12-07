using UnityEngine;
using System.Collections;

public class InteractableObject2D : MonoBehaviour
{
    [Header("CONFIGURACIÓN INTERACCIÓN")]
    public KeyCode teclaInteraccion = KeyCode.E;
    public float distanciaInteraccion = 2f;

    [Header("CONFIGURACIÓN TECLA E")]
    public Sprite spriteTeclaE;
    public Vector3 posicionTeclaE = new Vector3(0, 1.5f, 0);
    public Vector3 escalaTeclaE = new Vector3(0.25f, 0.25f, 0.25f);
    public float velocidadAnimacion = 3f;
    public float amplitudAnimacion = 0.1f;

    [Header("CONFIGURACIÓN PANEL")]
    public GameObject panelInteractivo;
    public bool mantenerPanelAbierto = false;

    [Header("CONFIGURACIÓN JUGADOR")]
    public MonoBehaviour scriptMovimientoJugador;

    private GameObject jugador;
    private SpriteRenderer spriteTeclaERenderer;
    private GameObject teclaEObj;
    private bool estaMirando = false;
    private bool interfazAbierta = false;
    private Rigidbody2D rbJugador;
    private Vector2 velocidadAntesDeBloquear;

    private void Start()
    {
        // Buscar jugador automáticamente
        jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            rbJugador = jugador.GetComponent<Rigidbody2D>();
        }

        // Configurar collider como trigger
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // Ocultar panel al inicio
        if (panelInteractivo != null)
        {
            panelInteractivo.SetActive(false);
        }

        // Crear sistema tecla E
        CrearSistemaTeclaE();
    }

    private void Update()
    {
        if (!interfazAbierta)
        {
            VerificarProximidadJugador();
        }

        ManejarInputInteraccion();

        // Animación tecla E
        if (estaMirando && !interfazAbierta && spriteTeclaERenderer != null && spriteTeclaERenderer.enabled)
        {
            float offsetY = Mathf.Sin(Time.time * velocidadAnimacion) * amplitudAnimacion;
            Vector3 nuevaPosicion = posicionTeclaE + new Vector3(0, offsetY, 0);
            if (teclaEObj != null)
            {
                teclaEObj.transform.localPosition = nuevaPosicion;
            }
        }

        // Cerrar panel con Escape (si está abierto y no se debe mantener)
        if (interfazAbierta && !mantenerPanelAbierto && Input.GetKeyDown(KeyCode.Escape))
        {
            CerrarInterfaz();
        }
    }

    private void CrearSistemaTeclaE()
    {
        if (spriteTeclaE != null)
        {
            teclaEObj = new GameObject("TeclaE_Indicator");
            teclaEObj.transform.SetParent(transform);
            teclaEObj.transform.localPosition = posicionTeclaE;
            teclaEObj.transform.localScale = escalaTeclaE;

            spriteTeclaERenderer = teclaEObj.AddComponent<SpriteRenderer>();
            spriteTeclaERenderer.sprite = spriteTeclaE;
            spriteTeclaERenderer.sortingOrder = 10;
            spriteTeclaERenderer.enabled = false;

            Debug.Log("Tecla E creada correctamente");
        }
        else
        {
            Debug.LogWarning("No hay sprite Tecla E asignado en el inspector");
        }
    }

    private void VerificarProximidadJugador()
    {
        if (jugador == null) return;

        float distancia = Vector2.Distance(jugador.transform.position, transform.position);
        bool nuevoEstadoMirando = (distancia <= distanciaInteraccion);

        if (nuevoEstadoMirando != estaMirando)
        {
            estaMirando = nuevoEstadoMirando;
            MostrarTeclaE(estaMirando);
        }
    }

    private void MostrarTeclaE(bool mostrar)
    {
        if (spriteTeclaERenderer != null)
        {
            spriteTeclaERenderer.enabled = mostrar;
        }
    }

    private void ManejarInputInteraccion()
    {
        if (estaMirando && Input.GetKeyDown(teclaInteraccion))
        {
            if (!interfazAbierta)
            {
                AbrirInterfaz();
            }
            else
            {
                CerrarInterfaz();
            }
        }
    }

    public void AbrirInterfaz()
    {
        interfazAbierta = true;

        if (panelInteractivo != null)
        {
            panelInteractivo.SetActive(true);
        }

        MostrarTeclaE(false);
        BloquearMovimientoJugador(true);

        Debug.Log("Panel ABIERTO - Puedes cerrarlo con E o Escape");
    }

    public void CerrarInterfaz()
    {
        interfazAbierta = false;

        if (panelInteractivo != null)
        {
            panelInteractivo.SetActive(false);
        }

        BloquearMovimientoJugador(false);

        if (estaMirando)
        {
            MostrarTeclaE(true);
        }

        Debug.Log("Panel CERRADO");
    }

    private void BloquearMovimientoJugador(bool bloquear)
    {
        if (bloquear)
        {
            if (rbJugador != null)
            {
                velocidadAntesDeBloquear = rbJugador.linearVelocity;
                rbJugador.linearVelocity = Vector2.zero;
                rbJugador.angularVelocity = 0f;
            }
        }
        else
        {
            if (rbJugador != null)
            {
                rbJugador.linearVelocity = velocidadAntesDeBloquear;
            }
        }

        if (scriptMovimientoJugador != null)
        {
            scriptMovimientoJugador.enabled = !bloquear;
        }

        Debug.Log($"Movimiento del jugador {(bloquear ? "BLOQUEADO" : "DESBLOQUEADO")}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugador = other.gameObject;
            estaMirando = true;
            MostrarTeclaE(true);
            Debug.Log("Jugador entró en el trigger - Tecla E ACTIVADA");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            estaMirando = false;
            MostrarTeclaE(false);

            // Si el jugador se aleja y el panel está abierto, cerrarlo
            if (interfazAbierta)
            {
                CerrarInterfaz();
            }

            Debug.Log("Jugador salió del trigger - Tecla E DESACTIVADA");
        }
    }

    private void OnDestroy()
    {
        if (teclaEObj != null)
        {
            Destroy(teclaEObj);
        }
    }

    // Métodos públicos para control desde otros scripts
    public void AbrirPanel()
    {
        AbrirInterfaz();
    }

    public void CerrarPanel()
    {
        CerrarInterfaz();
    }

    public void TogglePanel()
    {
        if (interfazAbierta)
        {
            CerrarInterfaz();
        }
        else
        {
            AbrirInterfaz();
        }
    }

    [ContextMenu("Debug Estado")]
    public void DebugEstado()
    {
        Debug.Log($"=== DEBUG INTERACTABLE {name} ===");
        Debug.Log($"Jugador cerca: {estaMirando}");
        Debug.Log($"Panel abierto: {interfazAbierta}");
        Debug.Log($"Tecla E visible: {spriteTeclaERenderer != null && spriteTeclaERenderer.enabled}");
        Debug.Log($"Distancia al jugador: {(jugador != null ? Vector2.Distance(jugador.transform.position, transform.position).ToString("F2") : "No encontrado")}");
    }
}