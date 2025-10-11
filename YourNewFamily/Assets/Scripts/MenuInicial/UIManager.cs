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
        SceneManager.LoadScene(1);
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
