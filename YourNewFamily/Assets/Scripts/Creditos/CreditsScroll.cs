using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsScroll : MonoBehaviour
{
    public float scrollSpeed = 50f; // Velocidad del scroll
    public float exitPosition = 2000f; // Altura a la que termina (ajustar según largo del texto)
    public string nextSceneName = "MainMenu"; // Escena a la que vuelve al terminar

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Mueve el texto hacia arriba
        rectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

        // Si el texto sale completamente de la pantalla o pulsas espacio, termina
        if (rectTransform.anchoredPosition.y > exitPosition || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            FinishCredits();
        }
    }

    void FinishCredits()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}