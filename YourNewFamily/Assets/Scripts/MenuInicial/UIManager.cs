using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject panelMainMenu;
    public GameObject panelAjustes;

    public GameObject continueButton;          // Referencia al botón Continuar
    public GameObject confirmNewGamePanel;     // Panel de confirmación (Sí / No)


    void Start()
    {

        // Asegurar que el panel de confirmación esté oculto al inicio
        if (confirmNewGamePanel != null)
            confirmNewGamePanel.SetActive(false);

        ShowMainMenu();
    }

    private void Update()
    {
        // Comprobar si existe guardado
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSaveFile();
        if (continueButton != null)
            continueButton.SetActive(hasSave);
    }

    // Botón "Continuar"
    public void ContinueGame()
    {
        if (SaveManager.Instance != null)
        {
            bool loaded = SaveManager.Instance.LoadGame();
            if (!loaded)
            {
                Debug.LogWarning("No se pudo cargar la partida. Iniciando nueva.");
                SaveManager.Instance.DeleteSave();
                SceneManager.LoadScene("SampleScene");
            }
            // Si LoadGame() es exitoso, la escena se cargará sola.
        }
    }

    // Botón "Nueva partida"
    public void StartNewGame()
    {
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSaveFile();
        Debug.Log($"StartNewGame - hasSave: {hasSave}");

        if (hasSave)
        {
            if (confirmNewGamePanel != null)
            {
                confirmNewGamePanel.SetActive(true);
                Debug.Log("Panel de confirmación ACTIVADO");
            }
            else
            {
                Debug.LogError("❌ confirmNewGamePanel es NULL");
            }
        }
        else
        {
            Debug.Log("No hay guardado, iniciando partida nueva directamente.");
            StartFreshGame();
        }
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

    public void ConfirmNewGame()
    {
        // Borrar guardado
        if (SaveManager.Instance != null)
            SaveManager.Instance.DeleteSave();

        // Ocultar panel
        if (confirmNewGamePanel != null)
            confirmNewGamePanel.SetActive(false);

        // Iniciar nueva partida
        StartFreshGame();
    }

    // Botón "No" del panel
    public void CancelNewGame()
    {
        if (confirmNewGamePanel != null)
            confirmNewGamePanel.SetActive(false);
    }

    // Función auxiliar para iniciar el juego desde cero
    private void StartFreshGame()
    {
        Debug.Log("Iniciando nueva partida desde cero.");
        // 👇 Limpiar el historial de diálogos antes de cargar la escena
        if (BacklogManager.Instance != null)
            BacklogManager.Instance.ClearBacklog();
        SceneManager.LoadScene("SampleScene");
    }
}
