using UnityEngine;
using System.Collections.Generic;

public class ColliderActivationSystem : MonoBehaviour
{
    [Header("Colliders a Destruir/Desactivar")]
    public List<Collider2D> collidersToDestroy = new List<Collider2D>();

    [Header("Collider a Activar")]
    public Collider2D colliderToActivate;

    [Header("Configuración")]
    public bool showDebugMessages = true;
    public bool countDisabledColliders = true;

    private int destroyedCount = 0;
    private bool isActivated = false;

    void Start()
    {
        InitializeSystem();
    }

    void InitializeSystem()
    {
        if (collidersToDestroy.Count == 0)
        {
            Debug.LogWarning("No hay colliders asignados en collidersToDestroy", this);
            // PERMITIR ACTIVACIÓN INMEDIATA SI NO HAY COLLIDERS A DESTRUIR
            ActivateTargetCollider();
            return;
        }

        if (colliderToActivate == null)
        {
            Debug.LogWarning("No hay collider asignado en colliderToActivate", this);
            return;
        }

        // IMPORTANTE: NO desactivar el collider objetivo aquí
        // colliderToActivate.enabled = false; // ← ESTA LÍNEA ES EL PROBLEMA

        // Verificar si ya hay colliders destruidos/desactivados al inicio
        CheckInitialState();

        // Suscribirse a los eventos de cada collider
        foreach (Collider2D collider in collidersToDestroy)
        {
            if (collider != null)
            {
                ColliderDestructor destructor = collider.GetComponent<ColliderDestructor>();
                if (destructor == null)
                {
                    destructor = collider.gameObject.AddComponent<ColliderDestructor>();
                }

                // Configurar el destructor
                destructor.countDisabled = countDisabledColliders;
                destructor.OnColliderDestroyed += HandleColliderDestroyed;
                destructor.OnColliderDisabled += HandleColliderDisabled;

                if (showDebugMessages)
                {
                    Debug.Log($"Sistema suscrito al collider: {collider.name}", this);
                }
            }
        }

        if (showDebugMessages)
        {
            Debug.Log($"Sistema inicializado. Esperando destrucción de {collidersToDestroy.Count} colliders", this);
        }
    }

    void CheckInitialState()
    {
        destroyedCount = 0; // Resetear contador

        foreach (Collider2D collider in collidersToDestroy)
        {
            if (collider == null)
            {
                destroyedCount++;
                if (showDebugMessages)
                {
                    Debug.Log($"Collider ya destruido: {collider}", this);
                }
            }
            else if (countDisabledColliders && !collider.enabled)
            {
                destroyedCount++;
                if (showDebugMessages)
                {
                    Debug.Log($"Collider ya desactivado: {collider.name}", this);
                }
            }
            else if (!collider.gameObject.activeInHierarchy)
            {
                destroyedCount++;
                if (showDebugMessages)
                {
                    Debug.Log($"GameObject ya desactivado: {collider.name}", this);
                }
            }
        }

        // Verificar si ya están todos destruidos
        if (destroyedCount >= collidersToDestroy.Count)
        {
            ActivateTargetCollider();
        }
        else if (showDebugMessages)
        {
            Debug.Log($"Estado inicial: {destroyedCount}/{collidersToDestroy.Count} colliders destruidos", this);
        }
    }

    void HandleColliderDestroyed(Collider2D destroyedCollider)
    {
        if (isActivated) return;

        // Verificar que el collider esté en nuestra lista
        if (!collidersToDestroy.Contains(destroyedCollider))
        {
            return;
        }

        destroyedCount++;

        if (showDebugMessages)
        {
            Debug.Log($"Collider destruido: {destroyedCollider.name}. Total: {destroyedCount}/{collidersToDestroy.Count}", this);
        }

        CheckForActivation();
    }

    void HandleColliderDisabled(Collider2D disabledCollider)
    {
        if (isActivated || !countDisabledColliders) return;

        // Verificar que el collider esté en nuestra lista
        if (!collidersToDestroy.Contains(disabledCollider))
        {
            return;
        }

        destroyedCount++;

        if (showDebugMessages)
        {
            Debug.Log($"Collider desactivado: {disabledCollider.name}. Total: {destroyedCount}/{collidersToDestroy.Count}", this);
        }

        CheckForActivation();
    }

    void CheckForActivation()
    {
        if (destroyedCount >= collidersToDestroy.Count)
        {
            ActivateTargetCollider();
        }
    }

