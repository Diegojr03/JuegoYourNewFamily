using UnityEngine;

public class PersistentBacklog : MonoBehaviour
{
    private static GameObject persistentPanel;
    private static BacklogManager persistentManager;

    void Awake()
    {
        if (persistentPanel == null)
        {
            // Guardar referencia al panel actual
            persistentPanel = gameObject;
            persistentManager = GetComponent<BacklogManager>();

            if (persistentManager == null)
                persistentManager = FindObjectOfType<BacklogManager>();

            // Hacer persistente este panel
            DontDestroyOnLoad(gameObject);

            Debug.Log("BacklogPanel hecho persistente por primera vez");
        }
        else
        {
            // Ya existe un panel persistente, destruir este
            Debug.Log("Destruyendo panel duplicado");

            // Antes de destruir, actualizar la referencia en el manager si es necesario
            BacklogManager localManager = GetComponent<BacklogManager>();
            if (localManager != null && persistentManager != null)
            {
                persistentManager.backlogPanel = persistentPanel;
            }

            Destroy(gameObject);
        }
    }

    void OnLevelWasLoaded(int level)
    {
        // Cada vez que se carga una escena, asegurar que el panel sigue conectado
        if (persistentPanel != null && persistentManager != null)
        {
            // Buscar el BacklogManager de la nueva escena y actualizar su referencia
            BacklogManager[] managers = FindObjectsOfType<BacklogManager>();
            foreach (BacklogManager manager in managers)
            {
                if (manager != persistentManager)
                {
                    manager.backlogPanel = persistentPanel;
                    Debug.Log("Referencia de backlogPanel actualizada en nuevo manager");
                }
            }
        }
    }
}