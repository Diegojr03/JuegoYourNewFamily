using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class SemaforoPuzzle2DCompleto : MonoBehaviour
{
    [System.Serializable]
    public class BotonSemaforo
    {
        public Button boton;
        public Image imagen;
        public Sprite spriteApagado;
        public Sprite spriteEncendido;
        public TipoBoton tipoBoton;
        [HideInInspector] public bool estaActivo = false;
    }

    public enum TipoBoton { Arriba, Medio, Abajo }

    [Header("LISTA DE LOS 12 BOTONES")]
    public BotonSemaforo[] botones = new BotonSemaforo[12];

    [Header("CONFIGURACIÓN PUZZLE")]
    public int requeridosArriba = 2;
    public int requeridosMedio = 3;
    public int requeridosAbajo = 1;

    [Header("INTERFAZ Y PANEL")]
    public GameObject panelSemaforos;
    public KeyCode teclaInteraccion = KeyCode.E;
    public float distanciaInteraccion = 2f;

    [Header("CONFIGURACIÓN TECLA E")]
    public Sprite spriteTeclaE;
    public Vector3 posicionTeclaE = new Vector3(0, 1.5f, 0);
    public Vector3 escalaTeclaE = new Vector3(0.25f, 0.25f, 0.25f);
    public float velocidadAnimacion = 3f;
    public float amplitudAnimacion = 0.1f;

    [Header("CONFIGURACIÓN JUGADOR")]
    public MonoBehaviour scriptMovimientoJugador;
    private Rigidbody2D rbJugador;
    private Vector2 velocidadAntesDeBloquear;

    [Header("FEEDBACK")]
    public AudioClip sonidoBoton;
    public AudioClip sonidoCompletado;
    public GameObject puerta;
    public ParticleSystem particulasCompletado;

    [Header("MENSAJE COMPLETADO")]
    public GameObject panelMensaje;
    public TextMeshProUGUI textoMensaje;
    public string mensajeCompletado = "¡Puzzle completado!";
    public float tiempoMostrarMensaje = 3f;
    public float delayAntesDeMensaje = 0.5f; // NUEVO: Delay antes de mostrar mensaje

    [Header("OBJETOS AL COMPLETAR")]
    public GameObject[] objectsToActivateAfter;
    public GameObject[] objectsToDestroyAfter;
    public bool destroyAfterCompletion = false;

    private bool estaMirando = false;
    private bool interfazAbierta = false;
    private bool puzzleCompletado = false;
    private GameObject jugador;
    private SpriteRenderer spriteTeclaERenderer;
    private GameObject teclaEObj;
    private AudioSource audioSource;

    // Eventos
    public event Action OnPuzzleCompletado;

    private void Start()
    {
        // Inicializar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Buscar jugador automáticamente
        jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            rbJugador = jugador.GetComponent<Rigidbody2D>();
        }

        // Ocultar elementos al inicio
        if (panelSemaforos != null)
            panelSemaforos.SetActive(false);

        if (panelMensaje != null)
            panelMensaje.SetActive(false);

        // Configurar collider como trigger (IMPORTANTE)
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // Crear sistema tecla E
        CrearSistemaTeclaE();

        // Configurar botones
        ConfigurarBotones();

        Debug.Log("Puzzle de semáforos inicializado - Esperando jugador...");
    }

    private void Update()
    {
        if (!interfazAbierta && !puzzleCompletado)
        {
            VerificarProximidadJugador();
        }

        ManejarInputInteraccion();

        // Animación tecla E
        if (estaMirando && !interfazAbierta && !puzzleCompletado && spriteTeclaERenderer != null && spriteTeclaERenderer.enabled)
        {
            float offsetY = Mathf.Sin(Time.time * velocidadAnimacion) * amplitudAnimacion;
            Vector3 nuevaPosicion = posicionTeclaE + new Vector3(0, offsetY, 0);
            if (teclaEObj != null)
            {
                teclaEObj.transform.localPosition = nuevaPosicion;
            }
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
            Debug.LogWarning("No hay sprite Tecla E asignado");
        }
    }

    private void ConfigurarBotones()
    {
        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i].boton != null)
            {
                int index = i;
                botones[i].boton.onClick.AddListener(() => ToggleBoton(index));
                Debug.Log($"Botón {i} configurado - Tipo: {botones[i].tipoBoton}");
            }
            else
            {
                Debug.LogError($"Botón {i} no asignado en el inspector!");
            }
        }
        ActualizarSprites();
    }

    private void ToggleBoton(int index)
    {
        if (puzzleCompletado) return;

        botones[index].estaActivo = !botones[index].estaActivo;
        ActualizarSprites();

        // Sonido
        if (sonidoBoton != null)
            audioSource.PlayOneShot(sonidoBoton);

        Debug.Log($"Botón {index} cambiado - Tipo: {botones[index].tipoBoton}, Activo: {botones[index].estaActivo}");

        VerificarPuzzle();
    }

    private void ActualizarSprites()
    {
        foreach (var boton in botones)
        {
            if (boton.imagen != null)
            {
                boton.imagen.sprite = boton.estaActivo ? boton.spriteEncendido : boton.spriteApagado;
            }
        }
    }

    private void VerificarPuzzle()
    {
        int contadorArriba = 0;
        int contadorMedio = 0;
        int contadorAbajo = 0;

        foreach (var boton in botones)
        {
            if (boton.estaActivo)
            {
                switch (boton.tipoBoton)
                {
                    case TipoBoton.Arriba: contadorArriba++; break;
                    case TipoBoton.Medio: contadorMedio++; break;
                    case TipoBoton.Abajo: contadorAbajo++; break;
                }
            }
        }

        bool condicionCumplida = contadorArriba == requeridosArriba &&
                                contadorMedio == requeridosMedio &&
                                contadorAbajo == requeridosAbajo;

        Debug.Log($"ESTADO PUZZLE - Arriba: {contadorArriba}/{requeridosArriba}, " +
                 $"Medio: {contadorMedio}/{requeridosMedio}, " +
                 $"Abajo: {contadorAbajo}/{requeridosAbajo}");

        if (condicionCumplida && !puzzleCompletado)
        {
            CompletarPuzzle();
        }
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
        BloquearMovimientoJugador(true);

        Debug.Log("Interfaz de semáforos ABIERTA");
    }

    public void CerrarInterfaz()
    {
        interfazAbierta = false;
        OcultarInterfaz();
        BloquearMovimientoJugador(false);

        if (estaMirando && !puzzleCompletado)
        {
            MostrarTeclaE(true);
        }

        Debug.Log("Interfaz de semáforos CERRADA");
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

    private void MostrarInterfaz()
    {
        if (panelSemaforos != null)
        {
            panelSemaforos.SetActive(true);
        }
    }

    private void OcultarInterfaz()
    {
        if (panelSemaforos != null)
        {
            panelSemaforos.SetActive(false);
        }
    }

    private void CompletarPuzzle()
    {
        puzzleCompletado = true;
        Debug.Log("¡PUZZLE DE SEMÁFOROS COMPLETADO!");

        // Sonido
        if (sonidoCompletado != null)
            audioSource.PlayOneShot(sonidoCompletado);

        // Partículas
        if (particulasCompletado != null)
            particulasCompletado.Play();

        // Abrir puerta
        if (puerta != null)
        {
            puerta.SetActive(false);
            Debug.Log("Puerta abierta");
        }

        // Cerrar interfaz
        CerrarInterfaz();

        // MOSTRAR MENSAJE CON DELAY DE 0.5 SEGUNDOS
        StartCoroutine(MostrarMensajeConDelay());

        // Desactivar botones
        foreach (var boton in botones)
        {
            if (boton.boton != null)
                boton.boton.interactable = false;
        }

        // Desactivar collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        OnPuzzleCompletado?.Invoke();
    }

    // NUEVO: Mostrar mensaje con delay de 0.5 segundos
    private IEnumerator MostrarMensajeConDelay()
    {
        // Esperar 0.5 segundos antes de mostrar el mensaje
        yield return new WaitForSeconds(delayAntesDeMensaje);

        if (panelMensaje != null && textoMensaje != null)
        {
            textoMensaje.text = mensajeCompletado;
            panelMensaje.SetActive(true);
            Debug.Log("✅ Mensaje de completado MOSTRADO (después de 0.5s)");

            // Ocultar después del tiempo configurado
            StartCoroutine(OcultarMensajeDespuesDeTiempo());
        }
        else
        {
            Debug.LogError("❌ PanelMensaje o TextoMensaje no asignado en el inspector!");
        }

        // Gestionar objetos después de mostrar el mensaje
        StartCoroutine(GestionarObjetosDespuesDeMensaje());
    }

    // NUEVO: Ocultar mensaje después del tiempo configurado
    private IEnumerator OcultarMensajeDespuesDeTiempo()
    {
        yield return new WaitForSeconds(tiempoMostrarMensaje);

        if (panelMensaje != null)
        {
            panelMensaje.SetActive(false);
            Debug.Log("Mensaje de completado ocultado");
        }
    }

    // NUEVO: Gestionar objetos después del mensaje
    private IEnumerator GestionarObjetosDespuesDeMensaje()
    {
        // Esperar el tiempo del mensaje + el delay inicial
        yield return new WaitForSeconds(delayAntesDeMensaje + tiempoMostrarMensaje);

        // Activar objetos
        foreach (GameObject obj in objectsToActivateAfter)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                Debug.Log($"Objeto activado: {obj.name}");
            }
        }

        // Destruir objetos
        foreach (GameObject obj in objectsToDestroyAfter)
        {
            if (obj != null)
            {
                Destroy(obj);
                Debug.Log($"Objeto destruido: {obj.name}");
            }
        }

        // Destruir este objeto si está configurado
        if (destroyAfterCompletion)
        {
            Debug.Log($"Destruyendo puzzle: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    // DETECCIÓN POR TRIGGER - IGUAL QUE TU SCRIPT FUNCIONA
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (puzzleCompletado) return;

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
            Debug.Log("Jugador salió del trigger - Tecla E DESACTIVADA");

            if (interfazAbierta)
            {
                CerrarInterfaz();
            }
        }
    }

    private void OnDestroy()
    {
        if (teclaEObj != null)
        {
            Destroy(teclaEObj);
        }
    }

    [ContextMenu("Debug Estado Puzzle")]
    public void DebugEstadoPuzzle()
    {
        Debug.Log($"=== DEBUG SEMÁFOROS {name} ===");
        Debug.Log($"Jugador cerca: {estaMirando}");
        Debug.Log($"Interfaz abierta: {interfazAbierta}");
        Debug.Log($"Puzzle completado: {puzzleCompletado}");
        Debug.Log($"Tecla E visible: {spriteTeclaERenderer != null && spriteTeclaERenderer.enabled}");

        int arriba = 0, medio = 0, abajo = 0;
        foreach (var boton in botones)
        {
            if (boton.estaActivo)
            {
                switch (boton.tipoBoton)
                {
                    case TipoBoton.Arriba: arriba++; break;
                    case TipoBoton.Medio: medio++; break;
                    case TipoBoton.Abajo: abajo++; break;
                }
            }
        }

        Debug.Log($"Botones activos - Arriba: {arriba}, Medio: {medio}, Abajo: {abajo}");
        Debug.Log($"Requeridos - Arriba: {requeridosArriba}, Medio: {requeridosMedio}, Abajo: {requeridosAbajo}");
    }

    [ContextMenu("Reiniciar Puzzle")]
    public void ReiniciarPuzzle()
    {
        puzzleCompletado = false;
        interfazAbierta = false;
        estaMirando = false;

        // Reiniciar botones
        foreach (var boton in botones)
        {
            boton.estaActivo = false;
            if (boton.boton != null)
                boton.boton.interactable = true;
        }

        ActualizarSprites();

        // Cerrar panel
        if (panelSemaforos != null)
            panelSemaforos.SetActive(false);

        // Ocultar mensaje si está visible
        if (panelMensaje != null)
            panelMensaje.SetActive(false);

        // Reactivar collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        // Reactivar puerta
        if (puerta != null)
            puerta.SetActive(true);

        // Ocultar tecla E
        MostrarTeclaE(false);

        Debug.Log("Puzzle de semáforos reiniciado completamente");
    }

    [ContextMenu("Forzar Completar Puzzle")]
    public void ForzarCompletarPuzzle()
    {
        // Activar la combinación correcta automáticamente
        int activadosArriba = 0;
        int activadosMedio = 0;
        int activadosAbajo = 0;

        foreach (var boton in botones)
        {
            if (activadosArriba < requeridosArriba && boton.tipoBoton == TipoBoton.Arriba)
            {
                boton.estaActivo = true;
                activadosArriba++;
            }
            else if (activadosMedio < requeridosMedio && boton.tipoBoton == TipoBoton.Medio)
            {
                boton.estaActivo = true;
                activadosMedio++;
            }
            else if (activadosAbajo < requeridosAbajo && boton.tipoBoton == TipoBoton.Abajo)
            {
                boton.estaActivo = true;
                activadosAbajo++;
            }
            else
            {
                boton.estaActivo = false;
            }
        }

        ActualizarSprites();
        CompletarPuzzle();
        Debug.Log("Puzzle forzado a completarse");
    }
}