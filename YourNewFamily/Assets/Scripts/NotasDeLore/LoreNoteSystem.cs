using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoreNoteSystem : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    public KeyCode interactionKey = KeyCode.F;

    [Header("Contenido de la Nota")]
    public TextAsset noteTextAsset;
    public Sprite noteSprite;

    [Header("Nota Gigante (Visual en Pantalla)")]
    public Transform giantNoteTransform;
    public SpriteRenderer giantSprite;
    public Vector3 giantScale = new Vector3(1.5f, 1.5f, 1f);

    [Range(0f, 1f)]
    public float verticalPositionPercent = 0.5f; // 0.5 = Centro exacto de la pantalla

    public float moveSpeed = 8f;

    [Header("UI del Panel de Lectura")]
    public GameObject noteUIPanel; // Objeto raíz del Canvas
    public CanvasGroup noteCanvasGroup; // Componente CanvasGroup del panel para el Fade
    public Image overlayBackground; // Componente Image del fondo negro

    [Range(0f, 1f)]
    public float backgroundOpacity = 0.5f; // Opacidad máxima del fondo (50%)

    public TextMeshProUGUI noteTextUI; // Texto TMP dentro del ScrollView
    public ScrollRect scrollRect; // Para controlar el ScrollView

    [Header("Efectos Visuales (Animación y Texto)")]
    public float fadeDuration = 0.35f;
    public float typewriterSpeed = 0.012f;

    [Header("Guardado e Identificador")]
    public string noteId = "";

    [Header("Configuración Post-Lectura")]
    public GameObject[] objectsToActivateAfter;
    public GameObject[] objectsToDestroyAfter;
    public bool destroyAfterRead = false;

    // Estados internos
    private Camera mainCamera;
    private MovimientoPersonaje playerMovement;
    private Rigidbody2D playerRigidbody;
    private Vector2 originalVelocity;

    private Vector2 targetPosition;
    private Vector2 hiddenPosition;

    private bool isPlayerInside = false;
    private bool isNoteActive = false;
    private bool isAnimating = false;
    private bool isTyping = false;

    private Coroutine typingCoroutine;


    void Start()
    {
        mainCamera = Camera.main;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerMovement = player.GetComponent<MovimientoPersonaje>();
            playerRigidbody = player.GetComponent<Rigidbody2D>();
        }

        // Si no se asignó CanvasGroup en el Inspector,
        // intentamos obtenerlo o agregarlo
        if (noteUIPanel != null && noteCanvasGroup == null)
        {
            noteCanvasGroup = noteUIPanel.GetComponent<CanvasGroup>();

            if (noteCanvasGroup == null)
                noteCanvasGroup = noteUIPanel.AddComponent<CanvasGroup>();
        }

        if (noteUIPanel != null)
        {
            noteUIPanel.SetActive(false);

            if (noteCanvasGroup != null)
                noteCanvasGroup.alpha = 0f;
        }

        if (giantSprite != null && noteSprite != null)
            giantSprite.sprite = noteSprite;

        CalculatePositions();

        if (giantNoteTransform != null)
            giantNoteTransform.position = hiddenPosition;
    }


    void Update()
    {
        // Al pulsar F dentro del trigger: Abrir nota
        if (isPlayerInside && !isNoteActive && !isAnimating && Input.GetKeyDown(interactionKey))
        {
            StartCoroutine(OpenNoteRoutine());
        }

        // Mientras la nota está activa
        else if (isNoteActive && !isAnimating && Input.GetKeyDown(interactionKey))
        {
            if (isTyping)
            {
                // Si aún se está escribiendo,
                // pulsar F muestra todo el texto al instante
                CompleteTyping();
            }
            else
            {
                // Si ya terminó de escribir,
                // pulsar F cierra la nota
                StartCoroutine(CloseNoteRoutine());
            }
        }
    }


    void CalculatePositions()
    {
        if (mainCamera == null)
            return;

        // Posición en el centro según la vista actual de la cámara
        targetPosition = mainCamera.ViewportToWorldPoint(
            new Vector3(
                0.5f,
                verticalPositionPercent,
                mainCamera.nearClipPlane
            )
        );

        float cameraHeight = mainCamera.orthographicSize * 2f;

        // Posición oculta por debajo de la pantalla
        hiddenPosition = new Vector2(
            targetPosition.x,
            targetPosition.y - cameraHeight * 1.5f
        );
    }


    private IEnumerator OpenNoteRoutine()
    {
        isAnimating = true;

        // Ocultar prompt "F"
        InteractionPromptManager.Instance?.HidePrompt();

        // Bloquear movimiento y físicas del jugador
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerRigidbody != null)
        {
            originalVelocity = playerRigidbody.linearVelocity;
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        // Recalcular posiciones en la ubicación actual de la cámara
        CalculatePositions();

        // Colocar forzosamente la nota abajo ANTES de animar la subida
        if (giantNoteTransform != null)
        {
            giantNoteTransform.position = hiddenPosition;
            giantNoteTransform.localScale = giantScale;

            yield return StartCoroutine(
                MoveNote(targetPosition)
            );
        }

        // Configurar opacidad del fondo
        if (overlayBackground != null)
        {
            Color c = overlayBackground.color;
            c.a = backgroundOpacity;
            overlayBackground.color = c;
        }

        // Reiniciar posición del ScrollView al inicio arriba
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        // Activar UI y hacer el Fade In
        if (noteUIPanel != null)
            noteUIPanel.SetActive(true);

        yield return StartCoroutine(
            FadeCanvasGroup(0f, 1f, fadeDuration)
        );

        isNoteActive = true;
        isAnimating = false;

        // Iniciar el efecto de máquina de escribir
        typingCoroutine = StartCoroutine(
            TypeTextRoutine()
        );
    }


    private IEnumerator CloseNoteRoutine()
    {
        isAnimating = true;

        // Fade Out del panel completo
        // Fondo + Texto
        yield return StartCoroutine(
            FadeCanvasGroup(1f, 0f, fadeDuration)
        );

        if (noteUIPanel != null)
            noteUIPanel.SetActive(false);

        // Animación de bajada de la nota hacia el fondo
        CalculatePositions();

        if (giantNoteTransform != null)
        {
            yield return StartCoroutine(
                MoveNote(hiddenPosition)
            );
        }

        // Registrar lectura en SaveManager
        if (!string.IsNullOrEmpty(noteId) && SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterDialogueCompleted(noteId);
        }

        // Activar objetos pendientes
        foreach (GameObject obj in objectsToActivateAfter)
        {
            if (obj != null)
            {
                SaveableObject saveable =
                    obj.GetComponent<SaveableObject>();

                if (saveable != null && SaveManager.Instance != null)
                {
                    SaveManager.Instance.RegisterObjectState(
                        saveable.objectId,
                        true
                    );
                }

                obj.SetActive(true);
            }
        }

        // Desactivar objetos pendientes
        foreach (GameObject obj in objectsToDestroyAfter)
        {
            if (obj != null)
            {
                SaveableObject saveable =
                    obj.GetComponent<SaveableObject>();

                if (saveable != null && SaveManager.Instance != null)
                {
                    SaveManager.Instance.RegisterObjectState(
                        saveable.objectId,
                        false
                    );
                }

                obj.SetActive(false);
            }
        }

        // Restablecer movimiento del jugador
        if (playerMovement != null)
            playerMovement.enabled = true;

        if (playerRigidbody != null)
            playerRigidbody.linearVelocity = originalVelocity;

        isNoteActive = false;
        isAnimating = false;

        // Destruir o reactivar interacción
        if (destroyAfterRead)
        {
            SaveableObject thisSaveable =
                GetComponent<SaveableObject>();

            if (thisSaveable != null && SaveManager.Instance != null)
            {
                SaveManager.Instance.RegisterObjectState(
                    thisSaveable.objectId,
                    false
                );
            }

            Destroy(gameObject);
        }
        else if (isPlayerInside)
        {
            InteractionPoint point =
                GetComponent<InteractionPoint>();

            if (point != null)
            {
                InteractionPromptManager.Instance?.ShowPrompt(point);
            }
        }
    }


    private IEnumerator MoveNote(Vector2 target)
    {
        while (
            Vector2.Distance(
                giantNoteTransform.position,
                target
            ) > 0.05f
        )
        {
            giantNoteTransform.position =
                Vector2.Lerp(
                    giantNoteTransform.position,
                    target,
                    moveSpeed * Time.deltaTime
                );

            yield return null;
        }

        giantNoteTransform.position = target;
    }


    private IEnumerator FadeCanvasGroup(
        float start,
        float end,
        float duration
    )
    {
        if (noteCanvasGroup == null)
            yield break;

        float counter = 0f;

        while (counter < duration)
        {
            counter += Time.deltaTime;

            noteCanvasGroup.alpha =
                Mathf.Lerp(
                    start,
                    end,
                    counter / duration
                );

            yield return null;
        }

        noteCanvasGroup.alpha = end;
    }


    private IEnumerator TypeTextRoutine()
    {
        if (noteTextUI == null)
            yield break;

        if (noteTextAsset == null)
        {
            Debug.LogWarning(
                "No se ha asignado un archivo de texto a esta nota."
            );

            yield break;
        }

        isTyping = true;

        // Obtener el texto del archivo .txt
        // y añadir dos líneas vacías al final.
        string text = noteTextAsset.text + "\n\n";

        // Poner el texto completo temporalmente
        // para que TMP pueda calcular correctamente
        // su tamaño real.
        noteTextUI.text = text;
        noteTextUI.maxVisibleCharacters = int.MaxValue;

        // Forzar actualización de TMP
        noteTextUI.ForceMeshUpdate();

        // Esperar un frame para que Unity actualice el Layout
        yield return null;

        Canvas.ForceUpdateCanvases();

        // Obtener la altura real que necesita el texto
        float textHeight = noteTextUI.preferredHeight;

        Debug.Log(
            "ALTURA REAL DEL TEXTO: " + textHeight
        );

        // Cambiar la altura del Content
        if (scrollRect != null && scrollRect.content != null)
        {
            RectTransform content = scrollRect.content;

            content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                textHeight
            );

            Canvas.ForceUpdateCanvases();

            Debug.Log(
                "ALTURA FINAL DEL CONTENT: " +
                content.rect.height
            );

            // Empezar arriba
            scrollRect.verticalNormalizedPosition = 1f;
        }

        // Comenzar el efecto de escritura
        noteTextUI.maxVisibleCharacters = 0;

        int totalCharacters =
            noteTextUI.textInfo.characterCount;

        for (int i = 0; i <= totalCharacters; i++)
        {
            noteTextUI.maxVisibleCharacters = i;

            yield return new WaitForSeconds(
                typewriterSpeed
            );
        }

        // Mostrar todo al terminar
        noteTextUI.maxVisibleCharacters = int.MaxValue;

        isTyping = false;

        Canvas.ForceUpdateCanvases();
    }


    private void CompleteTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (noteTextUI != null)
        {
            noteTextUI.ForceMeshUpdate();

            noteTextUI.maxVisibleCharacters =
                noteTextUI.textInfo.characterCount;
        }

        isTyping = false;
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (
            other.CompareTag("Player") &&
            !isNoteActive
        )
        {
            isPlayerInside = true;

            InteractionPoint point =
                GetComponent<InteractionPoint>();

            if (point != null)
            {
                InteractionPromptManager.Instance?.ShowPrompt(
                    point
                );
            }
        }
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;

            InteractionPromptManager.Instance?.HidePrompt();
        }
    }
}