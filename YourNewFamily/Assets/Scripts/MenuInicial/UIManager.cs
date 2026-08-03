using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject panelMainMenu;
    public GameObject panelAjustes;


    void Start()
    {
        ShowMainMenu();
    }

    public void Jugar()
    {
        // Intentar cargar partida guardada
        bool hasSave = SaveManager.Instance.LoadGame();

        if (!hasSave)
        {
            // No hay guardado, empezar desde el principio
            Debug.Log("No hay partida guardada. Iniciando nuevo juego.");
            SaveManager.Instance.DeleteSave(); // Limpiar por si acaso
            SceneManager.LoadScene("SampleScene");
        }
        // Si LoadGame() fue exitoso, ya se está cargando la escena guardada,
        // así que no hacemos nada más.
    }
    public void ShowMainMenu()
    {
        panelMainMenu.SetActive(true);
        panelAjustes.SetActive(false);
    }

    public void ShowSettings()
    {
        panelMainMenu.SetActive(false);
        panelAjustes.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();

        // Para testing en el editor
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
