using System.Collections;
using UnityEngine;

public class NPCPathFollower : MonoBehaviour
{
    [Header("Configuración del NPC")]
    public float moveSpeed = 3f;
    public float stoppingDistance = 1f;
    public bool facePlayer = true;

    [Header("Puntos del Recorrido")]
    public Transform[] pathPoints; // Puntos intermedios del recorrido
    public Transform finalTarget; // Objetivo final (normalmente el jugador)

    [Header("Activación")]
    public bool activateOnTrigger = true;
    public string triggerTag = "Player";


    private bool isMoving = false;
    private int currentPointIndex = 0;
    private Transform player;

    void Start()
    {
        // Buscar al jugador automáticamente si no está asignado
        if (finalTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                finalTarget = playerObj.transform;
            }
        }

        player = finalTarget;

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activateOnTrigger && other.CompareTag(triggerTag) && !isMoving)
        {
            StartFollowingPath();
        }
    }

    public void StartFollowingPath()
    {
        if (!isMoving)
        {
            isMoving = true;
            currentPointIndex = 0;
            StartCoroutine(FollowPath());
        }
    }

    private IEnumerator FollowPath()
    {


        // Seguir puntos del recorrido
        if (pathPoints != null && pathPoints.Length > 0)
        {
            for (int i = 0; i < pathPoints.Length; i++)
            {
                if (pathPoints[i] == null) continue;

                currentPointIndex = i;
                yield return StartCoroutine(MoveToTarget(pathPoints[i].position));
            }
        }

        // Moverse hacia el objetivo final (jugador)
        if (finalTarget != null)
        {
            yield return StartCoroutine(MoveToTarget(finalTarget.position));
        }

        // Llegó al destino
        PathCompleted();
    }

    private IEnumerator MoveToTarget(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > stoppingDistance)
        {
            // Calcular dirección
            Vector3 direction = (targetPosition - transform.position).normalized;

            // Mover NPC
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            // Rotar para mirar al objetivo si está configurado
            if (facePlayer && direction != Vector3.zero)
            {
                // Para 2D: ajustar la escala en X para voltear el sprite
                if (direction.x > 0)
                {
                    transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }
                else if (direction.x < 0)
                {
                    transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }
            }

            yield return null;
        }
    }

    private void PathCompleted()
    {
        isMoving = false;


        Debug.Log("NPC ha completado el recorrido");
    }

    // Métodos públicos para activación desde otros scripts
    public void StartPathFromScript()
    {
        StartFollowingPath();
    }

    public void SetFinalTarget(Transform newTarget)
    {
        finalTarget = newTarget;
    }

    public void SetPathPoints(Transform[] newPathPoints)
    {
        pathPoints = newPathPoints;
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    // Método para debug visual en el editor
    void OnDrawGizmosSelected()
    {
        // Dibujar puntos del recorrido
        if (pathPoints != null && pathPoints.Length > 0)
        {
            Gizmos.color = Color.yellow;

            // Dibujar primer punto
            Gizmos.DrawWireSphere(pathPoints[0].position, 0.3f);

            // Dibujar línea entre puntos
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                if (pathPoints[i] != null && pathPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
                    Gizmos.DrawWireSphere(pathPoints[i + 1].position, 0.3f);
                }
            }

            // Dibujar línea al objetivo final
            if (finalTarget != null && pathPoints[pathPoints.Length - 1] != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(pathPoints[pathPoints.Length - 1].position, finalTarget.position);
                Gizmos.DrawWireSphere(finalTarget.position, 0.4f);
            }
        }
        else if (finalTarget != null)
        {
            // Si no hay puntos intermedios, dibujar línea directa al objetivo
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, finalTarget.position);
            Gizmos.DrawWireSphere(finalTarget.position, 0.4f);
        }
    }
}
