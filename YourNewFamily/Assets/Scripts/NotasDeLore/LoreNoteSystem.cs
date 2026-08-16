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

        PrepareTextForDisplay();

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

        CalculatePositions();

        // 1. Ejecutar el Fade Out y el movimiento de bajada EN PARALELO para ahorrar tiempo
        Coroutine fadeCoroutine = StartCoroutine(FadeCanvasGroup(1f, 0f, fadeDuration * 0.5f)); // Fade 2x más rápido
        Coroutine moveCoroutine = null;

        if (giantNoteTransform != null)
        {
            // Pasamos un multiplicador de velocidad 2.5f para que baje mucho más rápido
            moveCoroutine = StartCoroutine(MoveNote(hiddenPosition, 2.5f));
        }

        // Esperar a que ambas animaciones terminen
        if (fadeCoroutine != null) yield return fadeCoroutine;
        if (moveCoroutine != null) yield return moveCoroutine;

        if (noteUIPanel != null)
            noteUIPanel.SetActive(false);

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
                SaveableObject saveable = obj.GetComponent<SaveableObject>();
                if (saveable != null && SaveManager.Instance != null)
                {
                    SaveManager.Instance.RegisterObjectState(saveable.objectId, true);
                }
                obj.SetActive(true);
            }
        }

        // Desactivar objetos pendientes
        foreach (GameObject obj in objectsToDestroyAfter)
        {
            if (obj != null)
            {
                SaveableObject saveable = obj.GetComponent<SaveableObject>();
                if (saveable != null && SaveManager.Instance != null)
                {
                    SaveManager.Instance.RegisterObjectState(saveable.objectId, false);
                }
                obj.SetActive(false);
            }
        }

        // Restablecer movimiento del jugador inmediatamente
        if (playerMovement != null)
            playerMovement.enabled = true;

        if (playerRigidbody != null)
            playerRigidbody.linearVelocity = originalVelocity;

        isNoteActive = false;
        isAnimating = false;

        // Destruir o reactivar interacción
        if (destroyAfterRead)
        {
            SaveableObject thisSaveable = GetComponent<SaveableObject>();
            if (thisSaveable != null && SaveManager.Instance != null)
            {
                SaveManager.Instance.RegisterObjectState(thisSaveable.objectId, false);
            }
            Destroy(gameObject);
        }
        else if (isPlayerInside)
        {
            InteractionPoint point = GetComponent<InteractionPoint>();
            if (point != null)
            {
                InteractionPromptManager.Instance?.ShowPrompt(point);
            }
        }
    }


    private IEnumerator MoveNote(Vector2 target, float speedMultiplier = 1f)
    {
        float currentSpeed = moveSpeed * speedMultiplier;

        while (Vector2.Distance(giantNoteTransform.position, target) > 0.05f)
        {
            giantNoteTransform.position = Vector2.Lerp(
                giantNoteTransform.position,
                target,
                currentSpeed * Time.deltaTime
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
            Debug.LogWarning("No se ha asignado un archivo de texto a esta nota.");
            yield break;
        }

        isTyping = true;

        // 1. Asignar el texto de la nota
        string text = noteTextAsset.text + "\n\n";
        noteTextUI.text = text;
        noteTextUI.maxVisibleCharacters = 0; // Ocultar texto mientras se calcula

        // 2. Esperar a que los canvas y el layout de TMP se actualicen tras la activación
        yield return new WaitForEndOfFrame();

        noteTextUI.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();

        // 3. Ajustar la altura del Content en el ScrollView
        if (scrollRect != null && scrollRect.content != null)
        {
            float textHeight = noteTextUI.preferredHeight;
            scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f; // Scroll arriba del todo
        }

        // 4. Bucle del efecto de máquina de escribir
        // Se usa text.Length para garantizar que recorra todos los caracteres
        int totalCharacters = text.Length;

        for (int i = 0; i <= totalCharacters; i++)
        {
            noteTextUI.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typewriterSpeed);
        }

        // Asegurar que al finalizar se muestre todo sin límite
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

    private void PrepareTextForDisplay()
    {
        if (noteTextUI == null || noteTextAsset == null)
            return;

        // Asignar el texto completo (incluyendo saltos de línea si quieres)
        noteTextUI.text = noteTextAsset.text + "\n\n";
        // Ocultar todos los caracteres
        noteTextUI.maxVisibleCharacters = 0;
        noteTextUI.ForceMeshUpdate();

        // Ajustar la altura del Content del ScrollView
        if (scrollRect != null && scrollRect.content != null)
        {
            float textHeight = noteTextUI.preferredHeight;
            scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f; // Arriba del todo
        }
    }
}