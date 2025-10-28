using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PuzzleBotonesMusicales : MonoBehaviour
{
    [System.Serializable]
    public class BotonConfig
    {
        public Button boton;
        public Image imagenBoton;
        public AudioClip sonidoBoton;
        public bool esCorrecto = false;
        [HideInInspector] public bool colocado = false;
        [HideInInspector] public bool pulsado = false;
        [HideInInspector] public Vector3 posicionOriginal;
        [HideInInspector] public Color colorOriginal;
        [HideInInspector] public int siblingIndexOriginal; // NUEVO: Guardar orden en jerarqu�a
    }

    [System.Serializable]
    public class HuecoConfig
    {
        public Button hueco;
        public Image imagenHueco;
        public int idBotonCorrecto;
        [HideInInspector] public bool ocupado = false;
        [HideInInspector] public BotonConfig botonColocado = null;
    }

    [Header("CONFIGURACI�N BOTONES Y HUECOS")]
    public BotonConfig[] botones = new BotonConfig[4];
    public HuecoConfig[] huecos = new HuecoConfig[4];

    [Header("INTERFAZ Y PANEL")]
    public GameObject panelPuzzle;
    public KeyCode teclaInteraccion = KeyCode.E;
    public float distanciaInteraccion = 2f;

    [Header("CONFIGURACI�N TECLA E")]
    public Sprite spriteTeclaE;
    public Vector3 posicionTeclaE = new Vector3(0, 1.5f, 0);
    public Vector3 escalaTeclaE = new Vector3(0.25f, 0.25f, 0.25f);
    public float velocidadAnimacion = 3f;
    public float amplitudAnimacion = 0.1f;

    [Header("CONFIGURACI�N COLORES")]
    [Range(0f, 1f)]
    public float factorOscurecimiento = 0.3f;

    [Header("SONIDOS")]
    public AudioClip sonidoError;
    public AudioClip sonidoAcierto;
    public AudioClip sonidoColocacion;

    [Header("CONFIGURACI�N JUGADOR")]
    public MonoBehaviour scriptMovimientoJugador;
    private Rigidbody2D rbJugador;
    private Vector2 velocidadAntesDeBloquear;

    [Header("CONFIGURACI�N UI")] // NUEVO: Secci�n para configuraci�n UI
    public bool traerBotonesAlFrenteEnFaseFinal = true;

    [Header("CONFIGURACIÓN FINALIZACIÓN")] // NUEVO: Sección para configuración al terminar
    public GameObject[] objectsToActivateAfter; // Objetos a activar después de completar el puzzle
    public GameObject[] objectsToDestroyAfter;  // Objetos a destruir después de completar el puzzle
    public bool destroyAfterCompletion = false; // Si se debe destruir este objeto al terminar

    private bool estaMirando = false;
    private bool interfazAbierta = false;
    private bool puzzleCompletado = false;
    private GameObject jugador;
    private SpriteRenderer spriteTeclaERenderer;
    private GameObject teclaEObj;

    private BotonConfig botonSeleccionado = null;
    private AudioSource audioSource;
    private AudioSource musicaSource;
    private bool musicaReproduciendo = false;

    private Canvas canvasPuzzle;
    private bool faseFinalActivada = false;

    private void Start()
    {
        if (panelPuzzle != null)
        {
            canvasPuzzle = panelPuzzle.GetComponentInParent<Canvas>();
            if (canvasPuzzle == null)
            {
                canvasPuzzle = panelPuzzle.GetComponent<Canvas>();
            }
        }

        InicializarSistema();
        OcultarInterfaz();

        if (jugador == null)
            jugador = GameObject.FindGameObjectWithTag("Player");

        if (jugador != null)
        {
            rbJugador = jugador.GetComponent<Rigidbody2D>();
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        musicaSource = gameObject.AddComponent<AudioSource>();
        musicaSource.loop = false;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        CrearSistemaTeclaE();
    }

    private void InicializarSistema()
    {
        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i].boton != null)
            {
                botones[i].posicionOriginal = botones[i].boton.transform.position;

                if (botones[i].imagenBoton != null)
                {
                    botones[i].colorOriginal = botones[i].imagenBoton.color;
                }

                // NUEVO: Guardar orden original en jerarqu�a
                botones[i].siblingIndexOriginal = botones[i].boton.transform.GetSiblingIndex();

                Button btn = botones[i].boton;
                btn.onClick.RemoveAllListeners();

                int index = i;
                btn.onClick.AddListener(() => SeleccionarBoton(botones[index]));

                btn.interactable = true;

                // NUEVO: Configurar componentes de UI correctamente
                ConfigurarComponentesUI(btn.gameObject);

                ActualizarAparienciaBoton(botones[i]);
            }
        }

        for (int i = 0; i < huecos.Length; i++)
        {
            if (huecos[i].hueco != null)
            {
                Button btn = huecos[i].hueco;
                btn.onClick.RemoveAllListeners();

                int index = i;
                btn.onClick.AddListener(() => IntentarColocarBoton(huecos[index]));

                btn.interactable = true;

                // NUEVO: Configurar componentes de UI para huecos tambi�n
                ConfigurarComponentesUI(btn.gameObject);
            }
        }
    }

    // NUEVO: M�todo para configurar componentes UI correctamente
    private void ConfigurarComponentesUI(GameObject uiObject)
    {
        CanvasGroup canvasGroup = uiObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Asegurar que tenga Image component
        Image image = uiObject.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }
    }

    private void Update()
    {
        if (!interfazAbierta && !puzzleCompletado)
        {
            VerificarProximidadJugador();
        }

        ManejarInputInteraccion();

        if (estaMirando && !interfazAbierta && !puzzleCompletado && spriteTeclaERenderer != null && spriteTeclaERenderer.enabled)
        {
            float offsetY = Mathf.Sin(Time.time * velocidadAnimacion) * amplitudAnimacion;
            Vector3 nuevaPosicion = posicionTeclaE + new Vector3(0, offsetY, 0);
            if (teclaEObj != null)
            {
                teclaEObj.transform.localPosition = nuevaPosicion;
            }
        }

        if (interfazAbierta && VerificarTodosBotonesColocados() && !faseFinalActivada && !puzzleCompletado)
        {
            ActivarFaseFinal();
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

    private void AbrirInterfaz()
    {
        if (puzzleCompletado) return;

        interfazAbierta = true;
        MostrarInterfaz();
        MostrarTeclaE(false);

        BloquearMovimientoJugador(true);

        if (canvasPuzzle != null)
        {
            canvasPuzzle.sortingOrder = 100;
        }

        DeseleccionarBoton();

        Debug.Log("Interfaz abierta - Botones deber�an ser clickables");
    }

    private void CerrarInterfaz()
    {
        interfazAbierta = false;
        OcultarInterfaz();
        BloquearMovimientoJugador(false);
        DeseleccionarBoton();

        if (estaMirando && !puzzleCompletado)
        {
            MostrarTeclaE(true);
        }
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
            Canvas.ForceUpdateCanvases();

            // NUEVO: Forzar rebuild de todos los layouts
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelPuzzle.GetComponent<RectTransform>());
        }
    }

    private void OcultarInterfaz()
    {
        if (panelPuzzle != null)
        {
            panelPuzzle.SetActive(false);
        }
    }

    private void SeleccionarBoton(BotonConfig boton)
    {
        if (boton.colocado || faseFinalActivada)
        {
            return;
        }

        DeseleccionarBoton();

        botonSeleccionado = boton;
        boton.pulsado = true;
        ActualizarAparienciaBoton(boton);

        Debug.Log($"Bot�n seleccionado: {boton.boton.name}");
    }

    private void DeseleccionarBoton()
    {
        if (botonSeleccionado != null)
        {
            botonSeleccionado.pulsado = false;
            ActualizarAparienciaBoton(botonSeleccionado);
            botonSeleccionado = null;
        }
    }

    private void IntentarColocarBoton(HuecoConfig hueco)
    {
        if (faseFinalActivada) return;

        if (botonSeleccionado == null)
        {
            Debug.Log("No hay bot�n seleccionado");
            return;
        }

        if (hueco.ocupado)
        {
            Debug.Log("Hueco ya ocupado");
            return;
        }

        int indexBotonSeleccionado = System.Array.IndexOf(botones, botonSeleccionado);

        Debug.Log($"Intentando colocar bot�n {indexBotonSeleccionado} en hueco que espera {hueco.idBotonCorrecto}");

        if (indexBotonSeleccionado == hueco.idBotonCorrecto)
        {
            ColocarBotonEnHueco(botonSeleccionado, hueco);
            if (sonidoColocacion != null)
                audioSource.PlayOneShot(sonidoColocacion);
        }
        else
        {
            if (sonidoError != null)
                audioSource.PlayOneShot(sonidoError);
            Debug.Log("�Hueco incorrecto!");
        }

        DeseleccionarBoton();
    }

    private void ColocarBotonEnHueco(BotonConfig boton, HuecoConfig hueco)
    {
        boton.colocado = true;
        hueco.ocupado = true;
        hueco.botonColocado = boton;

        // NUEVO: Mover el bot�n como hijo del hueco para mejor organizaci�n
        boton.boton.transform.SetParent(hueco.hueco.transform.parent, false);
        boton.boton.transform.position = hueco.hueco.transform.position;

        hueco.hueco.interactable = false;

        ActualizarAparienciaBoton(boton);
        Debug.Log($"Bot�n {boton.boton.name} colocado correctamente");
    }

    private bool VerificarTodosBotonesColocados()
    {
        foreach (BotonConfig boton in botones)
        {
            if (!boton.colocado)
                return false;
        }
        return true;
    }

    private void ActivarFaseFinal()
    {
        Debug.Log("�Todos los botones colocados! Fase final activada.");
        faseFinalActivada = true;

        foreach (BotonConfig boton in botones)
        {
            if (boton.boton != null)
            {
                // NUEVO: Limpiar completamente y reconfigurar
                boton.boton.onClick.RemoveAllListeners();

                int index = System.Array.IndexOf(botones, boton);
                boton.boton.onClick.AddListener(() => ReproducirSonidoBoton(botones[index]));

                // NUEVO: Asegurar que el bot�n est� completamente interactuable
                boton.boton.interactable = true;

                // NUEVO: Traer botones al frente si est� configurado
                if (traerBotonesAlFrenteEnFaseFinal)
                {
                    boton.boton.transform.SetAsLastSibling();
                }

                // NUEVO: Forzar actualizaci�n de componentes UI
                ConfigurarComponentesUI(boton.boton.gameObject);

                // NUEVO: Actualizar layout inmediatamente
                LayoutRebuilder.ForceRebuildLayoutImmediate(boton.boton.GetComponent<RectTransform>());
            }
        }

        foreach (HuecoConfig hueco in huecos)
        {
            if (hueco.hueco != null)
            {
                hueco.hueco.interactable = false;
            }
        }

        foreach (BotonConfig boton in botones)
        {
            ActualizarAparienciaBoton(boton);
        }

        Debug.Log("Fase final activada - Los botones ahora reproducen sonidos");

        // NUEVO: Debug adicional para verificar estado
        DebugBotonesInteractuables();
    }

    // NUEVO: M�todo para debuguear el estado de los botones
    private void DebugBotonesInteractuables()
    {
        foreach (BotonConfig boton in botones)
        {
            if (boton.boton != null)
            {
                CanvasGroup canvasGroup = boton.boton.GetComponent<CanvasGroup>();
                Debug.Log($"Bot�n {boton.boton.name} - Interactable: {boton.boton.interactable}, " +
                         $"CanvasGroup Alpha: {(canvasGroup != null ? canvasGroup.alpha : "N/A")}, " +
                         $"BlocksRaycasts: {(canvasGroup != null ? canvasGroup.blocksRaycasts : "N/A")}");
            }
        }
    }

    private void ReproducirSonidoBoton(BotonConfig boton)
    {
        Debug.Log($"Click recibido en bot�n: {boton.boton.name}");

        if (musicaReproduciendo)
        {
            musicaSource.Stop();
        }

        if (boton.sonidoBoton != null)
        {
            musicaSource.clip = boton.sonidoBoton;
            musicaSource.Play();
            musicaReproduciendo = true;
            Debug.Log($"Reproduciendo sonido: {boton.sonidoBoton.name}");
        }

        if (boton.esCorrecto)
        {
            Debug.Log("�Bot�n correcto detectado! Completando puzzle...");
            StartCoroutine(CompletarPuzzle());
        }
    }

    private IEnumerator CompletarPuzzle()
    {
        Debug.Log("¡Botón correcto! Puzzle completado.");

        if (sonidoAcierto != null)
        {
            audioSource.PlayOneShot(sonidoAcierto);
        }

        yield return new WaitForSeconds(0.5f);

        interfazAbierta = false;
        OcultarInterfaz();
        BloquearMovimientoJugador(false);
        puzzleCompletado = true;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        MostrarTeclaE(false);
        if (teclaEObj != null)
        {
            Destroy(teclaEObj);
        }

        // NUEVO: Activar y destruir objetos configurados
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

        // NUEVO: Destruir este objeto si está configurado
        if (destroyAfterCompletion)
        {
            Debug.Log($"Destruyendo objeto del puzzle: {gameObject.name}");
            Destroy(gameObject);
        }

        Debug.Log("Puzzle completado. La música continúa reproduciéndose.");
    }

    private void ActualizarAparienciaBoton(BotonConfig boton)
    {
        if (boton.imagenBoton != null)
        {
            if (boton.colocado)
            {
                boton.imagenBoton.color = boton.colorOriginal;
            }
            else if (boton.pulsado)
            {
                Color colorOscurecido = boton.colorOriginal * (1f - factorOscurecimiento);
                colorOscurecido.a = boton.colorOriginal.a;
                boton.imagenBoton.color = colorOscurecido;
            }
            else
            {
                boton.imagenBoton.color = boton.colorOriginal;
            }
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
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            estaMirando = false;
            MostrarTeclaE(false);
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
        Debug.Log($"=== DEBUG PUZZLE {name} ===");
        Debug.Log($"Interfaz abierta: {interfazAbierta}");
        Debug.Log($"Puzzle completado: {puzzleCompletado}");
        Debug.Log($"Fase final activada: {faseFinalActivada}");

        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i].boton != null)
            {
                CanvasGroup canvasGroup = botones[i].boton.GetComponent<CanvasGroup>();
                Debug.Log($"Bot�n {i} ({botones[i].boton.name}) - " +
                         $"Colocado: {botones[i].colocado}, " +
                         $"Interactable: {botones[i].boton.interactable}, " +
                         $"RaycastTarget: {(botones[i].imagenBoton != null ? botones[i].imagenBoton.raycastTarget : "N/A")}");
            }
        }
    }

    [ContextMenu("Forzar Fase Final")]
    public void ForzarFaseFinal()
    {
        foreach (var boton in botones)
        {
            boton.colocado = true;
        }
        ActivarFaseFinal();
    }

    // NUEVO: M�todo para resetear completamente los botones (�til para testing)
    [ContextMenu("Resetear Puzzle")]
    public void ResetearPuzzle()
    {
        faseFinalActivada = false;
        puzzleCompletado = false;

        foreach (var boton in botones)
        {
            boton.colocado = false;
            boton.pulsado = false;
            if (boton.boton != null)
            {
                boton.boton.transform.position = boton.posicionOriginal;
                boton.boton.transform.SetSiblingIndex(boton.siblingIndexOriginal);
                boton.boton.interactable = true;
            }
        }

        foreach (var hueco in huecos)
        {
            hueco.ocupado = false;
            hueco.botonColocado = null;
            if (hueco.hueco != null)
            {
                hueco.hueco.interactable = true;
            }
        }

        DeseleccionarBoton();
        Debug.Log("Puzzle reseteado completamente");
    }
}