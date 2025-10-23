using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionPoint : MonoBehaviour
{
    [Header("Configuración de Transición")]
    public Transform targetRoomCenter; // Punto central de la sala destino
    public Vector2 targetPlayerPosition; // Posición donde aparecerá el jugador
    public string transitionName; // Nombre para identificar la transición

    [Header("Configuración de Cámara")]
    public float cameraMoveSpeed = 10f; // Velocidad de movimiento de la cámara
    public float targetCameraSize = 5f; // Tamaño de cámara para esta sala
    public float cameraSizeChangeSpeed = 2f; // Velocidad de cambio de tamaño

    private Camera mainCamera;
    private bool isTransitioning = false;

    void Start()
    {
        mainCamera = Camera.main;
    }

    public void InitiateTransition(GameObject player)
    {
        if (!isTransitioning)
        {
            isTransitioning = true;

            // Teletransportar jugador
            player.transform.position = targetPlayerPosition;

            // Mover cámara suavemente a la nueva sala y cambiar tamaño
            if (mainCamera != null && targetRoomCenter != null)
            {
                StartCoroutine(MoveCameraToRoom());
            }
            else
            {
                isTransitioning = false;
            }
        }
    }

    private System.Collections.IEnumerator MoveCameraToRoom()
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
            // Mover posición de la cámara
            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                targetPosition,
                cameraMoveSpeed * Time.deltaTime
            );

            // Cambiar tamaño de la cámara
            mainCamera.orthographicSize = Mathf.Lerp(
                mainCamera.orthographicSize,
                targetSize,
                cameraSizeChangeSpeed * Time.deltaTime
            );

            yield return null;
        }

        // Asegurar posición y tamaño exactos
        mainCamera.transform.position = targetPosition;
        mainCamera.orthographicSize = targetSize;
        isTransitioning = false;
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Trigger entered with: {other.name}, Tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            Debug.Log($"Initiating transition with player: {other.name}");
            InitiateTransition(other.gameObject);
        }
        else
        {
            Debug.Log($"Collider tag is not Player. Actual tag: {other.tag}");
        }
    }
    // Dibujar gizmos en el editor para visualizar mejor
    void OnDrawGizmosSelected()
    {
        // Dibujar posición destino del jugador
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPlayerPosition, 0.3f);

        // Dibujar área de la cámara destino (si hay room center)
        if (targetRoomCenter != null)
        {
            // Obtener la cámara actual para el aspect ratio (o usar valor por defecto)
            Camera cam = Camera.main;
            float aspectRatio = 16f / 9f; // Valor por defecto común

            if (cam != null)
            {
                aspectRatio = cam.aspect;
            }
            else
            {
                // Si no hay cámara en la escena, buscar cualquier cámara
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

            // Dibujar línea desde este objeto hasta la posición destino
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetRoomCenter.position);
        }
    }
}
