using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TransitionPointWithFade : MonoBehaviour
{
    [Header("Configuración de Transición")]
    public Transform targetRoomCenter;
    public Vector2 targetPlayerPosition;
    public string transitionName;

    [Header("Configuración de Cámara")]
    public float cameraMoveSpeed = 10f;
    public float targetCameraSize = 5f;
    public float cameraSizeChangeSpeed = 2f;

    [Header("Configuración Fade")]
    public float fadeDuration = 1f;
    public Image fadeImage; // Imagen negra para el fade (debe estar en Canvas)

    [Header("Configuración de Sonido")]
    public AudioClip transitionSound;
    public float soundVolume = 1f;

    private Camera mainCamera;
    private bool isTransitioning = false;
    private MovimientoPersonaje playerMovement;
    private Rigidbody2D playerRigidbody;
    private Vector2 originalVelocity;

    void Start()
    {
        mainCamera = Camera.main;
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerMovement = player.GetComponent<MovimientoPersonaje>();
            playerRigidbody = player.GetComponent<Rigidbody2D>();
        }

        // Asegurar que la imagen de fade esté oculta al inicio
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.gameObject.SetActive(true);
        }
    }

    public void InitiateTransition(GameObject player)
    {
        if (!isTransitioning)
        {
            isTransitioning = true;

            // Reproducir sonido al iniciar la transición
            if (transitionSound != null)
            {
                AudioSource.PlayClipAtPoint(transitionSound, transform.position, soundVolume);
            }

            StartCoroutine(TransitionSequence(player));
        }
    }

    private IEnumerator TransitionSequence(GameObject player)
    {
        // 1. Bloquear movimiento del jugador
        BlockPlayerMovement(true);

        // 2. Fade In (aparece negro)
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // 3. Realizar el teletransporte
        player.transform.position = targetPlayerPosition;

        // 4. Mover cámara (si hay target)
        if (mainCamera != null && targetRoomCenter != null)
        {
            yield return StartCoroutine(MoveCameraToRoom());
        }

        // 5. Fade Out (desaparece negro)
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // 6. Desbloquear movimiento del jugador
        BlockPlayerMovement(false);

        isTransitioning = false;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, endAlpha);
    }

    private IEnumerator MoveCameraToRoom()
    {
        Vector3 targetPosition = new Vector3(
            targetRoomCenter.position.x,
            targetRoomCenter.position.y,
            mainCamera.transform.position.z
        );

        float startSize = mainCamera.orthographicSize;
        float targetSize = targetCameraSize;

        float distanceThreshold = 0.1f;
        float sizeThreshold = 0.05f;

        while (Vector3.Distance(mainCamera.transform.position, targetPosition) > distanceThreshold ||
               Mathf.Abs(mainCamera.orthographicSize - targetSize) > sizeThreshold)
        {
            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                targetPosition,
                cameraMoveSpeed * Time.deltaTime
            );

            mainCamera.orthographicSize = Mathf.Lerp(
                mainCamera.orthographicSize,
                targetSize,
                cameraSizeChangeSpeed * Time.deltaTime
            );

            yield return null;
        }

        mainCamera.transform.position = targetPosition;
        mainCamera.orthographicSize = targetSize;
    }

    private void BlockPlayerMovement(bool block)
    {
        if (block)
        {
            // Guardar velocidad actual y detener movimiento
            if (playerRigidbody != null)
            {
                originalVelocity = playerRigidbody.linearVelocity;
                playerRigidbody.linearVelocity = Vector2.zero;
            }

            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }
        }
        else
        {
            // Restaurar movimiento
            if (playerMovement != null)
            {
                playerMovement.enabled = true;
            }

            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = originalVelocity;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Trigger entered with: {other.name}, Tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            Debug.Log($"Initiating transition with fade for player: {other.name}");
            InitiateTransition(other.gameObject);
        }
        else
        {
            Debug.Log($"Collider tag is not Player. Actual tag: {other.tag}");
        }
    }

    // Dibujar gizmos en el editor (igual que el anterior)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPlayerPosition, 0.3f);

        if (targetRoomCenter != null)
        {
            Camera cam = Camera.main;
            float aspectRatio = 16f / 9f;

            if (cam != null)
            {
                aspectRatio = cam.aspect;
            }
            else
            {
                Camera anyCamera = FindObjectOfType<Camera>();
                if (anyCamera != null)
                {
                    aspectRatio = anyCamera.aspect;
                }
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(
                targetRoomCenter.position,
                new Vector3(targetCameraSize * 2 * aspectRatio, targetCameraSize * 2, 0)
            );

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetRoomCenter.position);
        }
    }
}
