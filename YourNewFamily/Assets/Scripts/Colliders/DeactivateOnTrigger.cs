using UnityEngine;
using System.Collections.Generic;

public class DeactivateOnTrigger : MonoBehaviour
{
    [Header("Objetos a ACTIVAR")]
    public List<GameObject> objectsToActivate = new List<GameObject>();

    [Header("Objetos a DESACTIVAR")]
    public List<GameObject> objectsToDeactivate = new List<GameObject>();

    [Header("Configuración")]
    public bool destroyAfterTrigger = false;
    public bool showDebug = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Activar objetos
        foreach (var obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                if (showDebug) Debug.Log("Activado: " + obj.name);
            }
        }

        // Desactivar objetos
        foreach (var obj in objectsToDeactivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                if (showDebug) Debug.Log("Desactivado: " + obj.name);
            }
        }

        // Destruir el trigger si se desea
        if (destroyAfterTrigger)
        {
            if (showDebug) Debug.Log("Destruyendo trigger: " + gameObject.name);
            Destroy(gameObject);
        }
    }
}