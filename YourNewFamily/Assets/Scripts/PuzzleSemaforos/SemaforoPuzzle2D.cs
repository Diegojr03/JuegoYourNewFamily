using UnityEngine;
using UnityEngine.UI;
using System;

public class SemaforoPuzzle2D : MonoBehaviour
{
    [System.Serializable]
    public class BotonSemaforo
    {
        public Button boton;
        public Image imagen;
        public Sprite spriteApagado;
        public Sprite spriteEncendido;
        [HideInInspector] public bool estaActivo = false;
    }

    [Header("Configuración Botones")]
    public BotonSemaforo botonArriba;
    public BotonSemaforo botonMedio;
    public BotonSemaforo botonAbajo;

    [Header("Interfaz y Interacción")]
    public GameObject interfazSemaforo;
    public float distanciaInteraccion = 2f;
    public KeyCode teclaInteraccion = KeyCode.E;
    public GameObject textoInteraccion;
    public Sprite spriteTeclaE;

    [Header("Configuración Tecla E en Pantalla")]
    public Vector3 posicionTeclaE = new Vector3(0, 1.5f, 0);
    public Vector3 escalaTeclaE = new Vector3(0.25f, 0.25f, 0.25f);
    public float velocidadAnimacion = 3f;
    public float amplitudAnimacion = 0.1f;

    [Header("Referencias")]
    public Camera camaraJugador;

    // Eventos
    public event Action<SemaforoPuzzle2D> OnEstadoCambiado;
    public event Action<bool> OnInterfazAbierta;

    private bool estaMirando = false;
    private bool interfazAbierta = false;
    private bool puzzleCompletado = false;
    private GameObject jugador;
    private SpriteRenderer spriteTeclaERenderer;
    private GameObject teclaEObj;
    private Vector3 ultimaPosicionTeclaE;
    private Vector3 ultimaEscalaTeclaE;
    private Sprite ultimoSpriteTeclaE;

    private void Start()
    {
        ConfigurarBotones();
        OcultarInterfaz();

        if (camaraJugador == null)
            camaraJugador = Camera.main;

        // Buscar jugador automáticamente
        jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador == null)
        {
            Debug.LogWarning("No se encontró objeto con tag 'Player'.");
        }

        if (textoInteraccion != null)
            textoInteraccion.SetActive(false);

