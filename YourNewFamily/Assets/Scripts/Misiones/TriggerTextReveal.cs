using UnityEngine;
using TMPro;
using System.Collections;

public class TriggerTextReveal : MonoBehaviour
{
    [Header("Identificador (opcional)")]
    public string triggerId = "";

    [TextArea]
    public string textoNuevo;
    public TMP_Text uiText;

    public float duracionRevelado = 1.2f;
    public float escalaInicial = 0.7f;
    public float escalaFinal = 1.0f;
    public float sacudidaIntensidad = 0.3f;
    public float sacudidaDuracion = 0.15f;

    private bool activado = false;

    void Start()
    {
        // 🔥 CORRECCIÓN: Comprobamos si hay texto de misión guardado en SaveManager
        if (SaveManager.Instance != null && uiText != null)
        {
            if (SaveManager.Instance.TryGetLastMissionText(out string savedText))
            {
                uiText.text = savedText;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activado) return;
        if (!other.CompareTag("Player")) return;

        activado = true;

        // Registramos el texto en el SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterMissionText(textoNuevo);
        }

        if (uiText == null)
        {
            Debug.LogWarning("uiText no está asignado en " + gameObject.name);
            // Si no hay UI, desactivamos el objeto directamente para no causar errores
            gameObject.SetActive(false);
            return;
        }

        uiText.text = textoNuevo;

        // Apagamos el collider inmediatamente para evitar dobles detecciones
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StopAllCoroutines();
        StartCoroutine(EfectoBonitoYDesactivar(uiText));
    }

    IEnumerator EfectoBonitoYDesactivar(TMP_Text text)
    {
        text.ForceMeshUpdate();
        int total = text.textInfo.characterCount;
        text.maxVisibleCharacters = 0;

        Vector3 escalaOriginal = text.transform.localScale;
        text.transform.localScale = escalaOriginal * escalaInicial;

        Color c = text.color;
        c.a = 0;
        text.color = c;

        float t = 0f;
        while (t < duracionRevelado)
        {
            float p = Mathf.Clamp01(t / duracionRevelado);
            c.a = p;
            text.color = c;
            int visibles = Mathf.FloorToInt(total * p);
            text.maxVisibleCharacters = visibles;
            text.transform.localScale = Vector3.Lerp(escalaOriginal * escalaInicial, escalaOriginal * escalaFinal, p);
            t += Time.deltaTime;
            yield return null;
        }

        c.a = 1f;
        text.color = c;
        text.maxVisibleCharacters = total;
        text.transform.localScale = escalaOriginal * escalaFinal;

        yield return StartCoroutine(Sacudida(text.transform));

        // 🟢 NUEVO: Una vez termina toda la animación y sacudida, desactivamos este GameObject
        gameObject.SetActive(false);
    }

    IEnumerator Sacudida(Transform t)
    {
        Vector3 original = t.localPosition;
        float tiempo = 0f;
        if (sacudidaDuracion <= 0f) yield break;

        while (tiempo < sacudidaDuracion)
        {
            float x = Random.Range(-sacudidaIntensidad, sacudidaIntensidad);
            float y = Random.Range(-sacudidaIntensidad, sacudidaIntensidad);
            t.localPosition = original + new Vector3(x, y, 0f);
            tiempo += Time.deltaTime;
            yield return null;
        }
        t.localPosition = original;
    }
}