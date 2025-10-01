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

    [Header("Referencias")]
    public SpriteRenderer spriteRenderer;

    // Evento para notificar cuando una estatua cambia de dirección
    public System.Action<Estatua> OnDireccionCambiada;

    private Direccion direccionActual;
    private bool jugadorCerca = false;

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
        // Validar que tenemos el SpriteRenderer
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError($"No se encontró SpriteRenderer en la estatua {estatuaID}");
            }
        }

        DireccionActual = direccionInicial;
        ActualizarSprite();
    }

    void Update()
    {
        // Verificar si el jugador está cerca y presiona E
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            RotarEstatua();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            Debug.Log($"Jugador cerca de estatua {estatuaID}");

            // Mostrar UI indicador (opcional)
            MostrarIndicador(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            Debug.Log($"Jugador se alejó de estatua {estatuaID}");

            // Ocultar UI indicador (opcional)
            MostrarIndicador(false);
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

    private void MostrarIndicador(bool mostrar)
    {
        // Aquí puedes implementar un indicador visual (como un "Presiona E")
        // Por ejemplo, activar/desactivar un GameObject hijo con el texto
    }

    // Método para forzar una dirección específica (útil para debugging)
    public void SetDireccion(Direccion nuevaDireccion)
    {
        DireccionActual = nuevaDireccion;
        ActualizarSprite();
    }

    // Método para obtener el sprite actual (útil para el manager)
    public Sprite GetSpriteActual()
    {
        return spriteRenderer.sprite;
    }
}