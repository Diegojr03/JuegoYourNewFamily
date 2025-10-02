using UnityEngine;

public class Estatua : MonoBehaviour
{
    public enum Direccion { Arriba, Abajo, Izquierda, Derecha }

    [Header("Configuración Estatua")]
    public Direccion direccionInicial = Direccion.Arriba;
    public int estatuaID = 0;

    [Header("Sprites para cada dirección")]
    public Sprite spriteArriba;
    public Sprite spriteAbajo;
    public Sprite spriteIzquierda;
    public Sprite spriteDerecha;

    [Header("Sprite para tecla E")]
    public Sprite spriteTeclaE; // Sprite que muestra la tecla E

    [Header("Configuración Tecla E")]
    public Vector3 posicionTeclaE = new Vector3(0, 1.5f, 0); // Posición relativa
    public Vector3 escalaTeclaE = new Vector3(0.25f, 0.25f, 0.25f); // Tamaño del sprite
    public float velocidadAnimacion = 3f; // Velocidad de la animación flotante
    public float amplitudAnimacion = 0.1f; // Qué tan alto llega la animación

    [Header("Referencias")]
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer spriteTeclaERenderer; // SpriteRenderer para mostrar la tecla E

    // Evento para notificar cuando una estatua cambia de dirección
    public System.Action<Estatua> OnDireccionCambiada;

    private Direccion direccionActual;
    private bool jugadorCerca = false;
    private GameObject teclaEObj; // Referencia al GameObject de la tecla E
    private Vector3 ultimaPosicionTeclaE;
    private Vector3 ultimaEscalaTeclaE;
    private Sprite ultimoSpriteTeclaE;

    // Propiedad pública para acceder a la dirección actual
    public Direccion DireccionActual
    {
        get => direccionActual;
        private set
        {
            direccionActual = value;
            OnDireccionCambiada?.Invoke(this);
        }
    }

    void Start()
    {
        InicializarEstatua();
    }

    void Update()
    {
        // Verificar si el jugador está cerca y presiona E
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            RotarEstatua();
        }

        // Verificar cambios en las variables del inspector
        VerificarCambios();

        // Animación flotante para la tecla E cuando está visible
        if (jugadorCerca && spriteTeclaERenderer != null && spriteTeclaERenderer.enabled)
        {
            // Movimiento suave arriba y abajo
            float offsetY = Mathf.Sin(Time.time * velocidadAnimacion) * amplitudAnimacion;
            Vector3 nuevaPosicion = posicionTeclaE + new Vector3(0, offsetY, 0);
            if (teclaEObj != null)
            {
                teclaEObj.transform.localPosition = nuevaPosicion;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            Debug.Log($"Jugador cerca de estatua {estatuaID}");

            // Mostrar indicador de tecla E
            MostrarIndicadorTeclaE(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            Debug.Log($"Jugador se alejó de estatua {estatuaID}");

            // Ocultar indicador de tecla E
            MostrarIndicadorTeclaE(false);

            // Restablecer posición original cuando el jugador se va
            if (teclaEObj != null)
            {
                teclaEObj.transform.localPosition = posicionTeclaE;
            }
        }
    }

    private void InicializarEstatua()
    {
        // Validar que tenemos el SpriteRenderer de la estatua
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError($"No se encontró SpriteRenderer en la estatua {estatuaID}");
            }
        }

        // Configurar el SpriteRenderer para la tecla E
        if (spriteTeclaERenderer == null)
        {
            // Crear un GameObject hijo para la tecla E si no existe
            teclaEObj = new GameObject("TeclaE_Indicator");
            teclaEObj.transform.SetParent(transform);
            teclaEObj.transform.localPosition = posicionTeclaE;
            teclaEObj.transform.localScale = escalaTeclaE;

            spriteTeclaERenderer = teclaEObj.AddComponent<SpriteRenderer>();
            spriteTeclaERenderer.sprite = spriteTeclaE;
            spriteTeclaERenderer.sortingOrder = 10; // Para que aparezca por encima de la estatua
        }
        else
        {
            // Si ya existe una referencia, obtener el GameObject padre
            teclaEObj = spriteTeclaERenderer.gameObject;
            // Aplicar la escala y posición configuradas
            teclaEObj.transform.localPosition = posicionTeclaE;
            teclaEObj.transform.localScale = escalaTeclaE;
        }

