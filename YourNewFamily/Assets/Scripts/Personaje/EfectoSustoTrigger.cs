using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EfectoSustoTrigger : MonoBehaviour
{
    [Header("Configuración del Shake de Cámara")]
    [SerializeField] private float duracionShake = 0.3f;
    [SerializeField] private float intensidadShake = 0.5f;
    [SerializeField] private float intervaloShakes = 2f;

    [Header("Configuración del Panel Rojo")]
    [SerializeField] private Image panelRojo;
    [SerializeField] private float opacidadMaxima = 0.75f;
    [SerializeField] private float duracionAnimacionPanel = 1.5f;

    private Camera mainCamera;
    private Vector3 posicionOriginalCamara;
    private bool efectoActivado = false;
    private Coroutine cicloEfectos;

    private void Start()
    {
        // Buscar la cámara si no está asignada
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            posicionOriginalCamara = mainCamera.transform.position;
            Debug.Log("Cámara encontrada: " + mainCamera.name);
        }
        else
        {
            Debug.LogError("ERROR: No se encontró ninguna cámara con tag 'MainCamera'");
        }

        // Buscar el panel si no está asignado
        if (panelRojo == null)
        {
            // Intentar encontrar automáticamente
            panelRojo = FindObjectOfType<Image>();
            if (panelRojo != null)
                Debug.Log("Panel encontrado automáticamente: " + panelRojo.name);
            else
                Debug.LogError("ERROR: No hay ningún panel asignado en el script");
        }

        // Configurar el panel
        if (panelRojo != null)
        {
            Color color = panelRojo.color;
            color.a = 0f;
            panelRojo.color = color;
            Debug.Log("Panel configurado correctamente, alpha inicial: " + color.a);
        }

        Debug.Log("Script inicializado correctamente en: " + gameObject.name);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Algo entró en el trigger: " + collision.name + " (Tag: " + collision.tag + ")");

        if (collision.CompareTag("Player"))
        {
            if (mainCamera != null)
                posicionOriginalCamara = mainCamera.transform.localPosition;
            Debug.Log("¡EL JUGADOR HA ENTRADO! Activando efecto de miedo...");

            if (!efectoActivado)
            {
                efectoActivado = true;

                // Detener cualquier ciclo anterior
                if (cicloEfectos != null)
                    StopCoroutine(cicloEfectos);

                cicloEfectos = StartCoroutine(CicloDeEfectos());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Jugador salió del trigger. Deteniendo efectos.");
            efectoActivado = false;

            if (cicloEfectos != null)
                StopCoroutine(cicloEfectos);

            // Restaurar todo
            if (mainCamera != null)
                mainCamera.transform.localPosition = posicionOriginalCamara;

            if (panelRojo != null)
            {
                Color color = panelRojo.color;
                color.a = 0f;
                panelRojo.color = color;
            }
        }
    }

    private IEnumerator CicloDeEfectos()
    {
        Debug.Log("Ciclo de efectos iniciado");

        // Hacer un efecto inmediato al entrar
        Debug.Log("Ejecutando efecto inicial");
        StartCoroutine(ShakeCamara());
        StartCoroutine(AnimacionPanelRojo());

        // Continuar con el ciclo cada X segundos
        while (efectoActivado)
        {
            yield return new WaitForSeconds(intervaloShakes);

            if (!efectoActivado) yield break;

            Debug.Log("Ejecutando efecto de intervalo (" + intervaloShakes + " segundos)");
            StartCoroutine(ShakeCamara());
            StartCoroutine(AnimacionPanelRojo());
        }
    }

    private IEnumerator ShakeCamara()
    {
        if (mainCamera == null)
        {
            Debug.LogError("No hay cámara para hacer shake");
            yield break;
        }

        Debug.Log("Iniciando shake de cámara");
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionShake)
        {
            float x = Random.Range(-intensidadShake, intensidadShake);
            float y = Random.Range(-intensidadShake, intensidadShake);
            mainCamera.transform.localPosition = posicionOriginalCamara + new Vector3(x, y, 0);

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.localPosition = posicionOriginalCamara;
        Debug.Log("Shake finalizado");
    }

    private IEnumerator AnimacionPanelRojo()
    {
        if (panelRojo == null)
        {
            Debug.LogError("No hay panel para animar");
            yield break;
        }

        Debug.Log("Iniciando animación del panel rojo");

        float tiempoTranscurrido = 0f;
        Color color = panelRojo.color;

        // Subir opacidad
        while (tiempoTranscurrido < duracionAnimacionPanel / 2)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / (duracionAnimacionPanel / 2);
            color.a = Mathf.Lerp(0f, opacidadMaxima, t);
            panelRojo.color = color;
            yield return null;
        }

        // Bajar opacidad
        tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracionAnimacionPanel / 2)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / (duracionAnimacionPanel / 2);
            color.a = Mathf.Lerp(opacidadMaxima, 0f, t);
            panelRojo.color = color;
            yield return null;
        }

        color.a = 0f;
        panelRojo.color = color;
        Debug.Log("Animación del panel finalizada");
    }
}