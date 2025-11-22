using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class Puzzle4Botones2D : MonoBehaviour
{
    [System.Serializable]
    public class BotonPuzzle
    {
        public Button boton;
        public Image imagen;
        public Sprite spriteNormal;
        public Sprite spritePresionado;
        [HideInInspector] public bool estaPresionado = false;
    }

    [Header("BOTONES DEL PUZZLE")]
    public BotonPuzzle[] botones = new BotonPuzzle[4];

    [Header("COMBINACIÓN CORRECTA")]
    public int[] combinacionCorrecta = new int[4]; // Ejemplo: [0, 1, 2, 3]

    [Header("INTERFAZ Y PANEL")]
    public GameObject panelPuzzle;
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
    public AudioClip sonidoCorrecto;
    public AudioClip sonidoIncorrecto;
    public AudioClip sonidoCompletado;
    public ParticleSystem particulasCompletado;

    [Header("MENSAJES")]
    public GameObject panelMensaje;
    public TextMeshProUGUI textoMensaje;
    public string mensajeCompletado = "¡Puzzle completado!";
    public string mensajeIncorrecto = "Combinación incorrecta";
    public float tiempoMostrarMensaje = 3f;
    public float delayAntesDeMensaje = 0.5f;

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

    private int[] secuenciaActual = new int[4];
    private int indiceSecuencia = 0;

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
        if (panelPuzzle != null)
            panelPuzzle.SetActive(false);

        if (panelMensaje != null)
            panelMensaje.SetActive(false);

        // Configurar collider como trigger
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // Crear sistema tecla E
        CrearSistemaTeclaE();

        // Configurar botones
        ConfigurarBotones();

        // Inicializar secuencia
        ReiniciarSecuencia();

        Debug.Log("Puzzle de 4 botones inicializado - Esperando jugador...");
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
                botones[i].boton.onClick.AddListener(() => PresionarBoton(index));
                Debug.Log($"Botón {i} configurado");
            }
            else
            {
                Debug.LogError($"Botón {i} no asignado en el inspector!");
            }
        }
        ActualizarSprites();
    }

    private void PresionarBoton(int index)
    {
        if (puzzleCompletado || botones[index].estaPresionado) return;

        // Registrar el botón en la secuencia
        secuenciaActual[indiceSecuencia] = index;
        indiceSecuencia++;

        // Marcar botón como presionado
        botones[index].estaPresionado = true;
        ActualizarSprites();

        // Sonido
        if (sonidoBoton != null)
            audioSource.PlayOneShot(sonidoBoton);

        Debug.Log($"Botón {index} presionado - Secuencia: {indiceSecuencia}/4");

        // Verificar si se completó la secuencia
        if (indiceSecuencia >= 4)
        {
            VerificarCombinacion();
        }
    }

    private void VerificarCombinacion()
    {
        bool combinacionEsCorrecta = true;

        for (int i = 0; i < 4; i++)
        {
            if (secuenciaActual[i] != this.combinacionCorrecta[i])
            {
                combinacionEsCorrecta = false;
                break;
            }
        }

        if (combinacionEsCorrecta)
        {
            // Combinación correcta
            if (sonidoCorrecto != null)
                audioSource.PlayOneShot(sonidoCorrecto);

            Debug.Log("¡Combinación CORRECTA!");
            CompletarPuzzle();
        }
        else
        {
            // Combinación incorrecta
            if (sonidoIncorrecto != null)
                audioSource.PlayOneShot(sonidoIncorrecto);

            Debug.Log("Combinación INCORRECTA - Mostrando mensaje...");
            MostrarMensajeIncorrecto();
        }
    }

    private void MostrarMensajeIncorrecto()
    {
        StartCoroutine(MostrarMensajeConDelay(mensajeIncorrecto, true));
    }

    private void CompletarPuzzle()
    {
        puzzleCompletado = true;
        Debug.Log("¡PUZZLE DE 4 BOTONES COMPLETADO!");

        // Sonido
        if (sonidoCompletado != null)
            audioSource.PlayOneShot(sonidoCompletado);

        // Partículas
        if (particulasCompletado != null)
            particulasCompletado.Play();

        // Cerrar interfaz
        CerrarInterfaz();

        // Mostrar mensaje con delay
        StartCoroutine(MostrarMensajeConDelay(mensajeCompletado, false));

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

    // FUNCIÓN ÚNICA PARA MOSTRAR MENSAJES (igual que en semáforos)
    private IEnumerator MostrarMensajeConDelay(string mensaje, bool esIncorrecto)
    {
        // Esperar delay antes de mostrar mensaje
        yield return new WaitForSeconds(delayAntesDeMensaje);

        if (panelMensaje != null && textoMensaje != null)
        {
            textoMensaje.text = mensaje;
            panelMensaje.SetActive(true);
            Debug.Log($"✅ Mensaje MOSTRADO: {mensaje}");

            // Ocultar después del tiempo configurado
            StartCoroutine(OcultarMensajeDespuesDeTiempo());

            // Si es mensaje incorrecto, reiniciar después de ocultar
            if (esIncorrecto)
            {
                StartCoroutine(ReiniciarDespuesDeMensaje());
            }
            else
            {
                // Si es mensaje de completado, gestionar objetos
                StartCoroutine(GestionarObjetosDespuesDeMensaje());
            }
        }
        else
        {
            Debug.LogError("❌ PanelMensaje o TextoMensaje no asignado en el inspector!");
        }
    }

    private IEnumerator OcultarMensajeDespuesDeTiempo()
    {
        yield return new WaitForSeconds(tiempoMostrarMensaje);

        if (panelMensaje != null)
        {
            panelMensaje.SetActive(false);
            Debug.Log("Mensaje ocultado");
        }
    }

    private IEnumerator ReiniciarDespuesDeMensaje()
    {
        // Esperar el tiempo del mensaje + el delay inicial
        yield return new WaitForSeconds(delayAntesDeMensaje + tiempoMostrarMensaje);
        ReiniciarPuzzleInterno();
    }

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

    private void ReiniciarPuzzleInterno()
    {
        // Reiniciar botones
        foreach (var boton in botones)
        {
            boton.estaPresionado = false;
            if (boton.boton != null)
                boton.boton.interactable = true;
        }

        // Reiniciar secuencia
        ReiniciarSecuencia();

        ActualizarSprites();

        Debug.Log("Puzzle reiniciado - Intenta de nuevo");
    }

    private void ReiniciarSecuencia()
    {
        indiceSecuencia = 0;
        for (int i = 0; i < 4; i++)
        {
            secuenciaActual[i] = -1;
        }
    }

    private void ActualizarSprites()
    {
        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i].imagen != null)
            {
                botones[i].imagen.sprite = botones[i].estaPresionado ?
                    botones[i].spritePresionado : botones[i].spriteNormal;
            }
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

        Debug.Log("Interfaz de puzzle ABIERTA");
    }

    public void CerrarInterfaz()
    {
        interfazAbierta = false;
        OcultarInterfaz();
        BloquearMovimientoJugador(false);

        // Reiniciar puzzle si se cierra sin completar
        if (!puzzleCompletado && indiceSecuencia > 0)
        {
            ReiniciarPuzzleInterno();
        }

        if (estaMirando && !puzzleCompletado)
        {
            MostrarTeclaE(true);
        }

        Debug.Log("Interfaz de puzzle CERRADA");
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
        if (panelPuzzle != null)
        {
            panelPuzzle.SetActive(true);
        }
    }

    private void OcultarInterfaz()
    {
        if (panelPuzzle != null)
        {
            panelPuzzle.SetActive(false);
        }
    }

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
        Debug.Log($"=== DEBUG PUZZLE 4 BOTONES {name} ===");
        Debug.Log($"Jugador cerca: {estaMirando}");
        Debug.Log($"Interfaz abierta: {interfazAbierta}");
        Debug.Log($"Puzzle completado: {puzzleCompletado}");
        Debug.Log($"Botones presionados: {indiceSecuencia}/4");
        Debug.Log($"Tecla E visible: {spriteTeclaERenderer != null && spriteTeclaERenderer.enabled}");

        string secuenciaStr = "Secuencia actual: [";
        for (int i = 0; i < indiceSecuencia; i++)
        {
            secuenciaStr += secuenciaActual[i] + (i < indiceSecuencia - 1 ? ", " : "");
        }
        secuenciaStr += "]";
        Debug.Log(secuenciaStr);

        string correctaStr = "Combinación correcta: [";
        for (int i = 0; i < combinacionCorrecta.Length; i++)
        {
            correctaStr += combinacionCorrecta[i] + (i < combinacionCorrecta.Length - 1 ? ", " : "");
        }
        correctaStr += "]";
        Debug.Log(correctaStr);
    }

    [ContextMenu("Reiniciar Puzzle Completamente")]
    public void ReiniciarPuzzleCompletamente()
    {
        puzzleCompletado = false;
        interfazAbierta = false;
        estaMirando = false;

        ReiniciarPuzzleInterno();

        // Cerrar panel
        if (panelPuzzle != null)
            panelPuzzle.SetActive(false);

        // Ocultar mensaje si está visible
        if (panelMensaje != null)
            panelMensaje.SetActive(false);

        // Reactivar collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        // Ocultar tecla E
        MostrarTeclaE(false);

        Debug.Log("Puzzle de 4 botones reiniciado completamente");
    }

    [ContextMenu("Forzar Completar Puzzle")]
    public void ForzarCompletarPuzzle()
    {
        // Simular la combinación correcta
        for (int i = 0; i < 4; i++)
        {
            secuenciaActual[i] = combinacionCorrecta[i];
            if (i < botones.Length)
            {
                botones[combinacionCorrecta[i]].estaPresionado = true;
            }
        }
        indiceSecuencia = 4;

        ActualizarSprites();
        CompletarPuzzle();
        Debug.Log("Puzzle forzado a completarse");
    }
}