using System.Collections;
using UnityEngine;

public class TriggerPathActivator : MonoBehaviour
{
    [Header("Configuración del Trigger")]
    public string triggerTag = "Player"; // Quién activa el recorrido

    [Header("Objeto a mover")]
    public GameObject objectToMove; // El objeto que se moverá (rata, coche, etc.)
    public Transform[] pathPoints;  // Puntos del recorrido
    public float moveSpeed = 3f;
    public float stoppingDistance = 0.1f;
    public bool faceDirection = true;
    public bool playOnce = true;

    [Header("Configuración de Destrucción")]
    public bool destroyAfterPath = false; // Destruir el objeto después del recorrido
    public bool destroyThisObject = false; // Destruir este objeto (el que tiene el script)

    private bool hasPlayed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag))
        {
            if (playOnce && hasPlayed) return;

            if (objectToMove != null && pathPoints.Length > 0)
            {
                StartCoroutine(MoveObjectAlongPath());
                hasPlayed = true;
            }
            else
            {
                Debug.LogWarning("TriggerPathActivator: Faltan puntos del recorrido o el objeto no está asignado.");
            }
        }
    }

    private IEnumerator MoveObjectAlongPath()
    {
        for (int i = 0; i < pathPoints.Length; i++)
        {
            Transform target = pathPoints[i];
            if (target == null) continue;

            while (Vector3.Distance(objectToMove.transform.position, target.position) > stoppingDistance)
            {
                Vector3 dir = (target.position - objectToMove.transform.position).normalized;
                objectToMove.transform.position = Vector3.MoveTowards(
                    objectToMove.transform.position,
                    target.position,
                    moveSpeed * Time.deltaTime
                );

                if (faceDirection && dir.x != 0)
                {
                    float newScaleX = dir.x > 0 ? Mathf.Abs(objectToMove.transform.localScale.x) : -Mathf.Abs(objectToMove.transform.localScale.x);
                    objectToMove.transform.localScale = new Vector3(newScaleX, objectToMove.transform.localScale.y, objectToMove.transform.localScale.z);
                }

                yield return null;
            }
        }

        OnPathComplete();
    }

    private void OnPathComplete()
    {
        Debug.Log($"{objectToMove.name} ha completado su recorrido.");

        // Destruir el objeto que se movió si está marcado
        if (destroyAfterPath && objectToMove != null)
        {
            Destroy(objectToMove);
            Debug.Log($"Se ha destruido {objectToMove.name} después del recorrido.");
        }

        // Destruir este objeto (el que tiene el script) si está marcado
        if (destroyThisObject)
        {
            Destroy(gameObject);
            Debug.Log($"Se ha destruido el trigger {gameObject.name} después del recorrido.");
        }
    }

    // Solo para visualizar en el editor
    private void OnDrawGizmosSelected()
    {
        if (pathPoints == null || pathPoints.Length == 0) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            if (pathPoints[i] && pathPoints[i + 1])
            {
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
                Gizmos.DrawWireSphere(pathPoints[i].position, 0.2f);
            }
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pathPoints[pathPoints.Length - 1].position, 0.25f);
    }

}