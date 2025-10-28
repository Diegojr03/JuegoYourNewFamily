using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections; // Añadir este using

public class SceneTransitionWithFade : MonoBehaviour
{
    public string sceneName;
    public AudioClip transitionSound;
    public Image fadeNegro; // Arrastra aquí tu objeto del Canvas

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            InitiateTransition();
        }
    }

    private void InitiateTransition()
    {
        // Reproducir sonido
        if (transitionSound != null)
        {
            AudioSource.PlayClipAtPoint(transitionSound, transform.position);
        }

        // Activar y hacer fade in del objeto negro
        if (fadeNegro != null)
        {
            fadeNegro.gameObject.SetActive(true);
            StartCoroutine(FadeIn());
        }
        else
        {
            // Si no hay fadeNegro, cambiar escena directamente
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator FadeIn() // Cambiado de System.Collections.IEnumerator a IEnumerator
    {
        float duration = 1f; // Duración del fade
        float elapsedTime = 0f;

        fadeNegro.color = new Color(0, 0, 0, 0);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            fadeNegro.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeNegro.color = new Color(0, 0, 0, 1f);

        // Cambiar escena cuando termine el fade
        SceneManager.LoadScene(sceneName);
    }
}