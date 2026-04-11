using UnityEngine;

public class PlayerScaleByY : MonoBehaviour
{
    [Header("Referencias")]
    public Transform playerTransform;

    [Header("Rango de Y en el mundo (dentro del trigger)")]
    public float minY = -10f;   // Y más baja -> escala máxima
    public float maxY = 10f;    // Y más alta -> escala mínima

    [Header("Rango de escala dentro del trigger")]
    public float maxScale = 1.5f; // Escala cuando Y <= minY
    public float minScale = 0.5f; // Escala cuando Y >= maxY

    [Header("Escala al salir del trigger")]
    public float defaultScale = 1f; // Escala que se aplica al salir de la zona (pon 2 si quieres)

    private bool isPlayerInside = false;
    private Vector2 lastPlayerPosition;

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else Debug.LogError("No se encontró el jugador. Asigna la referencia manualmente.");
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) Debug.LogError("Falta Collider2D en " + gameObject.name);
        else if (!col.isTrigger) Debug.LogWarning("El Collider2D debe ser Trigger");
    }

    private void Update()
    {
        // Solo actualizar si el jugador está dentro y se ha movido
        if (isPlayerInside && playerTransform != null)
        {
            Vector2 currentPos = playerTransform.position;
            if (currentPos != lastPlayerPosition)
            {
                float t = Mathf.InverseLerp(minY, maxY, currentPos.y);
                float newScale = Mathf.Lerp(maxScale, minScale, t);
                playerTransform.localScale = new Vector3(newScale, newScale, 1f);
                lastPlayerPosition = currentPos;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            // Guardar posición actual
            lastPlayerPosition = playerTransform.position;
            // Aplicar la escala correspondiente a la posición Y actual (inmediatamente)
            float t = Mathf.InverseLerp(minY, maxY, playerTransform.position.y);
            float newScale = Mathf.Lerp(maxScale, minScale, t);
            playerTransform.localScale = new Vector3(newScale, newScale, 1f);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            // Restaurar la escala por defecto al salir
            playerTransform.localScale = new Vector3(defaultScale, defaultScale, 1f);
        }
    }
}