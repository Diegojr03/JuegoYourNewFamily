using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PuzzlePeluchesSillas : MonoBehaviour
{
    [System.Serializable]
    public enum OrientacionSilla
    {
        Arriba,
        Abajo,
        Izquierda,
        Derecha
    }

    [System.Serializable]
    public class SpritePorOrientacion
    {
        public Sprite spriteArriba;
        public Sprite spriteAbajo;
        public Sprite spriteIzquierda;
        public Sprite spriteDerecha;
    }

    [System.Serializable]
    public class PelucheConfig
    {
        public Button botonPeluche;
        public Image imagenPeluche;
        public Sprite spriteOriginal; // Peluche en su posición original
        public SpritePorOrientacion spritesEnSilla; // Sprites del peluche en cada silla según orientación
        [HideInInspector] public bool colocado = false;
        [HideInInspector] public bool seleccionado = false;
        [HideInInspector] public Vector3 posicionOriginal;
        [HideInInspector] public Vector2 escalaOriginal;
        [HideInInspector] public Vector3 rotacionOriginal;
    }

    [System.Serializable]
    public class SillaConfig
    {
        public Button botonSilla;
        public Image imagenSilla;
        public OrientacionSilla orientacion;
        public Sprite spriteSillaVacia;
        public Sprite spriteSillaOcupadaBase; // Sprite base de silla ocupada (sin peluche)
        [HideInInspector] public bool ocupada = false;
        [HideInInspector] public int pelucheColocadoId = -1;
        [HideInInspector] public Vector2 escalaOriginal;
    }

    [Header("CONFIGURACIÓN PELUCHES Y SILLAS")]
    public PelucheConfig[] peluches = new PelucheConfig[4];
    public SillaConfig[] sillas = new SillaConfig[4];

    [Header("COMBINACIÓN CORRECTA")]
    public int[] combinacionCorrecta = new int[4]; // Ejemplo: [0, 1, 2, 3] donde el índice es silla y valor es peluche

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
    public AudioClip sonidoSeleccion;
    public AudioClip sonidoColocacion;
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

    private PelucheConfig pelucheSeleccionado = null;
    private int[] colocacionesActuales = new int[4]; // -1 significa vacío

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

        // Inicializar posiciones originales
        GuardarPosicionesOriginales();

        // Inicializar colocaciones
        ReiniciarColocaciones();

        Debug.Log("Puzzle de peluches y sillas inicializado");
    }

    private void Update()
    {
        // **SOLO ANIMACIÓN - LA DETECCIÓN ES POR COLLIDER**
        if (estaMirando && !interfazAbierta && !puzzleCompletado && spriteTeclaERenderer != null && spriteTeclaERenderer.enabled)
        {
            float offsetY = Mathf.Sin(Time.time * velocidadAnimacion) * amplitudAnimacion;
            Vector3 nuevaPosicion = posicionTeclaE + new Vector3(0, offsetY, 0);
            if (teclaEObj != null)
            {
                teclaEObj.transform.localPosition = nuevaPosicion;
            }
        }

        // Manejar input de interacción (manteniendo tu lógica original)
        ManejarInputInteraccion();
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
        // Configurar botones de peluches
        for (int i = 0; i < peluches.Length; i++)
        {
            if (peluches[i].botonPeluche != null)
            {
                // Guardar sprite original si no está asignado
                if (peluches[i].imagenPeluche != null && peluches[i].spriteOriginal == null)
                {
                    peluches[i].spriteOriginal = peluches[i].imagenPeluche.sprite;
                }

                int index = i;
                peluches[i].botonPeluche.onClick.AddListener(() => SeleccionarPeluche(index));

                // Configurar CanvasGroup
                ConfigurarCanvasGroup(peluches[i].botonPeluche.gameObject);
            }
        }

        // Configurar botones de sillas
        for (int i = 0; i < sillas.Length; i++)
        {
            if (sillas[i].botonSilla != null)
            {
                sillas[i].escalaOriginal = sillas[i].botonSilla.transform.localScale;

                // Asegurar que la silla muestra el sprite vacío al inicio
                if (sillas[i].imagenSilla != null && sillas[i].spriteSillaVacia != null)
                {
                    sillas[i].imagenSilla.sprite = sillas[i].spriteSillaVacia;
                }

                int index = i;
                sillas[i].botonSilla.onClick.AddListener(() => IntentarColocarEnSilla(index));

                // Configurar CanvasGroup
                ConfigurarCanvasGroup(sillas[i].botonSilla.gameObject);
            }
        }
    }

    private void GuardarPosicionesOriginales()
    {
        for (int i = 0; i < peluches.Length; i++)
        {
            if (peluches[i].botonPeluche != null)
            {
                // Guardar posición original (en coordenadas del RectTransform si es UI)
                RectTransform rectTransform = peluches[i].botonPeluche.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    peluches[i].posicionOriginal = rectTransform.anchoredPosition;
                }
                else
                {
                    peluches[i].posicionOriginal = peluches[i].botonPeluche.transform.localPosition;
                }

                peluches[i].escalaOriginal = peluches[i].botonPeluche.transform.localScale;
                peluches[i].rotacionOriginal = peluches[i].botonPeluche.transform.localEulerAngles;

                Debug.Log($"Peluche {i}: Posición original guardada: {peluches[i].posicionOriginal}");
            }
        }
    }

    private void ConfigurarCanvasGroup(GameObject uiObject)
    {
        CanvasGroup canvasGroup = uiObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void SeleccionarPeluche(int index)
    {
        if (peluches[index].colocado) return;

        // Deseleccionar peluche anterior si existe
        if (pelucheSeleccionado != null)
        {
            pelucheSeleccionado.seleccionado = false;
        }

        // Seleccionar nuevo peluche
        pelucheSeleccionado = peluches[index];
        pelucheSeleccionado.seleccionado = true;

        Debug.Log($"Peluche {index} seleccionado");
    }

    private void IntentarColocarEnSilla(int sillaIndex)
    {
        if (pelucheSeleccionado == null || pelucheSeleccionado.colocado)
        {
            Debug.Log("No hay peluche seleccionado o ya está colocado");
            return;
        }

        if (sillas[sillaIndex].ocupada)
        {
            Debug.Log("Silla ya ocupada");
            return;
        }

        // Colocar peluche en la silla
        ColocarPelucheEnSilla(pelucheSeleccionado, sillaIndex);

        // Verificar si se completaron todas las colocaciones
        if (VerificarTodasColocaciones())
        {
            VerificarCombinacion();
        }
    }

    private void ColocarPelucheEnSilla(PelucheConfig peluche, int sillaIndex)
    {
        int pelucheIndex = System.Array.IndexOf(peluches, peluche);

        // Marcar peluche como colocado
        peluche.colocado = true;
        peluche.seleccionado = false;

        // Marcar silla como ocupada
        sillas[sillaIndex].ocupada = true;
        sillas[sillaIndex].pelucheColocadoId = pelucheIndex;

        // Registrar colocación
        colocacionesActuales[sillaIndex] = pelucheIndex;

        // Obtener el sprite correcto del peluche según la orientación de la silla
        Sprite spritePelucheEnSilla = ObtenerSpritePelucheParaSilla(peluche, sillas[sillaIndex].orientacion);

        // 1. OCULTAR EL PELUCHE ORIGINAL EN EL PANEL
        if (peluche.imagenPeluche != null)
        {
            peluche.imagenPeluche.enabled = false;
        }

        // 2. CAMBIAR EL SPRITE DE LA SILLA
        if (spritePelucheEnSilla != null && sillas[sillaIndex].imagenSilla != null)
        {
            sillas[sillaIndex].imagenSilla.sprite = spritePelucheEnSilla;

            // Mantener la escala original
            if (sillas[sillaIndex].botonSilla != null)
            {
                sillas[sillaIndex].botonSilla.transform.localScale = sillas[sillaIndex].escalaOriginal;
            }

            Debug.Log($"Silla {sillaIndex} cambió a sprite con peluche (orientación: {sillas[sillaIndex].orientacion})");
        }

        // Mover el botón del peluche fuera de la pantalla (pero mantenerlo en el canvas para el reinicio)
        if (peluche.botonPeluche != null)
        {
            // Moverlo a una posición fuera de la vista pero mantenerlo en el panel
            RectTransform rectTransform = peluche.botonPeluche.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Mover fuera de la pantalla pero mantener en la jerarquía del canvas
                rectTransform.anchoredPosition = new Vector2(-1000, -1000);
            }
            else
            {
                peluche.botonPeluche.transform.position = new Vector3(-1000, -1000, 0);
            }
        }

        // Desactivar botón de la silla
        if (sillas[sillaIndex].botonSilla != null)
        {
            sillas[sillaIndex].botonSilla.interactable = false;
        }

        // Desactivar botón del peluche
        if (peluche.botonPeluche != null)
        {
            peluche.botonPeluche.interactable = false;
        }

        // Sonido de colocación
        if (sonidoColocacion != null)
            audioSource.PlayOneShot(sonidoColocacion);

        // Deseleccionar peluche
        pelucheSeleccionado = null;

        Debug.Log($"Peluche {pelucheIndex} colocado en silla {sillaIndex}");
    }

    private Sprite ObtenerSpritePelucheParaSilla(PelucheConfig peluche, OrientacionSilla orientacion)
    {
        if (peluche.spritesEnSilla == null)
        {
            Debug.LogWarning($"No hay sprites configurados para el peluche en diferentes orientaciones");
            return peluche.spriteOriginal;
        }

        switch (orientacion)
        {
            case OrientacionSilla.Arriba:
                return peluche.spritesEnSilla.spriteArriba;
            case OrientacionSilla.Abajo:
                return peluche.spritesEnSilla.spriteAbajo;
            case OrientacionSilla.Izquierda:
                return peluche.spritesEnSilla.spriteIzquierda;
            case OrientacionSilla.Derecha:
                return peluche.spritesEnSilla.spriteDerecha;
            default:
                Debug.LogWarning($"Orientación desconocida: {orientacion}");
                return peluche.spriteOriginal;
        }
    }

    private bool VerificarTodasColocaciones()
    {
        foreach (SillaConfig silla in sillas)
        {
            if (!silla.ocupada)
                return false;
        }
        return true;
    }

    private void VerificarCombinacion()
    {
        bool combinacionEsCorrecta = true;

        for (int i = 0; i < 4; i++)
        {
            if (colocacionesActuales[i] != combinacionCorrecta[i])
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
        Debug.Log("¡PUZZLE DE PELUCHES COMPLETADO!");

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

        // Desactivar collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    // FUNCIÓN ÚNICA PARA MOSTRAR MENSAJES - IGUAL QUE EN TU VERSIÓN ORIGINAL
    private IEnumerator MostrarMensajeConDelay(string mensaje, bool esIncorrecto)
    {
        yield return new WaitForSeconds(delayAntesDeMensaje);

        if (panelMensaje != null && textoMensaje != null)
        {
            textoMensaje.text = mensaje;
            panelMensaje.SetActive(true);
            Debug.Log($"✅ Mensaje MOSTRADO: {mensaje}");

            StartCoroutine(OcultarMensajeDespuesDeTiempo());

            if (esIncorrecto)
            {
                StartCoroutine(ReiniciarDespuesDeMensaje());
            }
            else
            {
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
        yield return new WaitForSeconds(delayAntesDeMensaje + tiempoMostrarMensaje);
        ReiniciarPuzzle();
    }

    private IEnumerator GestionarObjetosDespuesDeMensaje()
    {
        yield return new WaitForSeconds(delayAntesDeMensaje + tiempoMostrarMensaje);

        foreach (GameObject obj in objectsToActivateAfter)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                Debug.Log($"Objeto activado: {obj.name}");
            }
        }

        foreach (GameObject obj in objectsToDestroyAfter)
        {
            if (obj != null)
            {
                Destroy(obj);
                Debug.Log($"Objeto destruido: {obj.name}");
            }
        }

        if (destroyAfterCompletion)
        {
            Debug.Log($"Destruyendo puzzle: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    private void ReiniciarPuzzle()
    {
        // Reiniciar peluches
        foreach (var peluche in peluches)
        {
            peluche.colocado = false;
            peluche.seleccionado = false;

            if (peluche.botonPeluche != null)
            {
                peluche.botonPeluche.interactable = true;

                // Restaurar posición, escala y rotación originales
                RectTransform rectTransform = peluche.botonPeluche.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = (Vector2)peluche.posicionOriginal;
                }
                else
                {
                    peluche.botonPeluche.transform.localPosition = peluche.posicionOriginal;
                }

                peluche.botonPeluche.transform.localScale = peluche.escalaOriginal;
                peluche.botonPeluche.transform.localEulerAngles = peluche.rotacionOriginal;

                // Asegurarnos de que el CanvasGroup esté activo
                CanvasGroup canvasGroup = peluche.botonPeluche.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }

                Debug.Log($"Peluche restaurado a posición: {peluche.posicionOriginal}");
            }

            if (peluche.imagenPeluche != null)
            {
                // IMPORTANTE: Habilitar la imagen del peluche
                peluche.imagenPeluche.enabled = true;

                // Restaurar el sprite original
                if (peluche.spriteOriginal != null)
                {
                    peluche.imagenPeluche.sprite = peluche.spriteOriginal;
                }
                else if (peluche.imagenPeluche.sprite == null)
                {
                    Debug.LogWarning("Peluche no tiene sprite original asignado");
                }
            }
        }

        // Reiniciar sillas
        foreach (var silla in sillas)
        {
            silla.ocupada = false;
            silla.pelucheColocadoId = -1;

            if (silla.botonSilla != null)
            {
                silla.botonSilla.interactable = true;
                silla.botonSilla.transform.localScale = silla.escalaOriginal;

                // Asegurarnos de que el CanvasGroup esté activo
                CanvasGroup canvasGroup = silla.botonSilla.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            }

            if (silla.imagenSilla != null && silla.spriteSillaVacia != null)
            {
                silla.imagenSilla.sprite = silla.spriteSillaVacia;
            }
        }

        // Reiniciar variables
        pelucheSeleccionado = null;
        ReiniciarColocaciones();

        Debug.Log("Puzzle reiniciado - Intenta de nuevo");
    }

    private void ReiniciarColocaciones()
    {
        for (int i = 0; i < 4; i++)
        {
            colocacionesActuales[i] = -1;
        }
    }

    // **MÉTODOS PARA MANEJAR COLLIDER Y TECLA E - COMO EN PUZZLE4BOTONES2D**

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
        if (!puzzleCompletado)
        {
            if (pelucheSeleccionado != null)
            {
                pelucheSeleccionado.seleccionado = false;
                pelucheSeleccionado = null;
            }

            // Opcional: puedes reiniciar completamente si quieres
            // ReiniciarPuzzle();
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

    // **¡ESTOS SON LOS MÉTODOS IMPORTANTES DEL PUZZLE4BOTONES2D!**
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (puzzleCompletado) return;

        if (other.CompareTag("Player"))
        {
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
        Debug.Log($"=== DEBUG PUZZLE PELUCHES {name} ===");
        Debug.Log($"Jugador cerca: {estaMirando}");
        Debug.Log($"Interfaz abierta: {interfazAbierta}");
        Debug.Log($"Puzzle completado: {puzzleCompletado}");
        Debug.Log($"Peluche seleccionado: {(pelucheSeleccionado != null ? "Sí" : "No")}");

        string colocacionesStr = "Colocaciones actuales: [";
        for (int i = 0; i < 4; i++)
        {
            colocacionesStr += colocacionesActuales[i] + (i < 3 ? ", " : "");
        }
        colocacionesStr += "]";
        Debug.Log(colocacionesStr);

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

        ReiniciarPuzzle();

        if (panelPuzzle != null)
            panelPuzzle.SetActive(false);

        if (panelMensaje != null)
            panelMensaje.SetActive(false);

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        MostrarTeclaE(false);

        Debug.Log("Puzzle de peluches reiniciado completamente");
    }
}