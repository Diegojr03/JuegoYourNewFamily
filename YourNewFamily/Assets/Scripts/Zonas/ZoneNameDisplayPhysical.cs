using UnityEngine;
using TMPro;
using System.Collections;

public class ZoneNameDisplayPhysical : MonoBehaviour
{
    [Header("Configuración de la Zona")]
    public string zoneName = "Nombre de la Zona";

    [Header("Referencias UI")]
    public RectTransform panelRect;
    public TextMeshProUGUI zoneText;

    [Header("Ajustes de Movimiento")]
    public float dropDistance = 500f;
    public float dropDuration = 0.4f;   // Un poco más rápido para fluidez
    public AnimationCurve dropCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Ajustes de Balanceo")]
    public float swingAmount = 12f;
    public float swingSpeed = 5f;
    public float swingDuration = 1.5f;

    [Header("Tiempos")]
    public float displayTime = 1.5f;
    public string playerTag = "Player";

    private Vector2 originalPosition;
    private Coroutine currentSequence;
    private Vector2 hiddenPosition;

    void Start()
    {
        if (panelRect == null) panelRect = GetComponent<RectTransform>();

        originalPosition = panelRect.anchoredPosition;
        hiddenPosition = originalPosition + new Vector2(0, dropDistance);

        // Iniciar escondido
        panelRect.anchoredPosition = hiddenPosition;

        if (zoneText != null) zoneText.text = zoneName;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            // 1. Traer al frente para que no lo tape ningún otro cartel
            panelRect.SetAsLastSibling();

            if (currentSequence != null) StopCoroutine(currentSequence);
            currentSequence = StartCoroutine(ShowSignSequence());
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            // 2. Si el jugador sale de la zona, forzamos el guardado inmediato
            if (currentSequence != null) StopCoroutine(currentSequence);
            currentSequence = StartCoroutine(HideSignQuickly());
        }
    }

    IEnumerator ShowSignSequence()
    {
        // CAÍDA
        float elapsed = 0;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = dropCurve.Evaluate(elapsed / dropDuration);
            panelRect.anchoredPosition = Vector2.Lerp(hiddenPosition, originalPosition, t);
            yield return null;
        }
        panelRect.anchoredPosition = originalPosition;

        // BALANCEO
        float swingElapsed = 0;
        while (swingElapsed < swingDuration)
        {
            swingElapsed += Time.deltaTime;
            float decay = 1 - (swingElapsed / swingDuration);
            float angle = Mathf.Sin(Time.time * swingSpeed) * swingAmount * decay;
            panelRect.localRotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
        panelRect.localRotation = Quaternion.identity;

        yield return new WaitForSeconds(displayTime);

        // RETIRADA NORMAL
        yield return StartCoroutine(HideSignQuickly());
    }

    IEnumerator HideSignQuickly()
    {
        float elapsed = 0;
        Vector2 currentPos = panelRect.anchoredPosition;

        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            panelRect.anchoredPosition = Vector2.Lerp(currentPos, hiddenPosition, elapsed / dropDuration);
            // Enderezar si se estaba balanceando
            panelRect.localRotation = Quaternion.Lerp(panelRect.localRotation, Quaternion.identity, elapsed / dropDuration);
            yield return null;
        }
        panelRect.anchoredPosition = hiddenPosition;
        currentSequence = null;
    }
}