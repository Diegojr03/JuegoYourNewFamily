using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [Header("Car Settings")]
    public GameObject carPrefab;
    public float spawnInterval = 1f;
    public float carSpeed = 3f;         // Velocidad hacia la DERECHA

    [Header("Destroy Settings")]
    public float destroyXPosition = 20f;  // Posición donde se destruye a la DERECHA
    public Color gizmoColor = Color.red;

    [Header("Dirección del Movimiento")]
    public bool moveLeftToRight = true;   // ← ¡ESTA ES LA CLAVE!

    private float timer;

    void Start()
    {
        Debug.Log("=== CONFIGURACIÓN ACTUAL ===");
        Debug.Log("Spawner posición: " + transform.position);
        Debug.Log("Destroy X: " + destroyXPosition);
        Debug.Log("Movimiento: " + (moveLeftToRight ? "Izquierda → Derecha" : "Derecha → Izquierda"));

        // Verificación automática
        if (moveLeftToRight)
        {
            if (transform.position.x >= destroyXPosition)
            {
                Debug.LogError("❌ ERROR: Para movimiento Izq→Der:");
                Debug.LogError("   Spawner X debe ser MENOR que Destroy X");
                Debug.LogError("   Actual: " + transform.position.x + " >= " + destroyXPosition);
            }
            else
            {
                Debug.Log("✓ Configuración correcta para Izq→Der");
            }
        }
        else
        {
            if (transform.position.x <= destroyXPosition)
            {
                Debug.LogError("❌ ERROR: Para movimiento Der→Izq:");
                Debug.LogError("   Spawner X debe ser MAYOR que Destroy X");
                Debug.LogError("   Actual: " + transform.position.x + " <= " + destroyXPosition);
            }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnCar();
            timer = 0f;
        }
    }

    private void SpawnCar()
    {
        if (carPrefab == null)
        {
            Debug.LogError("No hay prefab asignado!");
            return;
        }

        GameObject car = Instantiate(carPrefab, transform.position, Quaternion.identity);
        car.name = "Car_" + Time.time;

        // Añadir movimiento en la dirección correcta
        CarMoverDirectional mover = car.AddComponent<CarMoverDirectional>();
        mover.Initialize(carSpeed, destroyXPosition, moveLeftToRight);

        Debug.Log($"🚗 Coche creado en X={transform.position.x}");
        Debug.Log($"   Destruirá en X={destroyXPosition}");
        Debug.Log($"   Dirección: {(moveLeftToRight ? "→" : "←")}");
    }

    void OnDrawGizmos()
    {
        // Spawner (verde)
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1, 2, 1));

        // Línea de destrucción (roja)
        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(
            new Vector3(destroyXPosition, transform.position.y - 5f, 0),
            new Vector3(destroyXPosition, transform.position.y + 5f, 0)
        );

        // Flecha de dirección
        Gizmos.color = Color.blue;
        if (moveLeftToRight)
        {
            // Izquierda a derecha
            Vector3 start = transform.position;
            Vector3 end = new Vector3(destroyXPosition, transform.position.y, 0);
            DrawArrow(start, end, Color.blue);
        }
        else
        {
            // Derecha a izquierda
            Vector3 start = new Vector3(destroyXPosition, transform.position.y, 0);
            Vector3 end = transform.position;
            DrawArrow(start, end, Color.blue);
        }
    }

    void DrawArrow(Vector3 start, Vector3 end, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(start, end);

        Vector3 direction = (end - start).normalized;
        float arrowSize = 0.5f;

        Vector3 right = Quaternion.Euler(0, 0, 30) * direction * arrowSize;
        Vector3 left = Quaternion.Euler(0, 0, -30) * direction * arrowSize;

        Gizmos.DrawLine(end, end - right);
        Gizmos.DrawLine(end, end - left);
    }
}

// Componente de movimiento DIRECCIONAL
public class CarMoverDirectional : MonoBehaviour
{
    private float speed;
    private float destroyX;
    private bool moveRight; // true = derecha, false = izquierda

    public void Initialize(float carSpeed, float destroyPosition, bool moveLeftToRight)
    {
        speed = carSpeed;
        destroyX = destroyPosition;
        moveRight = moveLeftToRight;

        Debug.Log($"🚗 Inicializado: Vel={speed}, Destroy={destroyX}, Dir={(moveRight ? "Derecha" : "Izquierda")}");
    }

    void Update()
    {
        if (moveRight)
        {
            // Moverse hacia la DERECHA
            transform.Translate(Vector3.right * speed * Time.deltaTime);

            // Destruir si pasa la posición a la DERECHA
            if (transform.position.x >= destroyX)
            {
                Debug.Log($"🗑️ Destruyendo {gameObject.name} en X={transform.position.x}");
                Destroy(gameObject);
            }
        }
        else
        {
            // Moverse hacia la IZQUIERDA
            transform.Translate(Vector3.left * speed * Time.deltaTime);

            // Destruir si pasa la posición a la IZQUIERDA
            if (transform.position.x <= destroyX)
            {
                Debug.Log($"🗑️ Destruyendo {gameObject.name} en X={transform.position.x}");
                Destroy(gameObject);
            }
        }
    }
}