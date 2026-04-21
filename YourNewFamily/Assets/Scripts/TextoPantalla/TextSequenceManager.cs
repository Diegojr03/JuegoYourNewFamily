using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextSequenceManager : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private GameObject[] textos; // Array de GameObjects de texto

    [Header("Tiempos")]
    [SerializeField] private float tiempoEstancia = 3f; // Tiempo que cada texto permanece visible
    [SerializeField] private float tiempoFadeIn = 0.5f; // Duración del fade in
    [SerializeField] private float tiempoFadeOut = 0.5f; // Duración del fade out
    [SerializeField] private float tiempoPrimerTexto = 0f; // Tiempo antes de mostrar el primer texto

    [Header("Shake")]
    [SerializeField] private float intensidadShake = 5f; // Intensidad de la vibración
    [SerializeField] private float velocidadShake = 30f; // Velocidad de la vibración

    [Header("Collider")]
    [SerializeField] private Collider2D zonaCollider; // Zona que activa el panel

    [Header("Panel")]
    [SerializeField] private GameObject panelTexto; // Panel que contiene los textos

    private List<int> ordenAleatorio = new List<int>();
    private Queue<int> colaTextos = new Queue<int>();
    private bool secuenciaActiva = false;
    private bool jugadorEnZona = false;
    private Coroutine rutinaSecuencia;
    private Dictionary<GameObject, Coroutine> shakesActivos = new Dictionary<GameObject, Coroutine>();
    private Dictionary<GameObject, Vector3> posicionesOriginales = new Dictionary<GameObject, Vector3>();

    void Start()
    {
        // Guardar posiciones originales y asegurar que todos los textos empiecen ocultos
        foreach (GameObject texto in textos)
        {
            if (texto != null)
            {
                // Guardar posición original
                posicionesOriginales[texto] = texto.transform.localPosition;

                texto.SetActive(false);
                CanvasGroup canvasGroup = texto.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = texto.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
            }
        }

        // Generar orden aleatorio inicial
        GenerarOrdenAleatorio();

        // Iniciar cola
        foreach (int indice in ordenAleatorio)
        {
            colaTextos.Enqueue(indice);
        }
    }

    void Update()
    {
        // Verificar si el jugador está dentro del collider
        bool jugadorDentro = VerificarJugadorEnZona();

        if (jugadorDentro && !secuenciaActiva)
        {
            // Jugador entró: activar panel y empezar secuencia
            ActivarSecuencia();
        }
        else if (!jugadorDentro && secuenciaActiva)
        {
            // Jugador salió: desactivar todo
            DesactivarSecuencia();
        }
    }

    private bool VerificarJugadorEnZona()
    {
        if (zonaCollider == null)
        {
            Debug.LogError("No se ha asignado un Collider2D en el inspector");
            return false;
        }

        // Verificar si hay un objeto con tag "Player" dentro del collider
        Collider2D[] colliders = Physics2D.OverlapPointAll(transform.position);
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Player"))
                return true;
        }

        // Método alternativo usando Trigger
        return jugadorEnZona;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnZona = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnZona = false;
        }
    }

    private void ActivarSecuencia()
    {
        secuenciaActiva = true;
        if (panelTexto != null)
            panelTexto.SetActive(true);

        // Iniciar la secuencia después del tiempo especificado
        rutinaSecuencia = StartCoroutine(SecuenciaTextos());
    }

    private void DesactivarSecuencia()
    {
        secuenciaActiva = false;
        if (rutinaSecuencia != null)
            StopCoroutine(rutinaSecuencia);

        // Detener todos los shakes activos y restaurar posiciones
        foreach (var shake in shakesActivos)
        {
            if (shake.Value != null)
                StopCoroutine(shake.Value);
            if (shake.Key != null && posicionesOriginales.ContainsKey(shake.Key))
                shake.Key.transform.localPosition = posicionesOriginales[shake.Key];
        }
        shakesActivos.Clear();

        // Ocultar todos los textos
        foreach (GameObject texto in textos)
        {
            if (texto != null)
            {
                texto.SetActive(false);
                // Restaurar posición por si acaso
                if (posicionesOriginales.ContainsKey(texto))
                    texto.transform.localPosition = posicionesOriginales[texto];
            }
        }

        if (panelTexto != null)
            panelTexto.SetActive(false);
    }

    private IEnumerator SecuenciaTextos()
    {
        // Esperar el tiempo antes del primer texto
        yield return new WaitForSeconds(tiempoPrimerTexto);

        List<GameObject> textosActivos = new List<GameObject>();
        List<Coroutine> corutinasFade = new List<Coroutine>();

        while (secuenciaActiva)
        {
            // Obtener siguiente texto
            if (colaTextos.Count == 0)
            {
                // Regenerar orden aleatorio cuando se acaben
                GenerarOrdenAleatorio();
                foreach (int indice in ordenAleatorio)
                {
                    colaTextos.Enqueue(indice);
                }
            }

            int indiceTexto = colaTextos.Dequeue();
            GameObject textoActual = textos[indiceTexto];

            // Restaurar posición original antes de mostrar
            if (posicionesOriginales.ContainsKey(textoActual))
                textoActual.transform.localPosition = posicionesOriginales[textoActual];

            // Mostrar nuevo texto con fade in
            textoActual.SetActive(true);
            StartCoroutine(FadeTexto(textoActual, 0f, 1f, tiempoFadeIn));
            textosActivos.Add(textoActual);

            // Iniciar animación de shake para este texto
            Coroutine shakeCoroutine = StartCoroutine(ShakeTexto(textoActual));
            shakesActivos[textoActual] = shakeCoroutine;

            // Esperar el tiempo de estancia
            yield return new WaitForSeconds(tiempoEstancia);

            // Si hay más de 1 texto activo, empezar fade out del más antiguo
            if (textosActivos.Count > 1)
            {
                GameObject textoAntiguo = textosActivos[0];
                textosActivos.RemoveAt(0);

                // Detener el shake del texto antiguo y restaurar posición
                if (shakesActivos.ContainsKey(textoAntiguo))
                {
                    if (shakesActivos[textoAntiguo] != null)
                        StopCoroutine(shakesActivos[textoAntiguo]);

                    // Restaurar posición original
                    if (posicionesOriginales.ContainsKey(textoAntiguo))
                        textoAntiguo.transform.localPosition = posicionesOriginales[textoAntiguo];

                    shakesActivos.Remove(textoAntiguo);
                }

                StartCoroutine(FadeTexto(textoAntiguo, 1f, 0f, tiempoFadeOut));
            }
        }
    }

    private IEnumerator ShakeTexto(GameObject texto)
    {
        // Asegurar que tenemos la posición original
        if (!posicionesOriginales.ContainsKey(texto))
            posicionesOriginales[texto] = texto.transform.localPosition;

        Vector3 posicionOriginal = posicionesOriginales[texto];

        while (texto.activeSelf && texto != null)
        {
            // Movimiento de vibración aleatorio
            float offsetX = Random.Range(-intensidadShake, intensidadShake);
            float offsetY = Random.Range(-intensidadShake, intensidadShake);

            texto.transform.localPosition = posicionOriginal + new Vector3(offsetX, offsetY, 0);

            // Velocidad del shake
            yield return new WaitForSeconds(1f / velocidadShake);
        }

        // Restaurar posición original cuando termina
        if (texto != null && posicionesOriginales.ContainsKey(texto))
            texto.transform.localPosition = posicionesOriginales[texto];
    }

    private IEnumerator FadeTexto(GameObject texto, float alphaInicial, float alphaFinal, float duracion)
    {
        CanvasGroup canvasGroup = texto.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = texto.AddComponent<CanvasGroup>();
        }

        float tiempoTranscurrido = 0f;
        canvasGroup.alpha = alphaInicial;

        while (tiempoTranscurrido < duracion)
        {
            if (texto == null) yield break;

            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / duracion;
            canvasGroup.alpha = Mathf.Lerp(alphaInicial, alphaFinal, t);
            yield return null;
        }

        canvasGroup.alpha = alphaFinal;

        // Si el fade es hacia 0, desactivar el GameObject
        if (alphaFinal == 0f && texto != null)
        {
            texto.SetActive(false);
        }
    }

    private void GenerarOrdenAleatorio()
    {
        ordenAleatorio.Clear();
        for (int i = 0; i < textos.Length; i++)
        {
            ordenAleatorio.Add(i);
        }

        // Mezclar aleatoriamente (Fisher-Yates)
        for (int i = ordenAleatorio.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = ordenAleatorio[i];
            ordenAleatorio[i] = ordenAleatorio[j];
            ordenAleatorio[j] = temp;
        }
    }
}