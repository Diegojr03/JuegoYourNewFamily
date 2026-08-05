using System.Collections;
using UnityEngine;

public class TriggerPathActivator : MonoBehaviour
{
    [Header("Identificador de Guardado")]
    [Tooltip("ID único para este activador. Se genera automáticamente o mediante el menú contextual.")]
    public string triggerId;

    [ContextMenu("Generate New ID")]
    public void GenerateId()
    {
        triggerId = System.Guid.NewGuid().ToString();
        Debug.Log($"🆔 ID generado para TriggerPathActivator ({gameObject.name}): {triggerId}");
    }

    private void Reset()
    {
        GenerateId();
    }

    private void OnValidate()
    {
        // Si el ID está vacío, generamos uno nuevo
        if (string.IsNullOrEmpty(triggerId))
        {
            GenerateId();
        }
        else
        {
            // Evitamos IDs duplicados si el componente fue copiado y pegado en el mismo GameObject
            TriggerPathActivator[] activators = GetComponents<TriggerPathActivator>();
            foreach (var act in activators)
            {
                if (act != this && act != null && act.triggerId == this.triggerId)
                {
                    GenerateId();
                    break;
                }
            }
        }
    }

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
    private bool isCompleted = false; // Indica si ESTE activador específico ya terminó su tarea

    private void Start()
    {
        // Al iniciar la escena, comprobamos si este recorrido ya fue activado/completado en la partida cargada
        if (!string.IsNullOrEmpty(triggerId) && SaveManager.Instance != null)
        {
            if (SaveManager.Instance.IsPathCompleted(triggerId))
            {
                hasPlayed = true;
                isCompleted = true;
                SkipToEnd();
                CheckAndDestroyTriggerObject();
                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag))
        {
            if (playOnce && hasPlayed) return;

            if (objectToMove != null && pathPoints.Length > 0)
            {
                hasPlayed = true;

                // Registrar inmediatamente que el recorrido se ha iniciado
                if (!string.IsNullOrEmpty(triggerId) && SaveManager.Instance != null)
                {
                    SaveManager.Instance.RegisterPathCompleted(triggerId);
                }

                StartCoroutine(MoveObjectAlongPath());
            }
            else
            {
                Debug.LogWarning($"TriggerPathActivator en {gameObject.name}: Faltan puntos del recorrido o el objeto no está asignado.");
            }
        }
    }

    private IEnumerator MoveObjectAlongPath()
    {
        for (int i = 0; i < pathPoints.Length; i++)
        {
            Transform target = pathPoints[i];
            if (target == null) continue;

            while (objectToMove != null && Vector3.Distance(objectToMove.transform.position, target.position) > stoppingDistance)
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
        if (objectToMove != null)
        {
            Debug.Log($"{objectToMove.name} ha completado su recorrido.");
        }

        isCompleted = true;

        // Destruir el objeto que se movió si está marcado
        if (destroyAfterPath && objectToMove != null)
        {
            Destroy(objectToMove);
            Debug.Log($"Se ha destruido {objectToMove.name} después del recorrido.");
        }

        // Verificar si se debe destruir el GameObject que contiene este trigger
        CheckAndDestroyTriggerObject();
    }

    /// <summary>
    /// Coloca el objeto directamente en el punto final del recorrido al cargar una partida guardada.
    /// </summary>
    private void SkipToEnd()
    {
        if (pathPoints != null && pathPoints.Length > 0 && objectToMove != null)
        {
            Transform lastPoint = pathPoints[pathPoints.Length - 1];
            if (lastPoint != null)
            {
                // Calcular orientación según el último tramo
                Vector3 lastDir = Vector3.zero;
                if (pathPoints.Length >= 2 && pathPoints[pathPoints.Length - 2] != null)
                {
                    lastDir = (lastPoint.position - pathPoints[pathPoints.Length - 2].position).normalized;
                }

                // Posicionar en el punto final
                objectToMove.transform.position = lastPoint.position;

                // Orientar el objeto
                if (faceDirection && lastDir.x != 0)
                {
                    float newScaleX = lastDir.x > 0 ? Mathf.Abs(objectToMove.transform.localScale.x) : -Mathf.Abs(objectToMove.transform.localScale.x);
                    objectToMove.transform.localScale = new Vector3(newScaleX, objectToMove.transform.localScale.y, objectToMove.transform.localScale.z);
                }
            }
        }

        // Aplicar destrucción del objeto en movimiento si está configurado
        if (destroyAfterPath && objectToMove != null)
        {
            Destroy(objectToMove);
        }
    }

    /// <summary>
    /// Revisa todos los TriggerPathActivator presentes en este mismo GameObject.
    /// Solo destruye el GameObject si TODOS han completado su recorrido y al menos uno solicita la destrucción.
    /// </summary>
    private void CheckAndDestroyTriggerObject()
    {
        TriggerPathActivator[] allActivators = GetComponents<TriggerPathActivator>();

        bool allFinished = true;
        bool shouldDestroy = false;

        foreach (var act in allActivators)
        {
            if (!act.isCompleted)
            {
                allFinished = false;
            }
            if (act.destroyThisObject)
            {
                shouldDestroy = true;
            }
        }

        if (allFinished && shouldDestroy)
        {
            Debug.Log($"Se ha destruido el trigger {gameObject.name} porque todos sus TriggerPathActivator han finalizado.");
            Destroy(gameObject);
        }
    }

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