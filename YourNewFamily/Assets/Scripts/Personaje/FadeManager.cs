using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    private static FadeManager instance;
    public Image fadeImage;
    public float fadeDuration = 1f;

    void Awake()
    {
        // Patrón Singleton para que solo haya uno
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // ¡Esto es clave!

            // Buscar la imagen si no está asignada
            if (fadeImage == null)
                fadeImage = GetComponentInChildren<Image>();

            // Asegurar que la imagen está transparente al inicio
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0, 0, 0, 0);
                fadeImage.gameObject.SetActive(true);
                fadeImage.raycastTarget = false; // Para que no bloquee clics
            }
        }
        else
        {
            Destroy(gameObject); // Destruir duplicados
        }
    }

    // Método para cambiar de escena con fade
    public void ChangeScene(string sceneName)
    {
        StartCoroutine(FadeOutAndIn(sceneName));
    }

    private IEnumerator FadeOutAndIn(string sceneName)
    {
        Debug.Log("Iniciando fade out...");

        // FADE OUT (de transparente a negro)
        float elapsedTime = 0f;
        Color color = fadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 1f);

        Debug.Log("Cambiando escena a: " + sceneName);

        // Cambiar escena
        SceneManager.LoadScene(sceneName);

        // Esperar un frame para que la escena cargue
        yield return null;

        Debug.Log("Iniciando fade in...");

        // FADE IN (de negro a transparente)
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 0f);

        Debug.Log("Fade completado");
    }
}