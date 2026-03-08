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
    public Image fadeImage;

    [Header("Configuración de Sonido")]
    public AudioClip transitionSound;
    public float soundVolume = 1f;

    [Header("Instanciación de Prefabs")]
    public GameObject prefabToSpawn; // El prefab del trigger de música
    public Transform spawnPoint; // Punto opcional donde instanciar (si es null, usa targetPlayerPosition)
    public bool spawnAfterFadeIn = true; // Si es true, sale cuando está en negro. Si es false, al final.

    [Header("Objetos a Activar después del Fade In")]
    public GameObject[] objectsToActivateAfterFadeIn;

    [Header("Objetos a Destruir después del Fade In")]
    public GameObject[] objectsToDestroyAfterFadeIn;

    [Header("Objetos a Activar al Final")]
    public GameObject[] objectsToActivateAfter;

    [Header("Objetos a Destruir al Final")]
    public GameObject[] objectsToDestroyAfter;

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
            if (transitionSound != null)
            {
                AudioSource.PlayClipAtPoint(transitionSound, transform.position, soundVolume);
            }
            StartCoroutine(TransitionSequence(player));
        }
    }

    private IEnumerator TransitionSequence(GameObject player)
    {
        BlockPlayerMovement(true);

        // 1. Fade In
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // 2. Acciones en negro
        ActivateObjectsAfterFadeIn();
        DestroyObjectsAfterFadeIn();

        if (spawnAfterFadeIn) { SpawnRequestedPrefab(); } // <--- Instanciar aquí si se prefiere en negro

        player.transform.position = targetPlayerPosition;

        if (mainCamera != null && targetRoomCenter != null)
        {
            yield return StartCoroutine(MoveCameraToRoom());
        }

        // 3. Fade Out
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // 4. Acciones finales
        ActivateObjectsAfter();
        DestroyObjectsAfter();

        if (!spawnAfterFadeIn) { SpawnRequestedPrefab(); } // <--- Instanciar aquí si se prefiere al final

        BlockPlayerMovement(false);
        isTransitioning = false;
    }

    private void SpawnRequestedPrefab()
    {
        if (prefabToSpawn != null)
        {
            Vector3 position = spawnPoint != null ? spawnPoint.position : (Vector3)targetPlayerPosition;
            Instantiate(prefabToSpawn, position, Quaternion.identity);
            Debug.Log($"Prefab {prefabToSpawn.name} instanciado correctamente.");
        }
    }

    // --- MÉTODOS DE FADE, CÁMARA Y BLOQUEO (SIN CAMBIOS) ---

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
        Vector3 targetPosition = new Vector3(targetRoomCenter.position.x, targetRoomCenter.position.y, mainCamera.transform.position.z);
        float targetSize = targetCameraSize;
        while (Vector3.Distance(mainCamera.transform.position, targetPosition) > 0.1f || Mathf.Abs(mainCamera.orthographicSize - targetSize) > 0.05f)
        {
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, cameraMoveSpeed * Time.deltaTime);
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetSize, cameraSizeChangeSpeed * Time.deltaTime);
            yield return null;
        }
        mainCamera.transform.position = targetPosition;
        mainCamera.orthographicSize = targetSize;
    }

    private void BlockPlayerMovement(bool block)
    {
        if (block)
        {
            if (playerRigidbody != null) { originalVelocity = playerRigidbody.linearVelocity; playerRigidbody.linearVelocity = Vector2.zero; }
            if (playerMovement != null) playerMovement.enabled = false;
        }
        else
        {
            if (playerMovement != null) playerMovement.enabled = true;
            if (playerRigidbody != null) playerRigidbody.linearVelocity = originalVelocity;
        }
    }

    private void ActivateObjectsAfterFadeIn() { foreach (GameObject obj in objectsToActivateAfterFadeIn) if (obj != null) obj.SetActive(true); }
    private void DestroyObjectsAfterFadeIn() { foreach (GameObject obj in objectsToDestroyAfterFadeIn) if (obj != null) Destroy(obj); }
    private void ActivateObjectsAfter() { foreach (GameObject obj in objectsToActivateAfter) if (obj != null) obj.SetActive(true); }
    private void DestroyObjectsAfter() { foreach (GameObject obj in objectsToDestroyAfter) if (obj != null) Destroy(obj); }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) InitiateTransition(other.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPlayerPosition, 0.3f);
        if (spawnPoint != null) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(spawnPoint.position, 0.2f); }
    }
}