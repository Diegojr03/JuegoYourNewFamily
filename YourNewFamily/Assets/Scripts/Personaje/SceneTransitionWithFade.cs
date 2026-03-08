using UnityEngine;

public class SceneTransitionWithFade : MonoBehaviour
{
    public string sceneName;
    public AudioClip transitionSound;

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

        // Buscar el FadeManager (como es persistente, lo encontraremos)
        FadeManager fadeManager = FindObjectOfType<FadeManager>();

        if (fadeManager != null)
        {
            fadeManager.ChangeScene(sceneName);
        }
        else
        {
            Debug.LogError("No se encontró FadeManager. Asegúrate de que existe en la escena inicial");
        }
    }
}