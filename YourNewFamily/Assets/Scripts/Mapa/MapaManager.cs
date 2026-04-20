using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapaManager : MonoBehaviour
{
    public static MapaManager Instance;

    [System.Serializable]
    public class ZonaMapa
    {
        public string nombreZona;
        public Image panelNegro;
        public bool descubierta = false;
        public Image cuadradoParpadeante;
    }

    public List<ZonaMapa> zonas;

    [Header("PARPADEO ZONA ACTUAL")]
    public float velocidadParpadeo = 0.8f;

    private Coroutine rutinaParpadeo = null;
    private Image cuadradoActual = null;
    private string nombreZonaActual = "";  // 🔴 NUEVO: Guardar nombre de zona actual

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        foreach (var zona in zonas)
        {
            if (zona.panelNegro != null)
            {
                zona.panelNegro.gameObject.SetActive(true);
                zona.descubierta = false;
            }

            if (zona.cuadradoParpadeante != null)
            {
                // 🔴 CORRECCIÓN 1: Configurar color inicial como TRANSPARENTE
                Color transparente = zona.cuadradoParpadeante.color;
                transparente.a = 0f;
                zona.cuadradoParpadeante.color = transparente;
            }
        }
    }

    public void DesbloquearZona(string nombreZona)
    {
        ZonaMapa zona = zonas.Find(z => z.nombreZona == nombreZona);

        if (zona != null && !zona.descubierta)
        {
            zona.descubierta = true;

            if (zona.panelNegro != null)
                zona.panelNegro.gameObject.SetActive(false);

            Debug.Log($"Zona desbloqueada: {nombreZona}");
        }
        else if (zona == null)
        {
            Debug.LogError($"No se encontró la zona '{nombreZona}' en MapaManager");
        }
    }

    public void ActivarParpadeo(string nombreZona)
    {
        ZonaMapa zona = zonas.Find(z => z.nombreZona == nombreZona);

        if (zona == null || zona.cuadradoParpadeante == null)
            return;

        // 🔴 CORRECCIÓN 2: Si es la misma zona, no hacer nada
        if (nombreZonaActual == nombreZona)
            return;

        // Detener parpadeo anterior y OCULTAR el cuadrado anterior
        if (rutinaParpadeo != null)
        {
            StopCoroutine(rutinaParpadeo);
            if (cuadradoActual != null)
            {
                Color transparente = cuadradoActual.color;
                transparente.a = 0f;
                cuadradoActual.color = transparente;
            }
        }

        // Guardar zona actual
        nombreZonaActual = nombreZona;
        cuadradoActual = zona.cuadradoParpadeante;

        // Iniciar nuevo parpadeo
        rutinaParpadeo = StartCoroutine(Parpadear());
    }

    private IEnumerator Parpadear()
    {
        while (true)
        {
            if (cuadradoActual == null) yield break;

            // 🔴 Fade IN: de transparente a negro (gradual)
            float tiempo = 0f;
            while (tiempo < velocidadParpadeo)
            {
                if (cuadradoActual == null) yield break;

                float alpha = Mathf.Lerp(0f, 1f, tiempo / velocidadParpadeo);
                Color negroConAlpha = new Color(0f, 0f, 0f, alpha);
                cuadradoActual.color = negroConAlpha;

                tiempo += Time.deltaTime;
                yield return null;
            }

            // Asegurar que llegue a negro total
            if (cuadradoActual != null)
                cuadradoActual.color = new Color(0f, 0f, 0f, 1f);

            yield return new WaitForSeconds(velocidadParpadeo * 0.2f); // Pequeña pausa en negro

            if (cuadradoActual == null) yield break;

            // 🔴 Fade OUT: de negro a transparente (gradual)
            tiempo = 0f;
            while (tiempo < velocidadParpadeo)
            {
                if (cuadradoActual == null) yield break;

                float alpha = Mathf.Lerp(1f, 0f, tiempo / velocidadParpadeo);
                Color negroConAlpha = new Color(0f, 0f, 0f, alpha);
                cuadradoActual.color = negroConAlpha;

                tiempo += Time.deltaTime;
                yield return null;
            }

            // Asegurar que llegue a transparente total
            if (cuadradoActual != null)
                cuadradoActual.color = new Color(0f, 0f, 0f, 0f);

            yield return new WaitForSeconds(velocidadParpadeo * 0.2f); // Pequeña pausa en transparente
        }
    }

    public void ReiniciarDescubrimientos()
    {
        foreach (var zona in zonas)
        {
            zona.descubierta = false;
            if (zona.panelNegro != null)
                zona.panelNegro.gameObject.SetActive(true);
        }
        Debug.Log("Todos los descubrimientos de zonas han sido reiniciados");
    }
}