    void ActivateTargetCollider()
    {
        if (colliderToActivate != null && !isActivated)
        {
            // Asegurarse de que el collider esté habilitado
            colliderToActivate.enabled = true;

            // Asegurarse de que el GameObject esté activo
            colliderToActivate.gameObject.SetActive(true);

            isActivated = true;

            if (showDebugMessages)
            {
                Debug.Log($"✅ ¡Todos los colliders destruidos/desactivados! Collider activado: {colliderToActivate.name}", this);

                // Debug adicional del collider objetivo
                MonoBehaviour[] scripts = colliderToActivate.GetComponents<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    if (script != null && script.enabled)
                    {
                        Debug.Log($"Script activo en collider objetivo: {script.GetType().Name}");
                    }
                }
            }
        }
    }

    [ContextMenu("Forzar Verificación")]
    public void ForceCheck()
    {
        destroyedCount = 0;
        CheckInitialState();

        if (showDebugMessages)
        {
            Debug.Log($"Verificación forzada. Estado: {destroyedCount}/{collidersToDestroy.Count} colliders destruidos/desactivados");
        }
    }

    [ContextMenu("Forzar Activación")]
    public void ForceActivation()
    {
        ActivateTargetCollider();
    }

    public void CheckCurrentStatus()
    {
        int actualDestroyed = 0;
        foreach (Collider2D collider in collidersToDestroy)
        {
            if (collider == null)
            {
                actualDestroyed++;
            }
            else if (!collider.enabled || !collider.gameObject.activeInHierarchy)
            {
                actualDestroyed++;
            }
        }

        Debug.Log($"Estado actual: {actualDestroyed}/{collidersToDestroy.Count} colliders destruidos/desactivados");
        Debug.Log($"Collider objetivo activo: {isActivated}");
        Debug.Log($"DestroyedCount interno: {destroyedCount}");

        if (colliderToActivate != null)
        {
            Debug.Log($"Collider objetivo - Enabled: {colliderToActivate.enabled}, GameObject Active: {colliderToActivate.gameObject.activeInHierarchy}");
        }
    }

    [ContextMenu("Reset System")]
    public void ResetSystem()
    {
        destroyedCount = 0;
        isActivated = false;

        // NO desactivar el collider objetivo al resetear
        // colliderToActivate.enabled = false;

        // Re-suscribirse a los eventos
        foreach (Collider2D collider in collidersToDestroy)
        {
            if (collider != null)
            {
                ColliderDestructor destructor = collider.GetComponent<ColliderDestructor>();
                if (destructor != null)
                {
                    destructor.OnColliderDestroyed -= HandleColliderDestroyed;
                    destructor.OnColliderDestroyed += HandleColliderDestroyed;

                    destructor.OnColliderDisabled -= HandleColliderDisabled;
                    destructor.OnColliderDisabled += HandleColliderDisabled;
                }
            }
        }

        if (showDebugMessages)
        {
            Debug.Log("Sistema reseteado", this);
        }
    }

    void OnDestroy()
    {
        foreach (Collider2D collider in collidersToDestroy)
        {
            if (collider != null)
            {
                ColliderDestructor destructor = collider.GetComponent<ColliderDestructor>();
                if (destructor != null)
                {
                    destructor.OnColliderDestroyed -= HandleColliderDestroyed;
                    destructor.OnColliderDisabled -= HandleColliderDisabled;
                }
            }
        }
    }
}

// Componente auxiliar mejorado
public class ColliderDestructor : MonoBehaviour
{
    public System.Action<Collider2D> OnColliderDestroyed;
    public System.Action<Collider2D> OnColliderDisabled;
    public bool countDisabled = true;

    private Collider2D trackedCollider;
    private bool wasEnabled = true;

    void Start()
    {
        trackedCollider = GetComponent<Collider2D>();
        if (trackedCollider != null)
        {
            wasEnabled = trackedCollider.enabled;
        }
    }

    void Update()
    {
        if (trackedCollider != null && countDisabled)
        {
            if (wasEnabled && !trackedCollider.enabled)
            {
                OnColliderDisabled?.Invoke(trackedCollider);
                wasEnabled = false;
            }
        }
    }

    void OnDestroy()
    {
        if (trackedCollider != null)
        {
            OnColliderDestroyed?.Invoke(trackedCollider);
        }
    }

    void OnDisable()
    {
        if (trackedCollider != null && countDisabled && gameObject.activeInHierarchy == false)
        {
            OnColliderDisabled?.Invoke(trackedCollider);
        }
    }
}