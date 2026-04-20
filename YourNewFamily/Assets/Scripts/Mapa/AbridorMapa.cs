using UnityEngine;

public class AbridorMapa : MonoBehaviour
{
    [Header("CONFIGURACIÓN MAPA")]
    public GameObject panelMapa;
    public KeyCode teclaMapa = KeyCode.M;

    [Header("CONFIGURACIÓN JUGADOR")]
    public MonoBehaviour scriptMovimientoJugador;  // Arrastra aquí tu script de movimiento del jugador
    public Rigidbody2D rbJugador;                   // Arrastra el Rigidbody2D del jugador

    [Header("CONFIGURACIÓN VISUAL (opcional)")]
    public Sprite spriteTeclaM;
    public Vector3 posicionTeclaM = new Vector3(0, 1.5f, 0);
    public Vector3 escalaTeclaM = new Vector3(0.25f, 0.25f, 0.25f);
    public float velocidadAnimacion = 3f;
    public float amplitudAnimacion = 0.1f;

    private GameObject jugador;
    private SpriteRenderer spriteTeclaMRenderer;
    private GameObject teclaMObj;
    private bool jugadorEnZona = false;
    private bool mapaAbierto = false;
    private Vector2 velocidadAntesDeBloquear;

    private void Start()
    {
        // Buscar jugador automáticamente si no está asignado
        if (jugador == null)
        {
            jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
            {
                if (rbJugador == null)
                    rbJugador = jugador.GetComponent<Rigidbody2D>();

                // Buscar script de movimiento automáticamente
                if (scriptMovimientoJugador == null)
                {
                    // Intenta encontrar scripts comunes de movimiento
                    MonoBehaviour[] scripts = jugador.GetComponents<MonoBehaviour>();
                    foreach (MonoBehaviour script in scripts)
                    {
                        if (script.GetType().Name.Contains("Movimiento") ||
                            script.GetType().Name.Contains("Movement") ||
                            script.GetType().Name.Contains("PlayerController"))
                        {
                            scriptMovimientoJugador = script;
                            break;
                        }
                    }
                }
            }
        }

        // Configurar collider como trigger (2D)
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        else
        {
            Debug.LogError($"AbridorMapa en {name}: No tiene Collider2D. Agrega un BoxCollider2D y marca Is Trigger");
        }

        // Crear sistema tecla M (opcional)
        if (spriteTeclaM != null)
        {
            CrearSistemaTeclaM();
        }

        // Ocultar mapa al inicio
        if (panelMapa != null)
        {
            panelMapa.SetActive(false);
        }
        else
        {
            Debug.LogError($"AbridorMapa en {name}: No se asignó el panelMapa en el Inspector");
        }
    }

    private void CrearSistemaTeclaM()
    {
        teclaMObj = new GameObject("TeclaM_Indicator");
        teclaMObj.transform.SetParent(transform);
        teclaMObj.transform.localPosition = posicionTeclaM;
        teclaMObj.transform.localScale = escalaTeclaM;

        spriteTeclaMRenderer = teclaMObj.AddComponent<SpriteRenderer>();
        spriteTeclaMRenderer.sprite = spriteTeclaM;
        spriteTeclaMRenderer.sortingOrder = 10;
        spriteTeclaMRenderer.enabled = false;
    }

    private void Update()
    {
        // Animación tecla M
        if (jugadorEnZona && spriteTeclaMRenderer != null && spriteTeclaMRenderer.enabled)
        {
            float offsetY = Mathf.Sin(Time.time * velocidadAnimacion) * amplitudAnimacion;
            Vector3 nuevaPosicion = posicionTeclaM + new Vector3(0, offsetY, 0);
            if (teclaMObj != null)
            {
                teclaMObj.transform.localPosition = nuevaPosicion;
            }
        }

        // Manejar input de la tecla M
        if (jugadorEnZona && Input.GetKeyDown(teclaMapa))
        {
            ToggleMapa();
        }
    }

    private void ToggleMapa()
    {
        if (panelMapa == null) return;

        if (!mapaAbierto)
        {
            AbrirMapa();
        }
        else
        {
            CerrarMapa();
        }
    }

    private void AbrirMapa()
    {
        mapaAbierto = true;
        panelMapa.SetActive(true);
        BloquearMovimientoJugador(true);
        Debug.Log("Mapa ABIERTO - Movimiento bloqueado");
    }

    private void CerrarMapa()
    {
        mapaAbierto = false;
        panelMapa.SetActive(false);
        BloquearMovimientoJugador(false);
        Debug.Log("Mapa CERRADO - Movimiento restaurado");
    }

    private void BloquearMovimientoJugador(bool bloquear)
    {
        if (bloquear)
        {
            // Guardar velocidad actual y detener al jugador
            if (rbJugador != null)
            {
                velocidadAntesDeBloquear = rbJugador.linearVelocity;
                rbJugador.linearVelocity = Vector2.zero;
                rbJugador.angularVelocity = 0f;
            }
        }
        else
        {
            // Restaurar velocidad
            if (rbJugador != null)
            {
                rbJugador.linearVelocity = velocidadAntesDeBloquear;
            }
        }

        // Deshabilitar/habilitar el script de movimiento del jugador
        if (scriptMovimientoJugador != null)
        {
            scriptMovimientoJugador.enabled = !bloquear;
        }

        Debug.Log($"Movimiento del jugador {(bloquear ? "BLOQUEADO" : "DESBLOQUEADO")}");
    }

    // Usando Trigger2D exactamente como en tu script que funciona
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugador = other.gameObject;
            jugadorEnZona = true;

            // Actualizar referencias si no se tenían
            if (rbJugador == null)
                rbJugador = jugador.GetComponent<Rigidbody2D>();

            if (spriteTeclaMRenderer != null)
            {
                spriteTeclaMRenderer.enabled = true;
            }

            Debug.Log("Jugador entró en zona del mapa - Tecla M ACTIVADA");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnZona = false;

            if (spriteTeclaMRenderer != null)
            {
                spriteTeclaMRenderer.enabled = false;
            }

            // Cerrar mapa si estaba abierto y desbloquear movimiento
            if (mapaAbierto)
            {
                CerrarMapa();
            }

            Debug.Log("Jugador salió de zona del mapa - Tecla M DESACTIVADA");
        }
    }

    private void OnDestroy()
    {
        // Asegurar que el movimiento se desbloquea si el objeto se destruye
        if (mapaAbierto)
        {
            BloquearMovimientoJugador(false);
        }

        if (teclaMObj != null)
        {
            Destroy(teclaMObj);
        }
    }

    // Métodos públicos para control externo
    public void ForzarCierreMapa()
    {
        if (mapaAbierto)
        {
            CerrarMapa();
        }
    }
}