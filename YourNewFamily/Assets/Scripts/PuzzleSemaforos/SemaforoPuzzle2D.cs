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

    [Header("Configuración Tecla E")]
    public Sprite spriteTeclaE;
    public Vector2 posicionTeclaE = new Vector2(0f, 1.5f);
    public Vector2 tamañoTeclaE = new Vector2(50f, 50f);
    public bool mostrarTeclaE = true;

    [Header("Referencias")]
    public Camera camaraJugador;

    // Eventos
    public event Action<SemaforoPuzzle2D> OnEstadoCambiado;

    private bool estaMirando = false;
    private bool interfazAbierta = false;
    private bool puzzleCompletado = false;
    private GameObject jugador;
    private GameObject teclaEObj;
    private Image imagenTeclaE;

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

        // Crear el sprite de la tecla E
        CrearTeclaE();
    }

    private void CrearTeclaE()
    {
        // Si ya existe, destruirlo para recrearlo
        if (teclaEObj != null)
        {
            Destroy(teclaEObj);
        }

        if (spriteTeclaE != null && mostrarTeclaE)
        {
            // Crear un nuevo GameObject para la tecla E
            teclaEObj = new GameObject("TeclaE_Visual");
            teclaEObj.transform.SetParent(transform);
            teclaEObj.transform.localPosition = Vector3.zero;

            // Agregar CanvasRenderer y Image
            teclaEObj.AddComponent<CanvasRenderer>();
            imagenTeclaE = teclaEObj.AddComponent<Image>();
            imagenTeclaE.sprite = spriteTeclaE;
            imagenTeclaE.preserveAspect = true;

            // Configurar RectTransform
            RectTransform rectTransform = teclaEObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = tamañoTeclaE;
            rectTransform.anchoredPosition = posicionTeclaE;

            // Ocultar inicialmente
            teclaEObj.SetActive(false);

            Debug.Log($"Tecla E creada en semáforo {name} - Posición: {posicionTeclaE}, Tamaño: {tamañoTeclaE}");
        }
        else
        {
            Debug.LogWarning($"No hay sprite Tecla E asignado en {name} o mostrarTeclaE está desactivado");
        }
    }

    private void Update()
    {
        if (!interfazAbierta && !puzzleCompletado)
        {
            VerificarProximidadJugador();
        }

        ManejarInputInteraccion();

        // Actualizar posición y tamaño en tiempo real (para debugging)
        ActualizarTeclaE();
    }

    private void ActualizarTeclaE()
    {
        if (teclaEObj != null && imagenTeclaE != null)
        {
            RectTransform rectTransform = teclaEObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = tamañoTeclaE;
                rectTransform.anchoredPosition = posicionTeclaE;
            }
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
        if (teclaEObj != null && mostrarTeclaE)
        {
            teclaEObj.SetActive(mostrar && !puzzleCompletado);
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

        Debug.Log($"Interfaz ABIERTA: {gameObject.name}");
    }

    public void CerrarInterfaz()
    {
        interfazAbierta = false;
        OcultarInterfaz();

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

        Debug.Log($"Semáforo {name} desactivado");
    }

    // Método para forzar la recreación de la tecla E (útil si cambias el sprite)
    [ContextMenu("Recrear Tecla E")]
    public void RecrearTeclaE()
    {
        CrearTeclaE();
        Debug.Log("Tecla E recreada");
    }

    // Método para probar la tecla E
    [ContextMenu("Mostrar Tecla E")]
    public void TestMostrarTeclaE()
    {
        MostrarTeclaE(true);
    }

    [ContextMenu("Ocultar Tecla E")]
    public void TestOcultarTeclaE()
    {
        MostrarTeclaE(false);
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
        Debug.Log($"Tecla E - Objeto: {teclaEObj != null}, Activo: {teclaEObj != null && teclaEObj.activeInHierarchy}");
        Debug.Log($"Posición Tecla E: {posicionTeclaE}, Tamaño: {tamañoTeclaE}");
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
}