        // Guardar los valores actuales para detectar cambios
        ultimaPosicionTeclaE = posicionTeclaE;
        ultimaEscalaTeclaE = escalaTeclaE;
        ultimoSpriteTeclaE = spriteTeclaE;

        // Ocultar indicador al inicio
        if (spriteTeclaERenderer != null)
        {
            spriteTeclaERenderer.enabled = false;
        }

        DireccionActual = direccionInicial;
        ActualizarSprite();
    }

    private void VerificarCambios()
    {
        // Verificar si cambió la posición
        if (teclaEObj != null && posicionTeclaE != ultimaPosicionTeclaE)
        {
            teclaEObj.transform.localPosition = posicionTeclaE;
            ultimaPosicionTeclaE = posicionTeclaE;
        }

        // Verificar si cambió la escala
        if (teclaEObj != null && escalaTeclaE != ultimaEscalaTeclaE)
        {
            teclaEObj.transform.localScale = escalaTeclaE;
            ultimaEscalaTeclaE = escalaTeclaE;
        }

        // Verificar si cambió el sprite
        if (spriteTeclaERenderer != null && spriteTeclaE != ultimoSpriteTeclaE)
        {
            spriteTeclaERenderer.sprite = spriteTeclaE;
            ultimoSpriteTeclaE = spriteTeclaE;
        }
    }

    public void RotarEstatua()
    {
        // Rotar 90 grados (siguiente dirección en sentido horario)
        switch (DireccionActual)
        {
            case Direccion.Arriba:
                DireccionActual = Direccion.Derecha;
                break;
            case Direccion.Derecha:
                DireccionActual = Direccion.Abajo;
                break;
            case Direccion.Abajo:
                DireccionActual = Direccion.Izquierda;
                break;
            case Direccion.Izquierda:
                DireccionActual = Direccion.Arriba;
                break;
        }

        ActualizarSprite();
        Debug.Log($"Estatua {estatuaID} rotada hacia: {DireccionActual}");
    }

    private void ActualizarSprite()
    {
        if (spriteRenderer != null)
        {
            switch (DireccionActual)
            {
                case Direccion.Arriba:
                    spriteRenderer.sprite = spriteArriba;
                    break;
                case Direccion.Abajo:
                    spriteRenderer.sprite = spriteAbajo;
                    break;
                case Direccion.Izquierda:
                    spriteRenderer.sprite = spriteIzquierda;
                    break;
                case Direccion.Derecha:
                    spriteRenderer.sprite = spriteDerecha;
                    break;
            }
        }
    }

    private void MostrarIndicadorTeclaE(bool mostrar)
    {
        if (spriteTeclaERenderer != null)
        {
            spriteTeclaERenderer.enabled = mostrar;

            // Asegurarse de que el sprite esté asignado
            if (mostrar && spriteTeclaERenderer.sprite == null && spriteTeclaE != null)
            {
                spriteTeclaERenderer.sprite = spriteTeclaE;
            }
        }
    }

    // Método para forzar una dirección específica (útil para debugging)
    public void SetDireccion(Direccion nuevaDireccion)
    {
        DireccionActual = nuevaDireccion;
        ActualizarSprite();
    }

    // Método para actualizar manualmente la tecla E (útil si cambias valores por código)
    public void ActualizarTeclaE()
    {
        if (teclaEObj != null)
        {
            teclaEObj.transform.localPosition = posicionTeclaE;
            teclaEObj.transform.localScale = escalaTeclaE;
        }
        if (spriteTeclaERenderer != null && spriteTeclaE != null)
        {
            spriteTeclaERenderer.sprite = spriteTeclaE;
        }

        ultimaPosicionTeclaE = posicionTeclaE;
        ultimaEscalaTeclaE = escalaTeclaE;
        ultimoSpriteTeclaE = spriteTeclaE;
    }
}