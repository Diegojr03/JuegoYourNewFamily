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
        if (string.IsNullOrEmpty(triggerId))
        {
            GenerateId();
        }
        else
        {
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
    public string triggerTag = "Player";

    [Header("Objeto a mover")]
    public GameObject objectToMove;
    public Transform[] pathPoints;
    public float moveSpeed = 3f;
    public float stoppingDistance = 0.1f;
    public bool faceDirection = true;
    public bool playOnce = true;

    [Header("Configuración de Destrucción")]
    public bool destroyAfterPath = false;
    public bool destroyThisObject = false;

    [Header("Control de Movimiento (para personaje principal)")]
    [Tooltip("Nombre del componente que controla el movimiento del personaje (ej: 'MovimientoPersonaje', 'PlayerMovement')")]
    public string movementControllerTypeName = "MovimientoPersonaje";

    // NUEVO: Arrays para activar/destruir objetos al completar el recorrido (igual que en DialogueSystem)
    [Header("Acciones al Completar el Recorrido")]
    public GameObject[] objectsToActivateAfter;
    public GameObject[] objectsToDestroyAfter;

    private bool hasPlayed = false;
    private bool isCompleted = false;

    // Variables para guardar el estado original del movimiento
    private MonoBehaviour playerMovementController;
    private Rigidbody2D playerRigidbody;
    private Vector2 originalVelocity;
    private bool isPlayerCharacter = false;

    private void Start()
    {
        // Detectar si el objeto a mover es el personaje principal
        if (objectToMove != null)
        {
            isPlayerCharacter = (objectToMove.CompareTag("Player") || objectToMove.name == "PersonajePrincipal");

            if (isPlayerCharacter)
            {
                // Buscar el componente de movimiento (como en DialogueSystem)
                playerMovementController = FindMovementController(objectToMove);
                playerRigidbody = objectToMove.GetComponent<Rigidbody2D>();
            }
        }

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

                // Bloquear movimiento si es el personaje principal (como en DialogueSystem)
                if (isPlayerCharacter)
                {
                    BlockPlayerMovement(true);
                }

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

        // Desbloquear movimiento si es el personaje principal
        if (isPlayerCharacter)
        {
            BlockPlayerMovement(false);
        }

        // NUEVO: Activar objetos (igual que en DialogueSystem)
        foreach (GameObject obj in objectsToActivateAfter)
        {
            if (obj != null)
            {
                SaveableObject saveable = obj.GetComponent<SaveableObject>();
                if (saveable != null && SaveManager.Instance != null)
                    SaveManager.Instance.RegisterObjectState(saveable.objectId, true);
                obj.SetActive(true);
            }
        }

        // NUEVO: Destruir objetos (igual que en DialogueSystem)
        foreach (GameObject obj in objectsToDestroyAfter)
        {
            if (obj != null)
            {
                SaveableObject saveable = obj.GetComponent<SaveableObject>();
                if (saveable != null && SaveManager.Instance != null)
                {
                    SaveManager.Instance.RegisterObjectDestroyed(saveable.objectId);
                }
                Destroy(obj);
            }
        }

        // Destruir el objeto que se movió SOLO si está marcado
        if (destroyAfterPath && objectToMove != null)
        {
            Destroy(objectToMove);
            Debug.Log($"Se ha destruido {objectToMove.name} después del recorrido.");
        }

        CheckAndDestroyTriggerObject();
    }

    private void SkipToEnd()
    {
        if (pathPoints != null && pathPoints.Length > 0 && objectToMove != null)
        {
            Transform lastPoint = pathPoints[pathPoints.Length - 1];
            if (lastPoint != null)
            {
                Vector3 lastDir = Vector3.zero;
                if (pathPoints.Length >= 2 && pathPoints[pathPoints.Length - 2] != null)
                {
                    lastDir = (lastPoint.position - pathPoints[pathPoints.Length - 2].position).normalized;
                }

                objectToMove.transform.position = lastPoint.position;

                if (faceDirection && lastDir.x != 0)
                {
                    float newScaleX = lastDir.x > 0 ? Mathf.Abs(objectToMove.transform.localScale.x) : -Mathf.Abs(objectToMove.transform.localScale.x);
                    objectToMove.transform.localScale = new Vector3(newScaleX, objectToMove.transform.localScale.y, objectToMove.transform.localScale.z);
                }
            }
        }

        // NUEVO: Activar objetos al cargar partida guardada
        foreach (GameObject obj in objectsToActivateAfter)
        {
            if (obj != null)
            {
                SaveableObject saveable = obj.GetComponent<SaveableObject>();
                if (saveable != null && SaveManager.Instance != null)
                    SaveManager.Instance.RegisterObjectState(saveable.objectId, true);
                obj.SetActive(true);
            }
        }

        // NUEVO: Destruir objetos al cargar partida guardada
        foreach (GameObject obj in objectsToDestroyAfter)
        {
            if (obj != null)
            {
                SaveableObject saveable = obj.GetComponent<SaveableObject>();
                if (saveable != null && SaveManager.Instance != null)
                {
                    SaveManager.Instance.RegisterObjectDestroyed(saveable.objectId);
                }
                Destroy(obj);
            }
        }

        // Destruir el objeto SOLO si está marcado (como en la lógica original)
        if (destroyAfterPath && objectToMove != null)
        {
            Destroy(objectToMove);
        }
    }

    /// <summary>
    /// Busca el componente de movimiento en el objeto (como en DialogueSystem)
    /// </summary>
    private MonoBehaviour FindMovementController(GameObject obj)
    {
        if (obj == null) return null;

        // Buscar en el objeto principal y en sus hijos
        MonoBehaviour[] behaviours = obj.GetComponentsInChildren<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            if (behaviour != null && behaviour.GetType().Name == movementControllerTypeName)
            {
                return behaviour;
            }
        }
        return null;
    }

    /// <summary>
    /// Bloquea/desbloquea el movimiento del personaje (como en DialogueSystem)
    /// </summary>
    private void BlockPlayerMovement(bool block)
    {
        // Desactivar/activar el componente de movimiento
        if (playerMovementController != null)
        {
            playerMovementController.enabled = !block;
            Debug.Log($"{(block ? "Bloqueado" : "Desbloqueado")} el movimiento del personaje (componente {movementControllerTypeName}).");
        }

        // Congelar/descongelar el Rigidbody
        if (playerRigidbody != null)
        {
            if (block)
            {
                originalVelocity = playerRigidbody.linearVelocity;
                playerRigidbody.linearVelocity = Vector2.zero;
            }
            else
            {
                playerRigidbody.linearVelocity = originalVelocity;
            }
        }
    }

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