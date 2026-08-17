using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPausa : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject menuPausa;          // Panel del menú de pausa
    public GameObject panelOpciones;       // Panel de opciones
    public Slider sliderVolumen;           // Slider para controlar el volumen

    [Header("Configuración")]
    public string nombreEscenaMenu = "MainMenu";

    private bool juegoPausado = false;
    private MusicManager musicManager;

    void Start()
    {
        // Asegurar que el juego empiece sin pausa
        Time.timeScale = 1f;
        juegoPausado = false;

        // Ocultar menús al inicio
        if (menuPausa != null) menuPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);

        // Obtener referencia al MusicManager
        musicManager = MusicManager.Instance;

        // Configurar el slider
        ConfigurarSlider();
    }

    void ConfigurarSlider()
    {
        if (sliderVolumen == null)
        {
            Debug.LogError("SLIDER NO ASIGNADO EN EL INSPECTOR");
            return;
        }

        if (musicManager == null)
        {
            Debug.LogError("MUSIC MANAGER NO ENCONTRADO");
            return;
        }

        // Configurar rango del slider (por si acaso)
        sliderVolumen.minValue = 0f;
        sliderVolumen.maxValue = 1f;

        // Establecer el valor actual
        float volumenActual = musicManager.GetVolume();
        sliderVolumen.value = volumenActual;

        Debug.Log($"Slider configurado - Valor inicial: {volumenActual}");

        // IMPORTANTE: Primero removemos todos los listeners para evitar duplicados
        sliderVolumen.onValueChanged.RemoveAllListeners();

        // Luego agregamos el listener
        sliderVolumen.onValueChanged.AddListener((valor) => {
            Debug.Log($"SLIDER CAMBIÓ A: {valor}");
            if (musicManager != null)
            {
                musicManager.SetVolume(valor);
            }
        });
    }

    void Update()
    {
        // Detectar tecla ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (juegoPausado)
            {
                if (panelOpciones != null && panelOpciones.activeSelf)
                {
                    CerrarOpciones();
                }
                else
                {
                    ReanudarJuego();
                }
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
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

    public void ReanudarJuego()
    {
        juegoPausado = false;
        Time.timeScale = 1f;
        if (menuPausa != null) menuPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

    public void BotonReanudar()
    {
        ReanudarJuego();
    }

    public void BotonOpciones()
    {
        Debug.Log("Abriendo panel de opciones");

        if (menuPausa != null) menuPausa.SetActive(false);
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(true);

            // Actualizar el slider cuando se abre el panel
            if (sliderVolumen != null && musicManager != null)
            {
                float volumenActual = musicManager.GetVolume();
                sliderVolumen.value = volumenActual;
                Debug.Log($"Panel abierto - Slider actualizado a: {volumenActual}");
            }
        }
    }

    public void CerrarOpciones()
    {
        Debug.Log("Cerrando panel de opciones");
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (menuPausa != null) menuPausa.SetActive(true);
    }

    public void BotonSalir()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}