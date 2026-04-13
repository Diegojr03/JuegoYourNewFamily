using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LockPuzzle : MonoBehaviour
{
    [System.Serializable]
    public class PuzzleSlot
    {
        [Header("Imágenes del espacio")]
        public Sprite[] imagenesPosibles; // 4 imágenes para este espacio

        [Header("Imagen correcta")]
        public int indiceCorrecto; // 0-3, cuál de las 4 es la correcta

        [Header("Referencias UI")]
        public Image imagenVisual;
        public Button botonArriba;
        public Button botonAbajo;

        [HideInInspector]
        public int indiceActual = 0;

        [HideInInspector]
        public bool estaAnimando = false;
    }

    [Header("Referencias")]
    public GameObject panelPuzzle;
    public InteractableObject2D interactableObject;

    [Header("Espacios del Puzzle")]
    public PuzzleSlot[] slots = new PuzzleSlot[5];

    [Header("Configuración Animación Slot")]
    [Range(0.05f, 0.3f)]
    public float duracionAnimacion = 0.15f;
    [Range(2, 10)]
    public int vueltasSlot = 3;
    [Range(0.01f, 0.1f)]
    public float intervaloCambio = 0.05f;

    [Header("Configuración Efectos")]
    public bool usarEscala = true;
    public float escalaMaxima = 1.3f;
    public bool usarColor = true;
    public Color colorAnimacion = new Color(1f, 0.8f, 0.2f);

    [Header("CONFIGURACIÓN ANIMACIÓN COMPLETADO")]
    public Color colorCompletado = Color.green; // Color del destello al completar
    public float escalaCompletado = 1.2f; // Escala durante el destello

    [Header("VELOCIDAD ANIMACIÓN COMPLETADO")]
    [Range(0.02f, 0.5f)]
    public float duracionDestelloSlot = 0.1f; // Duración del destello por slot (más bajo = más rápido)
    [Range(0f, 0.5f)]
    public float pausaEntreDestellos = 0.1f; // Pausa entre la primera y segunda pasada
    [Range(0.5f, 2f)]
    public float velocidadSegundaPasada = 0.7f; // Multiplicador de velocidad para la segunda pasada (menor = más rápido)
    public bool usarSegundaPasada = true; // Activar/desactivar la segunda pasada de destellos

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
    public string mensajeIncorrecto = "Combinación incorrecta";
    public float tiempoMostrarMensaje = 3f;
    public float delayAntesDeMensaje = 0.5f;

    [Header("OBJETOS AL COMPLETAR")]
    public GameObject[] objectsToActivateAfter;
    public GameObject[] objectsToDestroyAfter;
    public bool destroyAfterCompletion = false;

    [Header("DEBUG")]
    public TextMeshProUGUI textoDebug;

    private bool completado = false;
    private Coroutine[] animacionesActivas;
    private AudioSource audioSource;
    private CanvasGroup panelCanvasGroup; // Para bloquear el panel

    private void Start()
    {
        // Inicializar array de corutinas
        animacionesActivas = new Coroutine[slots.Length];

        // Inicializar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Buscar el InteractableObject2D si no está asignado
        if (interactableObject == null)
            interactableObject = GetComponent<InteractableObject2D>();

        // Configurar CanvasGroup para bloquear el panel
        if (panelPuzzle != null)
        {
            panelCanvasGroup = panelPuzzle.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = panelPuzzle.AddComponent<CanvasGroup>();
        }

        // Configurar todos los slots
        for (int i = 0; i < slots.Length; i++)
        {
            int slotIndex = i;

            // Configurar índice inicial (0 por defecto)
            slots[i].indiceActual = 0;

            // Actualizar la imagen visual
            ActualizarImagenSlot(slotIndex);

            // Configurar botones si existen
            if (slots[i].botonArriba != null)
            {
                slots[i].botonArriba.onClick.RemoveAllListeners();
                slots[i].botonArriba.onClick.AddListener(() => IniciarCambioConAnimacion(slotIndex, 1));
            }

            if (slots[i].botonAbajo != null)
            {
                slots[i].botonAbajo.onClick.RemoveAllListeners();
                slots[i].botonAbajo.onClick.AddListener(() => IniciarCambioConAnimacion(slotIndex, -1));
            }
        }

        // Ocultar UI inicialmente
        if (panelPuzzle != null)
            panelPuzzle.SetActive(false);
        if (panelMensaje != null)
            panelMensaje.SetActive(false);

        // Configurar collider como trigger
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;

        Debug.Log("LockPuzzle iniciado");
    }

    private void ActualizarImagenSlot(int slotIndex)
    {
        if (slots[slotIndex].imagenVisual != null && slots[slotIndex].imagenesPosibles.Length > 0)
        {
            slots[slotIndex].imagenVisual.sprite = slots[slotIndex].imagenesPosibles[slots[slotIndex].indiceActual];
        }
    }

    private void IniciarCambioConAnimacion(int slotIndex, int direccion)
    {
        // No permitir cambios si el puzzle está completado o ya está animando
        if (completado) return;
        if (slots[slotIndex].estaAnimando) return;

        // Reproducir sonido de flecha
        if (sonidoFlecha != null && audioSource != null)
            audioSource.PlayOneShot(sonidoFlecha);

        // Detener animación anterior si existe
        if (animacionesActivas[slotIndex] != null)
            StopCoroutine(animacionesActivas[slotIndex]);

        // Iniciar nueva animación
        animacionesActivas[slotIndex] = StartCoroutine(AnimacionSlotMachine(slotIndex, direccion));
    }

    private IEnumerator AnimacionSlotMachine(int slotIndex, int direccion)
    {
        slots[slotIndex].estaAnimando = true;

        // Deshabilitar botones durante la animación
        if (slots[slotIndex].botonArriba != null)
            slots[slotIndex].botonArriba.interactable = false;
        if (slots[slotIndex].botonAbajo != null)
            slots[slotIndex].botonAbajo.interactable = false;

        // Guardar el índice final (al que vamos a llegar)
        int nuevoIndice = slots[slotIndex].indiceActual + direccion;
        if (nuevoIndice < 0)
            nuevoIndice = slots[slotIndex].imagenesPosibles.Length - 1;
        else if (nuevoIndice >= slots[slotIndex].imagenesPosibles.Length)
            nuevoIndice = 0;

        int indiceFinal = nuevoIndice;

        // Calcular cuántos cambios vamos a hacer (vueltas completas + el cambio final)
        int totalCambios = (slots[slotIndex].imagenesPosibles.Length * vueltasSlot) + 1;

        // Variables para efectos visuales
        RectTransform rectTransform = slots[slotIndex].imagenVisual.GetComponent<RectTransform>();
        Vector3 escalaOriginal = Vector3.one;
        Color colorOriginal = slots[slotIndex].imagenVisual.color;

        // ANIMACIÓN DE ENTRADA (escala y color)
        if (usarEscala && rectTransform != null)
        {
            escalaOriginal = rectTransform.localScale;
            float tiempoEscala = 0f;
            float duracionEscala = duracionAnimacion * 0.2f;
            while (tiempoEscala < duracionEscala)
            {
                tiempoEscala += Time.deltaTime;
                float t = tiempoEscala / duracionEscala;
                float escalaActual = Mathf.Lerp(1f, escalaMaxima, t);
                rectTransform.localScale = escalaOriginal * escalaActual;
                yield return null;
            }
        }

        if (usarColor)
        {
            slots[slotIndex].imagenVisual.color = colorAnimacion;
        }

        // Pequeña pausa antes de empezar a girar
        yield return new WaitForSeconds(0.05f);

        // ANIMACIÓN PRINCIPAL (cambio rápido de imágenes)
        for (int i = 0; i < totalCambios; i++)
        {
            int indiceMostrar;
            if (i < totalCambios - 1)
            {
                int pasosRestantes = totalCambios - i;
                int offset = (direccion > 0) ? i + 1 : -i - 1;
                indiceMostrar = (slots[slotIndex].indiceActual + offset) % slots[slotIndex].imagenesPosibles.Length;
                if (indiceMostrar < 0) indiceMostrar += slots[slotIndex].imagenesPosibles.Length;
            }
            else
            {
                indiceMostrar = indiceFinal;
            }

            slots[slotIndex].imagenVisual.sprite = slots[slotIndex].imagenesPosibles[indiceMostrar];

            if (usarEscala && rectTransform != null && i % 2 == 0)
            {
                float vibracion = 1f + Mathf.Sin(i * 0.5f) * 0.1f;
                rectTransform.localScale = escalaOriginal * vibracion;
            }

            float espera = intervaloCambio;
            if (i > totalCambios - 8)
            {
                int pasosRestantes = totalCambios - i;
                espera = intervaloCambio * (1f + (8 - pasosRestantes) * 0.15f);
            }

            yield return new WaitForSeconds(espera);
        }

        slots[slotIndex].indiceActual = indiceFinal;

        // ANIMACIÓN DE SALIDA
        if (usarColor)
        {
            float tiempoColor = 0f;
            float duracionColor = duracionAnimacion * 0.2f;
            while (tiempoColor < duracionColor)
            {
                tiempoColor += Time.deltaTime;
                float t = tiempoColor / duracionColor;
                slots[slotIndex].imagenVisual.color = Color.Lerp(colorAnimacion, colorOriginal, t);
                yield return null;
            }
            slots[slotIndex].imagenVisual.color = colorOriginal;
        }

        if (usarEscala && rectTransform != null)
        {
            float tiempoEscala = 0f;
            float duracionEscala = duracionAnimacion * 0.2f;
            while (tiempoEscala < duracionEscala)
            {
                tiempoEscala += Time.deltaTime;
                float t = tiempoEscala / duracionEscala;
                float escalaActual = Mathf.Lerp(escalaMaxima, 1f, t);
                rectTransform.localScale = escalaOriginal * escalaActual;
                yield return null;
            }
            rectTransform.localScale = escalaOriginal;
        }

        if (usarEscala && rectTransform != null)
        {
            rectTransform.localScale = escalaOriginal * 0.95f;
            yield return new WaitForSeconds(0.03f);
            rectTransform.localScale = escalaOriginal;
        }

        // Rehabilitar botones
        if (slots[slotIndex].botonArriba != null)
            slots[slotIndex].botonArriba.interactable = true;
        if (slots[slotIndex].botonAbajo != null)
            slots[slotIndex].botonAbajo.interactable = true;

        slots[slotIndex].estaAnimando = false;

        // Verificar si el puzzle está completo después de la animación
        VerificarCompletado();

        yield return new WaitForSeconds(0.05f);
    }

    private void VerificarCompletado()
    {
        bool todosCorrectos = true;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].indiceActual != slots[i].indiceCorrecto)
            {
                todosCorrectos = false;
                break;
            }
        }

        if (todosCorrectos && !completado)
        {
            completado = true;

            // Reproducir sonido de completado
            if (sonidoCompletado != null && audioSource != null)
                audioSource.PlayOneShot(sonidoCompletado);

            // Reproducir partículas
            if (particulasCompletado != null)
                particulasCompletado.Play();

            StartCoroutine(AnimacionCompletado());
        }
    }

    // ANIMACIÓN DE COMPLETADO CON VELOCIDAD CONFIGURABLE
    private IEnumerator AnimacionCompletado()
    {
        Debug.Log("¡PUZZLE COMPLETADO! Combinación correcta encontrada.");

        // BLOQUEAR EL PANEL - No se puede interactuar
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        // Deshabilitar todos los botones de flecha inmediatamente
        foreach (PuzzleSlot slot in slots)
        {
            if (slot.botonArriba != null)
                slot.botonArriba.interactable = false;
            if (slot.botonAbajo != null)
                slot.botonAbajo.interactable = false;
        }

        // PRIMERA PASADA: de izquierda a derecha
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].imagenVisual != null)
            {
                yield return StartCoroutine(DestellarSlot(i));
            }
        }

        // Pausa entre pasadas (configurable)
        if (usarSegundaPasada)
        {
            yield return new WaitForSeconds(pausaEntreDestellos);

            // SEGUNDA PASADA: de derecha a izquierda (más rápida)
            for (int i = slots.Length - 1; i >= 0; i--)
            {
                if (slots[i].imagenVisual != null)
                {
                    // Guardar duración original
                    float duracionOriginal = duracionDestelloSlot;
                    // Aplicar velocidad de segunda pasada
                    duracionDestelloSlot = duracionOriginal * velocidadSegundaPasada;
                    yield return StartCoroutine(DestellarSlot(i));
                    // Restaurar duración original
                    duracionDestelloSlot = duracionOriginal;
                }
            }
        }

        yield return new WaitForSeconds(0.2f);

        // Mostrar mensaje de completado DENTRO del panel (en la posición que ya tienes)
        // y esperar a que termine su tiempo antes de cerrar todo
        yield return StartCoroutine(MostrarMensajeEnPanelYEsperar());

        // Después de que el mensaje desaparezca, cerrar el panel
        if (interactableObject != null)
        {
            interactableObject.CerrarPanel();
        }
        else if (panelPuzzle != null)
        {
            panelPuzzle.SetActive(false);
        }

        // Activar objetos
        foreach (GameObject obj in objectsToActivateAfter)
            if (obj != null) obj.SetActive(true);

        // Destruir objetos
        foreach (GameObject obj in objectsToDestroyAfter)
            if (obj != null) Destroy(obj);

        // Desactivar el collider del puzzle
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        // Destruir puzzle
        if (destroyAfterCompletion)
            Destroy(gameObject);
    }

    // NUEVO MÉTODO: Muestra el mensaje dentro del panel y espera a que desaparezca
    private IEnumerator MostrarMensajeEnPanelYEsperar()
    {
        if (panelMensaje != null && textoMensaje != null)
        {
            textoMensaje.text = mensajeCompletado;
            panelMensaje.SetActive(true);

            // Esperar el tiempo configurado
            yield return new WaitForSeconds(tiempoMostrarMensaje);

            // Ocultar el mensaje
            panelMensaje.SetActive(false);
        }
        else
        {
            // Si no hay panel de mensaje configurado, esperar un momento igual
            yield return new WaitForSeconds(tiempoMostrarMensaje);
        }
    }

    // Corutina para destellar un slot individual
    private IEnumerator DestellarSlot(int slotIndex)
    {
        if (slots[slotIndex].imagenVisual == null) yield break;

        RectTransform rect = slots[slotIndex].imagenVisual.GetComponent<RectTransform>();
        Color colorOriginal = slots[slotIndex].imagenVisual.color;
        Vector3 escalaOriginal = Vector3.one;

        if (rect != null)
        {
            escalaOriginal = rect.localScale;
        }

        // Cambiar a color de completado
        slots[slotIndex].imagenVisual.color = colorCompletado;

        // Escalar el slot
        if (rect != null)
        {
            rect.localScale = escalaOriginal * escalaCompletado;
        }

        // Esperar según la duración configurada
        yield return new WaitForSeconds(duracionDestelloSlot);

        // Volver a escala normal
        if (rect != null)
        {
            rect.localScale = escalaOriginal;
        }

        // Volver al color original
        slots[slotIndex].imagenVisual.color = colorOriginal;
    }

    // MÉTODO PARA MOSTRAR MENSAJE (para mensaje incorrecto)
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
            if (destroyAfterCompletion)
                Destroy(gameObject);
        }
    }

    // Método para mostrar mensaje incorrecto
    public void MostrarMensajeIncorrecto()
    {
        StartCoroutine(MostrarMensajeConDelay(mensajeIncorrecto, true));
    }

    // Método público para reiniciar el puzzle
    public void ReiniciarPuzzle()
    {
        // Detener todas las animaciones activas
        for (int i = 0; i < animacionesActivas.Length; i++)
        {
            if (animacionesActivas[i] != null)
            {
                StopCoroutine(animacionesActivas[i]);
                animacionesActivas[i] = null;
            }
            slots[i].estaAnimando = false;
        }

        completado = false;

        // Restaurar interactividad del panel
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].indiceActual = 0;
            ActualizarImagenSlot(i);

            if (slots[i].botonArriba != null)
                slots[i].botonArriba.interactable = true;
            if (slots[i].botonAbajo != null)
                slots[i].botonAbajo.interactable = true;

            if (slots[i].imagenVisual != null)
            {
                slots[i].imagenVisual.color = Color.white;
                RectTransform rect = slots[i].imagenVisual.GetComponent<RectTransform>();
                if (rect != null)
                    rect.localScale = Vector3.one;
            }
        }

        // Ocultar mensaje si estaba visible
        if (panelMensaje != null)
            panelMensaje.SetActive(false);

        // Reactivar collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = true;

        Debug.Log("Puzzle reiniciado");
    }

    public void OnPanelAbierto()
    {
        Debug.Log("Panel del puzzle abierto");

        // Restaurar interactividad si no está completado
        if (!completado && panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].imagenVisual != null)
            {
                slots[i].imagenVisual.color = Color.white;
                RectTransform rect = slots[i].imagenVisual.GetComponent<RectTransform>();
                if (rect != null)
                    rect.localScale = Vector3.one;
            }
        }
    }

    // MÉTODOS DE DEBUG
    [ContextMenu("Test Forzar Completado")]
    void ForzarCompletado()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].indiceActual = slots[i].indiceCorrecto;
            ActualizarImagenSlot(i);
        }
        VerificarCompletado();
    }

    [ContextMenu("Test Reset Puzzle")]
    void TestResetPuzzle()
    {
        ReiniciarPuzzle();
    }
}