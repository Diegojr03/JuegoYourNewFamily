using UnityEngine;

public class CreditsScroll : MonoBehaviour
{
    [Header("Configuración de Scroll")]
    public float scrollSpeed = 50f; // Velocidad del scroll
    public RectTransform targetElement; // Arrastra aquí el GameObject que activará el freno al llegar al centro (0,0,0)
    public Canvas parentCanvas; // Arrastra el Canvas principal aquí

    [Header("Configuración del Temporizador")]
    public float delayAfterLastElement = 7f; // Tiempo en segundos a esperar tras llegar al centro

    [Header("Transición")]
    public string nextSceneName = "MainMenu"; // Escena a la que vuelve al terminar
    public AudioClip transitionSound; // Opcional: sonido al iniciar la transición

    private RectTransform rectTransform;
    private bool isEnding = false;
    private bool isCountingDown = false;
    private float timer = 0f;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // Si no se asignó el Canvas manualmente, intenta buscarlo en los padres
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
    }

    void Update()
    {
        if (isEnding) return;

        // Si aún no hemos alcanzado el centro, continuamos moviendo el scroll
        if (!isCountingDown)
        {
            rectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);
        }

        // Permitir saltar los créditos inmediatamente con Espacio o Escape
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            FinishCredits();
            return;
        }

        // Comprobar si el targetElement ha alcanzado o superado el centro del Canvas (Y >= 0 en espacio local del Canvas)
        if (!isCountingDown && targetElement != null && parentCanvas != null)
        {
            // Convertir la posición en el mundo de targetElement al espacio local del RectTransform del Canvas
            RectTransform canvasRect = parentCanvas.transform as RectTransform;
            Vector2 localPoint;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                RectTransformUtility.WorldToScreenPoint(null, targetElement.position),
                null,
                out localPoint
            );

            // El punto (0,0) representa el centro del Canvas.
            // Si la coordenada Y del objeto llega o supera el centro (0), detenemos el scroll e iniciamos la cuenta atrás.
            if (localPoint.y >= 0f)
            {
                isCountingDown = true;
                timer = delayAfterLastElement;
            }
        }

        // Si se detuvo el scroll al llegar al centro, descontamos el temporizador
        if (isCountingDown)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                FinishCredits();
            }
        }
    }

    void FinishCredits()
    {
        if (isEnding) return;
        isEnding = true;

        // 🗑️ Borrar el archivo de guardado para forzar una nueva partida
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave();
        }
        else
        {
            Debug.LogWarning("No se encontró la instancia de SaveManager para borrar el archivo de guardado.");
        }

        // Reproducir sonido de transición si se asignó uno
        if (transitionSound != null)
        {
            AudioSource.PlayClipAtPoint(transitionSound, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        // Usar el FadeManager para cambiar de escena con fundido
        FadeManager fadeManager = FindObjectOfType<FadeManager>();

        if (fadeManager != null)
        {
            fadeManager.ChangeScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("No se encontró FadeManager en la escena. Cargando directamente la escena...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}