using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ValveController : MonoBehaviour
{
    [Header("Tuberías Controladas por esta Válvula")]
    [Tooltip("Arrastra las tuberías que esta válvula controlará")]
    public List<PipeController> tuberiasControladas = new List<PipeController>();

    [Header("Configuración de Animación de Válvula")]
    [Tooltip("Duración de la animación de rotación en segundos")]
    public float duracionAnimacion = 0.3f;

    [Tooltip("Ángulo que rota la válvula al presionarla (grados)")]
    public float anguloRotacion = 45f;

    [Tooltip("Curva de animación (opcional, null = linear)")]
    public AnimationCurve curvaAnimacion = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Configuración Visual (Opcional)")]
    public Sprite spriteNormal;
    public Sprite spritePresionado;

    private SpriteRenderer spriteRenderer;
    private UnityEngine.UI.Button botonComponente;
    private bool animando = false;
    private bool puedeInteractuar = true;
    private Quaternion rotacionOriginal;
    private Quaternion rotacionPresionada;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        botonComponente = GetComponent<UnityEngine.UI.Button>();

        // Guardar rotaciones para la animación
        rotacionOriginal = transform.localRotation;
        rotacionPresionada = rotacionOriginal * Quaternion.Euler(0, 0, -anguloRotacion);

        if (botonComponente != null)
        {
            botonComponente.onClick.AddListener(OnValvePressed);
        }
    }

    public void OnValvePressed()
    {
        // No permitir pulsar si ya está animando
        if (animando || !puedeInteractuar)
        {
            Debug.Log($"Válvula {gameObject.name} ocupada, espera a que termine la animación");
            return;
        }

        StartCoroutine(AnimarValvula());
    }

    private IEnumerator AnimarValvula()
    {
        animando = true;
        puedeInteractuar = false;

        // Desactivar el botón visualmente pero mantenerlo funcional
        if (botonComponente != null)
            botonComponente.interactable = false;

        Debug.Log($"Válvula {gameObject.name} presionada - Animando rotación");

        // ANIMACIÓN DE PRESIÓN (rotar hacia abajo)
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionAnimacion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = curvaAnimacion.Evaluate(tiempoTranscurrido / duracionAnimacion);

            // Interpolar rotación
            transform.localRotation = Quaternion.Slerp(rotacionOriginal, rotacionPresionada, t);

            yield return null;
        }

        transform.localRotation = rotacionPresionada;

        // Cambiar sprite si existe
        if (spriteRenderer != null && spritePresionado != null)
            spriteRenderer.sprite = spritePresionado;

        // Pequeña pausa en la posición presionada
        yield return new WaitForSeconds(0.05f);

        // ANIMACIÓN DE RETORNO (volver a posición original)
        tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionAnimacion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = curvaAnimacion.Evaluate(tiempoTranscurrido / duracionAnimacion);

            // Interpolar rotación de vuelta
            transform.localRotation = Quaternion.Slerp(rotacionPresionada, rotacionOriginal, t);

            yield return null;
        }

        transform.localRotation = rotacionOriginal;

        // Restaurar sprite original
        if (spriteRenderer != null && spriteNormal != null)
            spriteRenderer.sprite = spriteNormal;

        // AHORA SÍ, girar las tuberías (después de la animación)
        Debug.Log($"Válvula {gameObject.name} - Girando {tuberiasControladas.Count} tuberías a la IZQUIERDA");

        foreach (var tuberia in tuberiasControladas)
        {
            if (tuberia != null)
                tuberia.GirarIzquierda();
        }

        // Reactivar el botón
        if (botonComponente != null)
            botonComponente.interactable = true;

        animando = false;
        puedeInteractuar = true;

        Debug.Log($"Válvula {gameObject.name} - Animación completada");
    }

    // Método público para verificar si la válvula está disponible
    public bool EstaDisponible()
    {
        return !animando && puedeInteractuar;
    }
}