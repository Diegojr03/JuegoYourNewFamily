using UnityEngine;

public class FootstepZone : MonoBehaviour
{
    [Header("Configuración de la Zona")]
    [SerializeField] private string zoneName = "Nueva Zona";

    [Header("Sonido de Pisada")]
    [SerializeField] public AudioClip footstepSound;

    [Header("Comportamiento")]
    [SerializeField] public bool playOnEnter = true; // Reproducir un paso al entrar
    [SerializeField] private bool useDefaultOnExit = true; // Usar sonido por defecto al salir

    [Header("Visual (opcional)")]
    [SerializeField] private Color zoneColor = new Color(0, 1, 0, 0.2f); // Color del Gizmo

    private FootstepManager footstepManager;
    private int currentFootstepHash = 0; // Para detectar cambios

    void Start()
    {
        // Verificar que tiene un collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("FootstepZone necesita un Collider2D en " + gameObject.name);
            return;
        }

        // Verificar que sea trigger
        if (!col.isTrigger)
        {
            Debug.LogWarning("Se recomienda que el collider de " + gameObject.name + " sea Trigger");
        }

        // Verificar que tiene un sonido asignado
        if (footstepSound == null)
        {
            Debug.LogWarning("No se ha asignado un sonido de pisada para la zona: " + gameObject.name);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Obtener o agregar el FootstepManager al jugador
            footstepManager = other.GetComponent<FootstepManager>();
            if (footstepManager == null)
            {
                footstepManager = other.gameObject.AddComponent<FootstepManager>();
            }

            // Asignar esta zona al manager
            footstepManager.SetZone(this);

            // Actualizar hash para detectar cambios futuros
            currentFootstepHash = GetFootstepHash();

            Debug.Log("Jugador entró en zona: " + zoneName);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            footstepManager = other.GetComponent<FootstepManager>();
            if (footstepManager != null)
            {
                if (useDefaultOnExit)
                {
                    footstepManager.ExitZone();
                }
                else
                {
                    // Mantener la zona actual pero sin sonido (silencioso)
                    footstepManager.SetZone(null);
                }
            }

            Debug.Log("Jugador salió de zona: " + zoneName);
        }
    }

    // Método para verificar si el sonido cambió
    public bool HasChanged()
    {
        int newHash = GetFootstepHash();
        bool changed = newHash != currentFootstepHash;
        currentFootstepHash = newHash;
        return changed;
    }

    int GetFootstepHash()
    {
        if (footstepSound == null) return 0;
        return footstepSound.GetHashCode();
    }

    // Visualización en el editor
    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = zoneColor;

            if (col is BoxCollider2D box)
            {
                Gizmos.DrawCube(box.bounds.center, box.bounds.size);
            }
            else if (col is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(circle.bounds.center, circle.radius);
            }
            else if (col is PolygonCollider2D poly)
            {
                // Dibujar el polígono
                Vector2[] points = poly.points;
                if (points.Length > 2)
                {
                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        Gizmos.DrawLine(
                            (Vector2)transform.position + points[i],
                            (Vector2)transform.position + points[i + 1]
                        );
                    }
                    Gizmos.DrawLine(
                        (Vector2)transform.position + points[points.Length - 1],
                        (Vector2)transform.position + points[0]
                    );
                }
            }
        }
    }
}