using UnityEngine;
using TMPro;
using System.Collections;

public class TriggerTextReveal : MonoBehaviour
{
    [TextArea]
    public string textoNuevo;
    public TMP_Text uiText;

    public float duracionRevelado = 1.2f;
    public float escalaInicial = 0.7f;
    public float escalaFinal = 1.0f;
    public float sacudidaIntensidad = 0.3f;
    public float sacudidaDuracion = 0.15f;

    private bool activado = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activado) return;
        if (!other.CompareTag("Player")) return;

        activado = true;
        if (uiText == null)
        {
            Debug.LogWarning("uiText no está asignado en " + gameObject.name);
            return;
        }

        uiText.text = textoNuevo;

        StopAllCoroutines();
        StartCoroutine(EfectoBonito(uiText));

        // Desactivar el collider para que no vuelva a activarse
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    IEnumerator EfectoBonito(TMP_Text text)
    {
        text.ForceMeshUpdate();
        int total = text.textInfo.characterCount;
        text.maxVisibleCharacters = 0;

        // Guarda escala original
        Vector3 escalaOriginal = text.transform.localScale;
        text.transform.localScale = escalaOriginal * escalaInicial;

        // Empieza con alpha 0
        Color c = text.color;
        c.a = 0;
        text.color = c;

        float t = 0f;
        while (t < duracionRevelado)
        {
            float p = Mathf.Clamp01(t / duracionRevelado);

            // Fade-in
            c.a = p;
            text.color = c;

            // Revelado suave (no letra a letra exacta, sino progresivo)
            int visibles = Mathf.FloorToInt(total * p);
            text.maxVisibleCharacters = visibles;

            // Escalado "pop"
            text.transform.localScale = Vector3.Lerp(escalaOriginal * escalaInicial, escalaOriginal * escalaFinal, p);

            t += Time.deltaTime;
            yield return null;
        }

        // Asegura final
        c.a = 1f;
        text.color = c;
        text.maxVisibleCharacters = total;
        text.transform.localScale = escalaOriginal * escalaFinal;

        // Llamar sacudida y esperar a que termine
        yield return StartCoroutine(Sacudida(text.transform));

        // Garantía final: dejamos la coroutine con un yield break explícito
        yield break;
    }

    IEnumerator Sacudida(Transform t)
    {
        Vector3 original = t.localPosition;
        float tiempo = 0f;

        // Si la duración es 0 o negativa, salimos inmediatamente (y seguimos siendo un IEnumerator válido)
        if (sacudidaDuracion <= 0f)
        {
            yield break;
        }

        while (tiempo < sacudidaDuracion)
        {
            float x = Random.Range(-sacudidaIntensidad, sacudidaIntensidad);
            float y = Random.Range(-sacudidaIntensidad, sacudidaIntensidad);
            t.localPosition = original + new Vector3(x, y, 0f);

            tiempo += Time.deltaTime;
            yield return null;
        }

        t.localPosition = original;

        // Aseguramos terminar la enumeración correctamente
        yield break;
    }
}