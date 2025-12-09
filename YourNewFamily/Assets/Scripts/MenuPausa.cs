using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject menuPausa;          // Panel del menú de pausa

    [Header("Configuración")]
    public string nombreEscenaMenu = "MainMenu";

    private bool juegoPausado = false;

    void Start()
    {
        // Asegurar que el juego empiece sin pausa
        Time.timeScale = 1f;
        juegoPausado = false;

        // Ocultar menú al inicio
        if (menuPausa != null) menuPausa.SetActive(false);
    }

    void Update()
    {
        // Detectar tecla ESC únicamente para pausar/reanudar
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (juegoPausado)
            {
                ReanudarJuego();
            }
            else
            {
                PausarJuego();
            }
        }
    }

    public void PausarJuego()
    {
        juegoPausado = true;
        Time.timeScale = 0f;
        if (menuPausa != null) menuPausa.SetActive(true);
    }

    public void ReanudarJuego()
    {
        juegoPausado = false;
        Time.timeScale = 1f;
        if (menuPausa != null) menuPausa.SetActive(false);
    }

    // Método para el botón "Reanudar" en el UI
    public void BotonReanudar()
    {
        ReanudarJuego();
    }

    // Método para el botón "Salir" en el UI
    public void BotonSalir()
    {
        // Reanudar el tiempo antes de cambiar de escena
        Time.timeScale = 1f;

        // Cargar escena con índice 0
        SceneManager.LoadScene(0);
    }
}
