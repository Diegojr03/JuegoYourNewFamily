using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class PuzzleFlechasTipos : MonoBehaviour
{
    [System.Serializable]
    public class TipoSprites
    {
        public string nombre = "Tipo";
        public Image[] objetosConImagen = new Image[3]; // 3 objetos con componente Image
        public Sprite[] spritesBackup = new Sprite[3]; // Backup opcional si no quieres usar objetos
    }

    [Header("CONFIGURACIÓN TIPOS")]
    public TipoSprites[] tipos = new TipoSprites[5]; // 5 tipos

    [Header("UI ELEMENTOS")]
    public GameObject panelPuzzle;
    public Button botonFlechaIzquierda;
    public Button botonFlechaDerecha;
    public Button botonComprobar;

    [Header("CONFIGURACIÓN TECLA E")]
    public Sprite spriteTeclaE;
    public Vector3 posicionTeclaE = new Vector3(0, 1.5f, 0);
    public Vector3 escalaTeclaE = new Vector3(0.25f, 0.25f, 0.25f);

    [Header("CONFIGURACIÓN INTERACCIÓN")]
    public KeyCode teclaInteraccion = KeyCode.E;
    public float distanciaInteraccion = 2f;

    [Header("CONFIGURACIÓN JUGADOR")]
    public MonoBehaviour scriptMovimientoJugador;

    [Header("COMBINACIÓN CORRECTA")]
    public int tipoCorrecto = 0; // Índice del tipo correcto (0-4)

    [Header("FEEDBACK")]
    public AudioClip sonidoFlecha;
    public AudioClip sonidoCorrecto;
    public AudioClip sonidoIncorrecto;
    public AudioClip sonidoCompletado;
    public ParticleSystem particulasCompletado;

    [Header("MENSAJES")]
    public GameObject panelMensaje;
    public TextMeshProUGUI textoMensaje;
    public string mensajeCompletado = "¡Puzzle completado!";
    public string mensajeIncorrecto = "Tipo incorrecto";
    public float tiempoMostrarMensaje = 3f;
    public float delayAntesDeMensaje = 0.5f;

    [Header("OBJETOS AL COMPLETAR")]
    public GameObject[] objectsToActivateAfter;
    public GameObject[] objectsToDestroyAfter;
    public bool destroyAfterCompletion = false;

    [Header("DEBUG")]
    public TextMeshProUGUI textoDebugTipo; // Opcional: para mostrar debug del tipo actual

    // Variables privadas
    private bool estaMirando = false;
    private bool interfazAbierta = false;
    private bool puzzleCompletado = false;
    private GameObject jugador;
    private SpriteRenderer spriteTeclaERenderer;
    private GameObject teclaEObj;
    private AudioSource audioSource;
    private Rigidbody2D rbJugador;
    private Vector2 velocidadAntesDeBloquear;
    private int tipoActual = 0; // Índice del tipo actual (0-4)

    // Eventos
    public event Action OnPuzzleCompletado;

    void Start()
    {
        // Buscar jugador
        jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null) rbJugador = jugador.GetComponent<Rigidbody2D>();

        // Inicializar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Ocultar UI inicialmente
        if (panelPuzzle != null) panelPuzzle.SetActive(false);
        if (panelMensaje != null) panelMensaje.SetActive(false);

        // Crear tecla E
        if (spriteTeclaE != null)
        {
            teclaEObj = new GameObject("TeclaE_Indicator");
            teclaEObj.transform.SetParent(transform);
            teclaEObj.transform.localPosition = posicionTeclaE;
            teclaEObj.transform.localScale = escalaTeclaE;

            spriteTeclaERenderer = teclaEObj.AddComponent<SpriteRenderer>();
            spriteTeclaERenderer.sprite = spriteTeclaE;
            spriteTeclaERenderer.sortingOrder = 999;
            spriteTeclaERenderer.enabled = false;
        }

        // Configurar botones
        if (botonFlechaIzquierda != null) botonFlechaIzquierda.onClick.AddListener(CambiarTipoAnterior);
        if (botonFlechaDerecha != null) botonFlechaDerecha.onClick.AddListener(CambiarTipoSiguiente);
        if (botonComprobar != null) botonComprobar.onClick.AddListener(ComprobarTipo);

        // Configurar collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.isTrigger = true;

        // Inicializar tipo
        tipoActual = 0;
        OcultarTodosLosObjetosImagen();
        ActualizarSpritesTipo();

        Debug.Log("Puzzle Flechas Tipos iniciado");
    }

    void Update()
    {
        // Verificar distancia si no está abierto el puzzle
        if (!interfazAbierta && !puzzleCompletado)
        {
            if (jugador != null)
            {
                float distancia = Vector2.Distance(jugador.transform.position, transform.position);
                estaMirando = distancia <= distanciaInteraccion;

                if (spriteTeclaERenderer != null)
                    spriteTeclaERenderer.enabled = estaMirando && !puzzleCompletado;
            }
        }

        // Tecla E para abrir/cerrar
        if (estaMirando && Input.GetKeyDown(teclaInteraccion) && !puzzleCompletado)
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

        // Escape para cerrar
        if (interfazAbierta && Input.GetKeyDown(KeyCode.Escape))
        {
            CerrarInterfaz();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (puzzleCompletado) return;

        if (other.CompareTag("Player"))
        {
            jugador = other.gameObject;
            estaMirando = true;
            if (spriteTeclaERenderer != null) spriteTeclaERenderer.enabled = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            estaMirando = false;
            if (spriteTeclaERenderer != null) spriteTeclaERenderer.enabled = false;

            if (interfazAbierta)
            {
                CerrarInterfaz();
            }
        }
    }

    // MÉTODOS DE INTERFAZ
    public void AbrirInterfaz()
    {
        if (puzzleCompletado) return;

        interfazAbierta = true;
        if (panelPuzzle != null) panelPuzzle.SetActive(true);
        if (spriteTeclaERenderer != null) spriteTeclaERenderer.enabled = false;
        BloquearMovimientoJugador(true);

        Debug.Log("Puzzle abierto");
    }

    public void CerrarInterfaz()
    {
        interfazAbierta = false;
        if (panelPuzzle != null) panelPuzzle.SetActive(false);
        BloquearMovimientoJugador(false);

        if (estaMirando && !puzzleCompletado && spriteTeclaERenderer != null)
            spriteTeclaERenderer.enabled = true;

        Debug.Log("Puzzle cerrado");
    }

    void BloquearMovimientoJugador(bool bloquear)
    {
        if (rbJugador != null)
        {
            if (bloquear)
            {
                velocidadAntesDeBloquear = rbJugador.linearVelocity;
                rbJugador.linearVelocity = Vector2.zero;
            }
            else
            {
                rbJugador.linearVelocity = velocidadAntesDeBloquear;
            }
        }

        if (scriptMovimientoJugador != null)
            scriptMovimientoJugador.enabled = !bloquear;
    }

    // MÉTODOS DEL PUZZLE
    void OcultarTodosLosObjetosImagen()
    {
        foreach (var tipo in tipos)
        {
            foreach (var imgObj in tipo.objetosConImagen)
            {
                if (imgObj != null) imgObj.gameObject.SetActive(false);
            }
        }
    }

    void ActualizarSpritesTipo()
    {
        OcultarTodosLosObjetosImagen();

        if (tipoActual >= 0 && tipoActual < tipos.Length)
        {
            for (int i = 0; i < 3; i++)
            {
                if (i < tipos[tipoActual].objetosConImagen.Length && tipos[tipoActual].objetosConImagen[i] != null)
                {
                    tipos[tipoActual].objetosConImagen[i].gameObject.SetActive(true);

                    if (i < tipos[tipoActual].spritesBackup.Length && tipos[tipoActual].spritesBackup[i] != null)
                    {
                        tipos[tipoActual].objetosConImagen[i].sprite = tipos[tipoActual].spritesBackup[i];
                    }
                }
            }

            if (textoDebugTipo != null) textoDebugTipo.text = $"Tipo {tipoActual + 1}";
        }
    }

    void CambiarTipoSiguiente()
    {
        tipoActual = (tipoActual + 1) % tipos.Length;
        ActualizarSpritesTipo();
        if (sonidoFlecha != null) audioSource.PlayOneShot(sonidoFlecha);
    }

    void CambiarTipoAnterior()
    {
        tipoActual--;
        if (tipoActual < 0) tipoActual = tipos.Length - 1;
        ActualizarSpritesTipo();
        if (sonidoFlecha != null) audioSource.PlayOneShot(sonidoFlecha);
    }

    void ComprobarTipo()
    {
        if (puzzleCompletado) return;

        if (tipoActual == tipoCorrecto)
        {
            // Correcto
            if (sonidoCorrecto != null) audioSource.PlayOneShot(sonidoCorrecto);
            CompletarPuzzle();
        }
        else
        {
            // Incorrecto
            if (sonidoIncorrecto != null) audioSource.PlayOneShot(sonidoIncorrecto);
            MostrarMensajeIncorrecto();
        }
    }

    void MostrarMensajeIncorrecto()
    {
        StartCoroutine(MostrarMensajeConDelay(mensajeIncorrecto, true));
    }

    void CompletarPuzzle()
    {
        puzzleCompletado = true;
        Debug.Log("¡PUZZLE COMPLETADO!");

        if (sonidoCompletado != null) audioSource.PlayOneShot(sonidoCompletado);
        if (particulasCompletado != null) particulasCompletado.Play();

        CerrarInterfaz();
        StartCoroutine(MostrarMensajeConDelay(mensajeCompletado, false));

        // Desactivar botones
        if (botonFlechaIzquierda != null) botonFlechaIzquierda.interactable = false;
        if (botonFlechaDerecha != null) botonFlechaDerecha.interactable = false;
        if (botonComprobar != null) botonComprobar.interactable = false;

        // Desactivar collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        // Ocultar tecla E
        if (spriteTeclaERenderer != null) spriteTeclaERenderer.enabled = false;

        OnPuzzleCompletado?.Invoke();
    }

    IEnumerator MostrarMensajeConDelay(string mensaje, bool esIncorrecto)
    {
        yield return new WaitForSeconds(delayAntesDeMensaje);

        if (panelMensaje != null && textoMensaje != null)
        {
            textoMensaje.text = mensaje;
            panelMensaje.SetActive(true);

            yield return new WaitForSeconds(tiempoMostrarMensaje);
            panelMensaje.SetActive(false);
        }

        if (!esIncorrecto)
        {
            yield return new WaitForSeconds(0.5f);

            // Activar objetos
            foreach (GameObject obj in objectsToActivateAfter)
                if (obj != null) obj.SetActive(true);

            // Destruir objetos
            foreach (GameObject obj in objectsToDestroyAfter)
                if (obj != null) Destroy(obj);

            // Destruir puzzle
            if (destroyAfterCompletion) Destroy(gameObject);
        }
    }

    // DEBUG
    [ContextMenu("Test Abrir Puzzle")]
    void TestAbrirPuzzle()
    {
        AbrirInterfaz();
    }

    [ContextMenu("Test Cerrar Puzzle")]
    void TestCerrarPuzzle()
    {
        CerrarInterfaz();
    }

    [ContextMenu("Forzar Completar")]
    void ForzarCompletar()
    {
        tipoActual = tipoCorrecto;
        ActualizarSpritesTipo();
        CompletarPuzzle();
    }
}