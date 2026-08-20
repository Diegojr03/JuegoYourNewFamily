using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PuzzleBotonesMusicales : MonoBehaviour
{
    [System.Serializable]
    public class ImagenArrastrableConfig
    {
        public Image imagen;
        public RectTransform rectTransform;
        public AudioClip sonidoBoton;
        public bool esCorrecto = false;
        [HideInInspector] public bool colocado = false;
        [HideInInspector] public Vector3 posicionOriginal;
        [HideInInspector] public Color colorOriginal;
        [HideInInspector] public int idCorrecto;
        [HideInInspector] public bool estaSiendoArrastrada = false;
    }

    [System.Serializable]
    public class HuecoArrastreConfig
    {
        public GameObject hueco;
        public BoxCollider2D colliderHueco;
        public int idImagenCorrecta;
        public int idBotonFinalCorrespondiente;
        [HideInInspector] public bool ocupado = false;
        [HideInInspector] public ImagenArrastrableConfig imagenColocada = null;
        [HideInInspector] public Vector3 posicionOriginal;
        [HideInInspector] public Color colorOriginal;
    }

    [Header("CONFIGURACIÓN IMÁGENES ARRASTRABLES")]
    public ImagenArrastrableConfig[] imagenesArrastrables = new ImagenArrastrableConfig[4];

    [Header("CONFIGURACIÓN HUECOS")]
    public HuecoArrastreConfig[] huecosArrastre = new HuecoArrastreConfig[4];

    [Header("CONFIGURACIÓN BOTONES FINALES")]
    public Button[] botonesFinales = new Button[4];
    public AudioClip[] sonidosBotonesFinales = new AudioClip[4];
    public Image[] imagenesBotonesFinales;

    [Header("INTERFAZ Y PANEL")]
    public GameObject panelPuzzle;
    public KeyCode teclaInteraccion = KeyCode.E;
    public float distanciaInteraccion = 2f;
    public GameObject panelInstrucciones;

    [Header("CONFIGURACIÓN TECLA E")]
    public Sprite spriteTeclaE;
    public Vector3 posicionTeclaE = new Vector3(0, 1.5f, 0);
    public Vector3 escalaTeclaE = new Vector3(0.25f, 0.25f, 0.25f);
    public float velocidadAnimacion = 3f;
    public float amplitudAnimacion = 0.1f;

    [Header("CONFIGURACIÓN COLORES")]
    public Color colorHuecoCorrecto = Color.green;
    public Color colorHuecoIncorrecto = Color.red;
    public float tiempoFeedbackColor = 0.5f;

    [Header("SONIDOS")]
    public AudioClip sonidoError;
    public AudioClip sonidoAcierto;
    public AudioClip sonidoColocacion;

    [Header("CONFIGURACIÓN JUGADOR")]
    public MonoBehaviour scriptMovimientoJugador;
    private Rigidbody2D rbJugador;
    private Vector2 velocidadAntesDeBloquear;

    [Header("CONFIGURACIÓN FINALIZACIÓN")]
    public GameObject[] objectsToActivateAfter;
    public GameObject[] objectsToDestroyAfter;
    public bool destroyAfterCompletion = false;

    // Estado del puzzle
    private bool estaMirando = false;
    private bool interfazAbierta = false;
    private bool puzzleCompletado = false;
    private bool faseBotonesFinales = false;
    private GameObject jugador;
    private SpriteRenderer spriteTeclaERenderer;
    private GameObject teclaEObj;

    private ImagenArrastrableConfig imagenSeleccionada = null;
    private AudioSource audioSource;
    private AudioSource musicaSource;
    private bool musicaReproduciendo = false;

    private Canvas canvasPuzzle;
    private Vector3 offsetArrastre;
    private bool arrastrando = false;
    private GameObject objetoArrastrando = null;

    // Botón correcto siempre el elemento 2 (índice 2)
    private const int BOTON_CORRECTO_INDEX = 2;

    // Contador de imágenes colocadas
    private int imagenesColocadas = 0;

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
        OcultarBotonesFinales();

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

        Debug.Log($"Botón correcto será siempre el elemento {BOTON_CORRECTO_INDEX} (tercer botón)");
        imagenesColocadas = 0;
    }

    private void InicializarSistema()
    {
        // Configurar imágenes arrastrables
        for (int i = 0; i < imagenesArrastrables.Length; i++)
        {
            if (imagenesArrastrables[i].imagen != null)
            {
                imagenesArrastrables[i].rectTransform = imagenesArrastrables[i].imagen.GetComponent<RectTransform>();
                imagenesArrastrables[i].posicionOriginal = imagenesArrastrables[i].rectTransform.position;
                imagenesArrastrables[i].colorOriginal = imagenesArrastrables[i].imagen.color;
                imagenesArrastrables[i].idCorrecto = i;
                imagenesArrastrables[i].estaSiendoArrastrada = false;

                AgregarComponentesArrastreConCollider(imagenesArrastrables[i]);
            }
        }

        // Configurar huecos con colliders
        foreach (var hueco in huecosArrastre)
        {
            if (hueco.hueco != null)
            {
                hueco.posicionOriginal = hueco.hueco.transform.position;

                // Guardar color original
                Image img = hueco.hueco.GetComponent<Image>();
                if (img != null)
                {
                    hueco.colorOriginal = img.color;
                }

                // Asegurar que tenga BoxCollider2D
                if (hueco.colliderHueco == null)
                {
                    hueco.colliderHueco = hueco.hueco.GetComponent<BoxCollider2D>();
                    if (hueco.colliderHueco == null)
                    {
                        hueco.colliderHueco = hueco.hueco.AddComponent<BoxCollider2D>();
                    }
                }
                hueco.colliderHueco.isTrigger = true;
            }
        }

        // Configurar botones finales (inicialmente desactivados)
        for (int i = 0; i < botonesFinales.Length; i++)
        {
            if (botonesFinales[i] != null)
            {
                botonesFinales[i].gameObject.SetActive(false);
                int index = i;
                botonesFinales[i].onClick.AddListener(() => ReproducirSonidoBotonFinal(index));
            }
        }

        Debug.Log($"Botón correcto configurado en elemento {BOTON_CORRECTO_INDEX}");
    }

    private void AgregarComponentesArrastreConCollider(ImagenArrastrableConfig imagen)
    {
        // Agregar collider a la imagen para detectar colisiones
        BoxCollider2D collider = imagen.imagen.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = imagen.imagen.gameObject.AddComponent<BoxCollider2D>();
        }
        collider.isTrigger = true;

        // Agregar Rigidbody2D para que pueda ser arrastrada (pero que no afecte físicas)
        Rigidbody2D rb = imagen.imagen.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = imagen.imagen.gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;

        // Agregar Event Trigger para el drag
        EventTrigger trigger = imagen.imagen.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = imagen.imagen.gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();

        // Evento Begin Drag
        EventTrigger.Entry beginDrag = new EventTrigger.Entry();
        beginDrag.eventID = EventTriggerType.BeginDrag;
        beginDrag.callback.AddListener((data) => IniciarArrastreConCollider(imagen, (PointerEventData)data));
        trigger.triggers.Add(beginDrag);

        // Evento Drag
        EventTrigger.Entry drag = new EventTrigger.Entry();
        drag.eventID = EventTriggerType.Drag;
        drag.callback.AddListener((data) => MoverArrastre(imagen, (PointerEventData)data));
        trigger.triggers.Add(drag);

        // Evento End Drag
        EventTrigger.Entry endDrag = new EventTrigger.Entry();
        endDrag.eventID = EventTriggerType.EndDrag;
        endDrag.callback.AddListener((data) => TerminarArrastreConCollider(imagen, (PointerEventData)data));
        trigger.triggers.Add(endDrag);
    }

    private void IniciarArrastreConCollider(ImagenArrastrableConfig imagen, PointerEventData data)
    {
        if (imagen.colocado || faseBotonesFinales) return;

        imagen.estaSiendoArrastrada = true;
        objetoArrastrando = imagen.imagen.gameObject;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            imagen.rectTransform,
            data.position,
            data.pressEventCamera,
            out Vector3 globalMousePos
        );
        offsetArrastre = imagen.rectTransform.position - globalMousePos;
        arrastrando = true;
        imagenSeleccionada = imagen;

        imagen.imagen.transform.SetAsLastSibling();

        Debug.Log($"Comenzando arrastre de: {imagen.imagen.name}");
    }

    private void MoverArrastre(ImagenArrastrableConfig imagen, PointerEventData data)
    {
        if (imagen.colocado || !arrastrando || imagen != imagenSeleccionada) return;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            imagen.rectTransform,
            data.position,
            data.pressEventCamera,
            out Vector3 globalMousePos
        );

        imagen.rectTransform.position = globalMousePos + offsetArrastre;
    }

    private void TerminarArrastreConCollider(ImagenArrastrableConfig imagen, PointerEventData data)
    {
        if (imagen.colocado) return;

        imagen.estaSiendoArrastrada = false;

        // Verificar colisiones con los huecos
        bool colocadoCorrectamente = false;
        bool colisionConAlgunHueco = false;
        HuecoArrastreConfig huecoIncorrecto = null;

        foreach (var hueco in huecosArrastre)
        {
            if (hueco.ocupado) continue;

            // Verificar si la imagen está dentro del collider del hueco
            if (EstaDentroDelCollider(imagen, hueco.colliderHueco))
            {
                colisionConAlgunHueco = true;

                // Verificar si es la imagen correcta
                if (System.Array.IndexOf(imagenesArrastrables, imagen) == hueco.idImagenCorrecta)
                {
                    // Colocación correcta
                    ColocarImagenEnHueco(imagen, hueco);
                    colocadoCorrectamente = true;
                    break;
                }
                else
                {
                    // Guardar referencia al hueco incorrecto para feedback
                    huecoIncorrecto = hueco;
                    break;
                }
            }
        }

        // Si hay un hueco incorrecto, mostrar feedback y volver la imagen a su posición
        if (huecoIncorrecto != null && !colocadoCorrectamente)
        {
            StartCoroutine(FeedbackColocacionIncorrecta(huecoIncorrecto));
            if (sonidoError != null)
                audioSource.PlayOneShot(sonidoError);

            // VOLVER A POSICIÓN ORIGINAL
            imagen.rectTransform.position = imagen.posicionOriginal;
            Debug.Log($"Imagen {imagen.imagen.name} volvió a su posición original (hueco incorrecto)");
        }
        else if (!colocadoCorrectamente && !colisionConAlgunHueco)
        {
            // No está en ningún hueco, volver a posición original
            imagen.rectTransform.position = imagen.posicionOriginal;
            Debug.Log($"Imagen {imagen.imagen.name} volvió a su posición original (fuera de cualquier hueco)");
        }

        // Limpiar estado de arrastre
        arrastrando = false;
        imagenSeleccionada = null;
        objetoArrastrando = null;
        imagen.estaSiendoArrastrada = false;
    }

    private bool EstaDentroDelCollider(ImagenArrastrableConfig imagen, BoxCollider2D collider)
    {
        if (collider == null) return false;

        // Obtener la posición de la imagen en el espacio mundial
        Vector3 imagenPos = imagen.rectTransform.position;

        // Obtener los límites del collider en el espacio mundial
        Bounds bounds = collider.bounds;

        // Verificar si la posición de la imagen está dentro del collider
        return bounds.Contains(imagenPos);
    }

    private void ColocarImagenEnHueco(ImagenArrastrableConfig imagen, HuecoArrastreConfig hueco)
    {
        imagen.colocado = true;
        hueco.ocupado = true;
        hueco.imagenColocada = imagen;
        imagenesColocadas++;

        // Reproducir sonido de colocación
        if (sonidoColocacion != null)
            audioSource.PlayOneShot(sonidoColocacion);

        // Feedback visual de acierto
        StartCoroutine(FeedbackColocacionCorrecta(hueco));

        // Ocultar la imagen (desaparece)
        imagen.imagen.gameObject.SetActive(false);

        // Ocultar el hueco (desaparece)
        hueco.hueco.SetActive(false);
        if (hueco.colliderHueco != null)
            hueco.colliderHueco.enabled = false;

        // Mostrar el botón final correspondiente en esta posición
        int idBotonFinal = hueco.idBotonFinalCorrespondiente;
        if (idBotonFinal < botonesFinales.Length && botonesFinales[idBotonFinal] != null)
        {
            // Posicionar el botón en el lugar donde estaba el hueco
            botonesFinales[idBotonFinal].transform.position = hueco.posicionOriginal;
            botonesFinales[idBotonFinal].gameObject.SetActive(true);
            botonesFinales[idBotonFinal].interactable = true;

            // Configurar el sprite del botón si existe
            if (idBotonFinal < imagenesBotonesFinales.Length && imagenesBotonesFinales[idBotonFinal] != null)
            {
                Image botonImage = botonesFinales[idBotonFinal].GetComponent<Image>();
                if (botonImage != null)
                {
                    botonImage.sprite = imagenesBotonesFinales[idBotonFinal].sprite;
                }
            }

            // Asegurar que el botón sea interactuable
            CanvasGroup canvasGroup = botonesFinales[idBotonFinal].GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = botonesFinales[idBotonFinal].gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            Debug.Log($"Botón final {idBotonFinal} apareció en la posición del hueco");
        }

        Debug.Log($"Imagen {imagen.imagen.name} colocada correctamente. ({imagenesColocadas}/{imagenesArrastrables.Length})");

        // Verificar si todas las imágenes están colocadas
        if (imagenesColocadas >= imagenesArrastrables.Length)
        {
            StartCoroutine(ActivarFaseBotonesFinalesConRetraso());
        }
    }

    private IEnumerator FeedbackColocacionCorrecta(HuecoArrastreConfig hueco)
    {
        Image img = hueco.hueco.GetComponent<Image>();
        if (img != null)
        {
            img.color = colorHuecoCorrecto;
            yield return new WaitForSeconds(0.3f);
            // El hueco desaparecerá después
        }
    }

    private IEnumerator FeedbackColocacionIncorrecta(HuecoArrastreConfig hueco)
    {
        Image img = hueco.hueco.GetComponent<Image>();
        if (img != null)
        {
            // Guardar color original antes de cambiar
            Color colorOriginal = hueco.colorOriginal;

            // Cambiar a rojo
            img.color = colorHuecoIncorrecto;
            Debug.Log($"Hueco {hueco.hueco.name} se puso rojo (imagen incorrecta)");

            // Esperar
            yield return new WaitForSeconds(tiempoFeedbackColor);

            // Restaurar color original
            img.color = colorOriginal;
            Debug.Log($"Hueco {hueco.hueco.name} restauró su color original");
        }
    }

    private IEnumerator ActivarFaseBotonesFinalesConRetraso()
    {
        yield return new WaitForSeconds(0.5f);
        ActivarFaseBotonesFinales();
    }

    private void ActivarFaseBotonesFinales()
    {
        Debug.Log("¡Todas las imágenes colocadas! Fase de botones finales activada.");
        faseBotonesFinales = true;

        if (panelInstrucciones != null)
            panelInstrucciones.SetActive(false);

        // Los botones finales ya están activos individualmente desde ColocarImagenEnHueco
        foreach (var btn in botonesFinales)
        {
            if (btn != null && btn.gameObject.activeSelf)
            {
                btn.interactable = true;
                CanvasGroup canvasGroup = btn.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            }
        }

        Debug.Log("Fase de botones finales activada. Botón correcto: elemento " + BOTON_CORRECTO_INDEX);
    }

    private void OcultarBotonesFinales()
    {
        foreach (var btn in botonesFinales)
        {
            if (btn != null)
                btn.gameObject.SetActive(false);
        }
    }

    private void ReproducirSonidoBotonFinal(int index)
    {
        Debug.Log($"Click en botón final {index}");

        if (musicaReproduciendo)
        {
            musicaSource.Stop();
            musicaReproduciendo = false;
        }

        if (index < sonidosBotonesFinales.Length && sonidosBotonesFinales[index] != null)
        {
            musicaSource.clip = sonidosBotonesFinales[index];
            musicaSource.Play();
            musicaReproduciendo = true;
            Debug.Log($"Reproduciendo sonido: {sonidosBotonesFinales[index].name}");
        }

        if (index == BOTON_CORRECTO_INDEX)
        {
            Debug.Log($"¡Botón correcto (elemento {BOTON_CORRECTO_INDEX})! Completando puzzle...");
            StartCoroutine(CompletarPuzzle());
        }
        else
        {
            if (sonidoError != null)
                audioSource.PlayOneShot(sonidoError);
            Debug.Log($"Botón incorrecto (se esperaba elemento {BOTON_CORRECTO_INDEX}), intenta de nuevo.");
        }
    }

    private IEnumerator CompletarPuzzle()
    {
        Debug.Log("¡Puzzle completado!");

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
            Debug.Log($"Destruyendo objeto del puzzle: {gameObject.name}");
            Destroy(gameObject);
        }

        Debug.Log("Puzzle completado.");
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

        if (panelInstrucciones != null && !faseBotonesFinales)
            panelInstrucciones.SetActive(true);

        Debug.Log("Interfaz abierta - Modo arrastre activado");
    }

    private void CerrarInterfaz()
    {
        interfazAbierta = false;
        OcultarInterfaz();
        BloquearMovimientoJugador(false);

        if (imagenSeleccionada != null && !imagenSeleccionada.colocado)
        {
            imagenSeleccionada.rectTransform.position = imagenSeleccionada.posicionOriginal;
            imagenSeleccionada = null;
            arrastrando = false;
        }

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
    }

    private void MostrarInterfaz()
    {
        if (panelPuzzle != null)
        {
            panelPuzzle.SetActive(true);
            Canvas.ForceUpdateCanvases();
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
        Debug.Log($"Fase botones finales: {faseBotonesFinales}");
        Debug.Log($"Imágenes colocadas: {imagenesColocadas}/{imagenesArrastrables.Length}");
        Debug.Log($"Botón correcto (SIEMPRE): elemento {BOTON_CORRECTO_INDEX}");

        for (int i = 0; i < imagenesArrastrables.Length; i++)
        {
            Debug.Log($"Imagen {i} - Colocada: {imagenesArrastrables[i].colocado} - Activa: {imagenesArrastrables[i].imagen.gameObject.activeSelf}");
        }

        for (int i = 0; i < botonesFinales.Length; i++)
        {
            if (botonesFinales[i] != null)
            {
                Debug.Log($"Botón final {i} - Activo: {botonesFinales[i].gameObject.activeSelf} - Es correcto: {i == BOTON_CORRECTO_INDEX}");
            }
        }
    }

    [ContextMenu("Resetear Puzzle")]
    public void ResetearPuzzle()
    {
        faseBotonesFinales = false;
        puzzleCompletado = false;
        musicaReproduciendo = false;
        musicaSource.Stop();
        imagenesColocadas = 0;

        // Resetear imágenes arrastrables
        foreach (var img in imagenesArrastrables)
        {
            img.colocado = false;
            img.estaSiendoArrastrada = false;
            if (img.imagen != null)
            {
                img.imagen.gameObject.SetActive(true);
                img.imagen.raycastTarget = true;
                img.imagen.color = img.colorOriginal;
                if (img.rectTransform != null)
                {
                    img.rectTransform.position = img.posicionOriginal;
                }

                var trigger = img.imagen.gameObject.GetComponent<EventTrigger>();
                if (trigger != null)
                    trigger.enabled = true;
            }
        }

        // Resetear huecos
        foreach (var hueco in huecosArrastre)
        {
            hueco.ocupado = false;
            hueco.imagenColocada = null;
            if (hueco.hueco != null)
            {
                hueco.hueco.SetActive(true);
                if (hueco.colliderHueco != null)
                    hueco.colliderHueco.enabled = true;
                Image img = hueco.hueco.GetComponent<Image>();
                if (img != null)
                {
                    img.color = hueco.colorOriginal;
                }
            }
        }

        // Ocultar botones finales
        OcultarBotonesFinales();

        if (panelInstrucciones != null)
            panelInstrucciones.SetActive(true);

        imagenSeleccionada = null;
        arrastrando = false;
        objetoArrastrando = null;

        Debug.Log($"Puzzle reseteado completamente. Botón correcto: elemento {BOTON_CORRECTO_INDEX}");
    }
}