using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ZoneNameDisplay : MonoBehaviour
{
    [Header("Configuración de la Zona")]
    public string zoneName = "Nombre de la Zona";

    [Header("Referencias UI (Arrastra aquí los elementos)")]
    public GameObject uiPanel;          // El panel que contiene el fondo y texto
    public TextMeshProUGUI zoneText;     // El texto que mostrará el nombre
    public Image backgroundImage;        // El fondo (opcional)

    [Header("Personalización")]
    public TMP_FontAsset customFont;     // Fuente personalizada
    public float fontSize = 36f;
    public Color textColor = Color.white;
    public Color backgroundColor = new Color(0, 0, 0, 0.7f);
    public Sprite backgroundSprite;      // Sprite para el fondo

    [Header("Tiempo")]
    public float displayTime = 1f;       // Tiempo que se muestra el mensaje (1 segundo)
    public float fadeDuration = 0.3f;    // Duración del fade in/out

    [Header("Tags")]
    public string playerTag = "Player";  // Tag del jugador

    private CanvasGroup canvasGroup;
    private Coroutine messageCoroutine;
    private bool isPlayerInZone = false;
    private bool isQuitting = false;     // Para detectar cuando se cierra el juego

    void Start()
    {
        // Configurar el UI
        if (uiPanel != null)
        {
            // Asegurar que tiene CanvasGroup
            canvasGroup = uiPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = uiPanel.AddComponent<CanvasGroup>();

            // Configurar texto
            if (zoneText != null)
            {
                zoneText.text = zoneName;

                if (customFont != null)
                    zoneText.font = customFont;

                zoneText.fontSize = fontSize;
                zoneText.color = textColor;
            }

            // Configurar fondo
            if (backgroundImage != null)
            {
                if (backgroundSprite != null)
                    backgroundImage.sprite = backgroundSprite;

                backgroundImage.color = backgroundColor;
            }

            // Iniciar oculto
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }
        else
        {
            Debug.LogError("No se ha asignado el Panel UI en " + gameObject.name);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInZone = true;

            // Detener cualquier corrutina anterior
            if (messageCoroutine != null)
                StopCoroutine(messageCoroutine);

            // Verificar que el objeto está activo antes de iniciar la corrutina
            if (gameObject.activeInHierarchy)
            {
                messageCoroutine = StartCoroutine(ShowMessageSequence());
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInZone = false;

            // Detener la corrutina actual
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
                messageCoroutine = null;
            }

            // Hacer fade out inmediato solo si el objeto está activo
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(FadeTo(0f));
            }
            else
            {
                // Si el objeto está inactivo, directamente poner alpha a 0
                if (canvasGroup != null)
                    canvasGroup.alpha = 0f;
            }
        }
    }

    IEnumerator ShowMessageSequence()
    {
        // Fade In
        yield return StartCoroutine(FadeTo(1f));

        // Esperar 1 segundo (displayTime)
        yield return new WaitForSeconds(displayTime);

        // Fade out
        yield return StartCoroutine(FadeTo(0f));

        messageCoroutine = null;
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        // Verificar que el canvasGroup existe antes de empezar
        if (canvasGroup == null)
            yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            // Verificar en cada frame que el objeto sigue activo
            if (!gameObject.activeInHierarchy || canvasGroup == null)
                yield break;

            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        // Verificar una última vez antes de asignar
        if (canvasGroup != null)
            canvasGroup.alpha = targetAlpha;
    }

    // Método público para desactivar manualmente (por si necesitas desactivar el trigger)
    public void DisableZone()
    {
        // Detener corrutinas
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        // Ocultar UI inmediatamente
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        // Desactivar el gameObject
        gameObject.SetActive(false);
    }

    // Detectar cuando el juego se está cerrando
    void OnApplicationQuit()
    {
        isQuitting = true;
    }

    void OnDisable()
    {
        // Solo ejecutar si no estamos cerrando el juego
        if (!isQuitting)
        {
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
                messageCoroutine = null;
            }

            // Verificar que canvasGroup existe antes de usarlo
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }
    }

    void OnDestroy()
    {
        // Limpiar referencias
        canvasGroup = null;
    }
}