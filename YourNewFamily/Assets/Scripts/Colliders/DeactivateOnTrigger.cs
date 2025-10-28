using UnityEngine;
using System.Collections.Generic;

public class DeactivateOnTrigger : MonoBehaviour
{
    [Header("Objetos a Desactivar")]
    public List<GameObject> objectsToDeactivate = new List<GameObject>();

    [Header("Configuración")]
    public bool destroyThisAfterDeactivate = false;
    public bool showDebug = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Desactivar todos los objetos de la lista
            foreach (GameObject obj in objectsToDeactivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    if (showDebug)
                    {
                        Debug.Log($"Objeto desactivado: {obj.name}");
                    }
                }
            }

            // Destruir este objeto si está configurado
            if (destroyThisAfterDeactivate)
            {
                if (showDebug)
                {
                    Debug.Log($"Destruyendo: {gameObject.name}");
                }
                Destroy(gameObject);
            }
        }
    }
}