        // Configurar collider como trigger
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // Crear el sistema de tecla E similar a las estatuas
        CrearSistemaTeclaE();
    }

    private void CrearSistemaTeclaE()
    {
        // Si ya existe, destruirlo para recrearlo
        if (teclaEObj != null)
        {
            Destroy(teclaEObj);
        }

        if (spriteTeclaE != null)
        {
            // Crear GameObject hijo para la tecla E
            teclaEObj = new GameObject("TeclaE_Indicator");
            teclaEObj.transform.SetParent(transform);
            teclaEObj.transform.localPosition = posicionTeclaE;
            teclaEObj.transform.localScale = escalaTeclaE;

            spriteTeclaERenderer = teclaEObj.AddComponent<SpriteRenderer>();
            spriteTeclaERenderer.sprite = spriteTeclaE;
            spriteTeclaERenderer.sortingOrder = 10;

            // Guardar los valores actuales para detectar cambios
            ultimaPosicionTeclaE = posicionTeclaE;
            ultimaEscalaTeclaE = escalaTeclaE;
            ultimoSpriteTeclaE = spriteTeclaE;

            // Ocultar indicador al inicio
            spriteTeclaERenderer.enabled = false;

            Debug.Log($"Sistema Tecla E creado para {name}");
        }
        else
        {
            Debug.LogWarning($"No hay sprite Tecla E asignado en {name}");
        }
    }

    private void Update()
    {
        if (!interfazAbierta && !puzzleCompletado)
        {
            VerificarProximidadJugador();
        }

        ManejarInputInteraccion();

        // Animación flotante para la tecla E cuando está visible
        if (estaMirando && !interfazAbierta && !puzzleCompletado && spriteTeclaERenderer != null && spriteTeclaERenderer.enabled)
        {
            float offsetY = Mathf.Sin(Time.time * velocidadAnimacion) * amplitudAnimacion;
            Vector3 nuevaPosicion = posicionTeclaE + new Vector3(0, offsetY, 0);
            if (teclaEObj != null)
            {
                teclaEObj.transform.localPosition = nuevaPosicion;
            }
        }

        // Verificar cambios en las variables del inspector
        VerificarCambiosTeclaE();
    }

    private void VerificarCambiosTeclaE()
    {
        // Verificar si cambió la posición
        if (teclaEObj != null && posicionTeclaE != ultimaPosicionTeclaE)
        {
            teclaEObj.transform.localPosition = posicionTeclaE;
            ultimaPosicionTeclaE = posicionTeclaE;
        }

        // Verificar si cambió la escala
        if (teclaEObj != null && escalaTeclaE != ultimaEscalaTeclaE)
        {
            teclaEObj.transform.localScale = escalaTeclaE;
            ultimaEscalaTeclaE = escalaTeclaE;
        }

        // Verificar si cambió el sprite
        if (spriteTeclaERenderer != null && spriteTeclaE != ultimoSpriteTeclaE)
        {
            spriteTeclaERenderer.sprite = spriteTeclaE;
            ultimoSpriteTeclaE = spriteTeclaE;
        }
    }

    private void ConfigurarBotones()
    {
        if (botonArriba.boton != null)
            botonArriba.boton.onClick.AddListener(() => ToggleBoton(botonArriba));

        if (botonMedio.boton != null)
            botonMedio.boton.onClick.AddListener(() => ToggleBoton(botonMedio));

        if (botonAbajo.boton != null)
            botonAbajo.boton.onClick.AddListener(() => ToggleBoton(botonAbajo));

        ActualizarSprites();
    }

    private void ToggleBoton(BotonSemaforo boton)
    {
        if (puzzleCompletado) return;

        boton.estaActivo = !boton.estaActivo;
        ActualizarSprites();
        OnEstadoCambiado?.Invoke(this);
    }

    private void ActualizarSprites()
    {
        if (botonArriba.imagen != null)
            botonArriba.imagen.sprite = botonArriba.estaActivo ? botonArriba.spriteEncendido : botonArriba.spriteApagado;

        if (botonMedio.imagen != null)
            botonMedio.imagen.sprite = botonMedio.estaActivo ? botonMedio.spriteEncendido : botonMedio.spriteApagado;

        if (botonAbajo.imagen != null)
            botonAbajo.imagen.sprite = botonAbajo.estaActivo ? botonAbajo.spriteEncendido : botonAbajo.spriteApagado;
    }

    private void VerificarProximidadJugador()
    {
        if (jugador == null || puzzleCompletado) return;

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
            spriteTeclaERenderer.enabled = mostrar && !puzzleCompletado;
        }

        if (textoInteraccion != null)
        {
            textoInteraccion.SetActive(mostrar && !puzzleCompletado);
        }
    }

    private void ManejarInputInteraccion()
    {
        if (puzzleCompletado) return;

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

        // Cerrar interfaz con ESC también
        if (interfazAbierta && Input.GetKeyDown(KeyCode.Escape))
        {
            CerrarInterfaz();
        }
    }

    public void AbrirInterfaz()
    {
        if (puzzleCompletado) return;

        interfazAbierta = true;
        MostrarInterfaz();
        MostrarTeclaE(false);

        // Notificar que la interfaz se abrió (para bloquear movimiento)
        OnInterfazAbierta?.Invoke(true);

        Debug.Log($"Interfaz ABIERTA: {gameObject.name}");
    }

    public void CerrarInterfaz()
    {
        interfazAbierta = false;
        OcultarInterfaz();

        // Notificar que la interfaz se cerró (para desbloquear movimiento)
        OnInterfazAbierta?.Invoke(false);

        if (estaMirando && !puzzleCompletado)
        {
            MostrarTeclaE(true);
        }

        Debug.Log($"Interfaz CERRADA: {gameObject.name}");
    }

    private void MostrarInterfaz()
    {
        if (interfazSemaforo != null)
        {
            interfazSemaforo.SetActive(true);
        }
    }

    private void OcultarInterfaz()
    {
        if (interfazSemaforo != null)
            interfazSemaforo.SetActive(false);
    }

    public void DesactivarSemaforo()
    {
        puzzleCompletado = true;
        interfazAbierta = false;

        OcultarInterfaz();
        MostrarTeclaE(false);

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // Notificar que la interfaz se cerró
        OnInterfazAbierta?.Invoke(false);

        // Destruir el objeto de la tecla E
        if (teclaEObj != null)
        {
            Destroy(teclaEObj);
        }

        Debug.Log($"Semáforo {name} desactivado");
    }

    // Método para forzar la recreación de la tecla E
    [ContextMenu("Recrear Tecla E")]
    public void RecrearTeclaE()
    {
        CrearSistemaTeclaE();
        Debug.Log("Tecla E recreada");
    }

    // Método para actualizar manualmente la tecla E
    public void ActualizarTeclaE()
    {
        if (teclaEObj != null)
        {
            teclaEObj.transform.localPosition = posicionTeclaE;
            teclaEObj.transform.localScale = escalaTeclaE;
        }
        if (spriteTeclaERenderer != null && spriteTeclaE != null)
        {
            spriteTeclaERenderer.sprite = spriteTeclaE;
        }

        ultimaPosicionTeclaE = posicionTeclaE;
        ultimaEscalaTeclaE = escalaTeclaE;
        ultimoSpriteTeclaE = spriteTeclaE;
    }

    // Métodos públicos para acceder al estado
    public bool GetArribaActivo() => botonArriba.estaActivo;
    public bool GetMedioActivo() => botonMedio.estaActivo;
    public bool GetAbajoActivo() => botonAbajo.estaActivo;
    public bool IsInterfazAbierta() => interfazAbierta;
    public bool IsPuzzleCompletado() => puzzleCompletado;

    [ContextMenu("Debug Estado")]
    public void DebugEstado()
    {
        Debug.Log($"=== DEBUG {name} ===");
        Debug.Log($"Botones - Arriba: {GetArribaActivo()}, Medio: {GetMedioActivo()}, Abajo: {GetAbajoActivo()}");
        Debug.Log($"Estado - Interfaz: {interfazAbierta}, Puzzle: {puzzleCompletado}, Mirando: {estaMirando}");
        Debug.Log($"Tecla E - Renderer: {spriteTeclaERenderer != null}, Activo: {spriteTeclaERenderer != null && spriteTeclaERenderer.enabled}");
        Debug.Log($"Posición: {posicionTeclaE}, Escala: {escalaTeclaE}");
    }

    // Detección cuando el jugador entra en el trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (puzzleCompletado) return;

        if (other.CompareTag("Player"))
        {
            jugador = other.gameObject;
            estaMirando = true;
            MostrarTeclaE(true);
            Debug.Log($"Jugador entró en trigger de {name}");
        }
    }

    // Detección cuando el jugador sale del trigger
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            estaMirando = false;
            MostrarTeclaE(false);
            Debug.Log($"Jugador salió del trigger de {name}");

            if (interfazAbierta)
            {
                CerrarInterfaz();
            }
        }
    }

    private void OnDestroy()
    {
        // Limpiar el objeto cuando se destruya el semáforo
        if (teclaEObj != null)
        {
            Destroy(teclaEObj);
        }
    }
}