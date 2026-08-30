using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPausa : MonoBehaviour
{
    // =========================================================
    // REFERENCIAS DE UI (se asignan en cada escena)
    // =========================================================
    [Header("Paneles principales")]
    public GameObject menuPausa;
    public GameObject panelOpciones;
    public GameObject panelConfirmacionSalir;

    [Header("Botones del menú de pausa")]
    public Button botonReanudar;
    public Button botonOpciones;
    public Button botonSalir;
    public Button BotonVolver;                 // Botón "Volver" en el panel de opciones

    [Header("Botones de confirmación")]
    public Button botonSi;
    public Button botonNo;

    [Header("Controles")]
    public Slider sliderVolumen;

    [Header("Configuración")]
    public string nombreEscenaMenu = "MainMenu";

    // =========================================================
    // ESTADO INTERNO
    // =========================================================
    private bool juegoPausado = false;
    private MusicManager musicManager;

    private static MenuPausa instance;

    // =========================================================
    // MÉTODOS DE UNITY
    // =========================================================

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[MenuPausa] Instancia persistente creada.");
        }
        else if (instance != this)
        {
            Debug.Log("[MenuPausa] Nueva escena detectada. Actualizando referencias de UI...");
            instance.ActualizarReferenciasUI(this);
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (instance != this) return;

        ConfigurarUI();

        Time.timeScale = 1f;
        juegoPausado = false;
        OcultarTodosLosPaneles();
    }

    private void Update()
    {
        if (instance != this) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (panelConfirmacionSalir != null && panelConfirmacionSalir.activeSelf)
            {
                CancelarSalir();
                return;
            }

            if (juegoPausado)
            {
                if (panelOpciones != null && panelOpciones.activeSelf)
                    CerrarOpciones();
                else
                    ReanudarJuego();
            }
            else
            {
                PausarJuego();
            }
        }
    }

    // =========================================================
    // CONFIGURACIÓN DE UI
    // =========================================================

    private void ConfigurarUI()
    {
        ConfigurarSlider();

        ConfigurarBoton(botonReanudar, ReanudarJuego);
        ConfigurarBoton(botonOpciones, BotonOpciones);
        ConfigurarBoton(botonSalir, MostrarConfirmacionSalir);
        ConfigurarBoton(botonSi, ConfirmarSalir);
        ConfigurarBoton(botonNo, CancelarSalir);
        ConfigurarBoton(BotonVolver, cerrarPanelAjustes);  // ← Botón Volver por código

        Debug.Log("[MenuPausa] UI configurada correctamente.");
    }

    private void ConfigurarSlider()
    {
        if (sliderVolumen == null)
        {
            Debug.LogError("[MenuPausa] Slider no asignado en el inspector.");
            return;
        }

        musicManager = MusicManager.Instance;
        if (musicManager == null)
        {
            Debug.LogError("[MenuPausa] MusicManager no encontrado.");
            return;
        }

        // Rango de 0 a 0.5 (50% del volumen máximo)
        sliderVolumen.minValue = 0f;
        sliderVolumen.maxValue = 0.5f;

        // Siempre inicio en 0.25 (mitad del slider) → volumen real = 0.25
        float valorInicial = 0.25f;

        sliderVolumen.value = valorInicial;
        musicManager.SetVolume(valorInicial);

        sliderVolumen.onValueChanged.RemoveAllListeners();
        sliderVolumen.onValueChanged.AddListener((valor) =>
        {
            if (musicManager != null)
                musicManager.SetVolume(valor);
        });

        Debug.Log($"[MenuPausa] Slider configurado - Rango: 0 a 0.5, Valor inicial: {sliderVolumen.value}");
    }

    private void ConfigurarBoton(Button boton, UnityEngine.Events.UnityAction accion)
    {
        if (boton == null)
        {
            Debug.LogWarning("[MenuPausa] Un botón no está asignado, se omite.");
            return;
        }

        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(accion);
        boton.interactable = true;
    }

    // =========================================================
    // ACTUALIZACIÓN DE REFERENCIAS (al cambiar de escena)
    // =========================================================

    public void ActualizarReferenciasUI(MenuPausa nuevoMenu)
    {
        if (nuevoMenu == null) return;

        menuPausa = nuevoMenu.menuPausa;
        panelOpciones = nuevoMenu.panelOpciones;
        panelConfirmacionSalir = nuevoMenu.panelConfirmacionSalir;

        botonReanudar = nuevoMenu.botonReanudar;
        botonOpciones = nuevoMenu.botonOpciones;
        botonSalir = nuevoMenu.botonSalir;
        BotonVolver = nuevoMenu.BotonVolver;
        botonSi = nuevoMenu.botonSi;
        botonNo = nuevoMenu.botonNo;

        sliderVolumen = nuevoMenu.sliderVolumen;

        ConfigurarUI();

        OcultarTodosLosPaneles();
        Time.timeScale = 1f;
        juegoPausado = false;

        Debug.Log("[MenuPausa] Referencias de UI actualizadas correctamente.");
    }

    // =========================================================
    // MÉTODOS DE PAUSA / REANUDAR
    // =========================================================

    public void PausarJuego()
    {
        juegoPausado = true;
        Time.timeScale = 0f;
        if (menuPausa != null) menuPausa.SetActive(true);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelConfirmacionSalir != null) panelConfirmacionSalir.SetActive(false);
    }

    public void ReanudarJuego()
    {
        juegoPausado = false;
        Time.timeScale = 1f;
        OcultarTodosLosPaneles();
    }

    // =========================================================
    // BOTONES DEL MENÚ DE PAUSA
    // =========================================================

    public void BotonReanudar()
    {
        ReanudarJuego();
    }

    public void BotonOpciones()
    {
        Debug.Log("[MenuPausa] Abriendo panel de opciones");
        if (menuPausa != null) menuPausa.SetActive(false);
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(true);
            // Mostrar el valor actual (que siempre será 0.25 o el que haya puesto el usuario)
            if (sliderVolumen != null && musicManager != null)
            {
                float vol = musicManager.GetVolume();
                sliderVolumen.value = Mathf.Min(vol, 0.5f);
            }
        }
        if (panelConfirmacionSalir != null) panelConfirmacionSalir.SetActive(false);
    }

    public void CerrarOpciones()
    {
        Debug.Log("[MenuPausa] Cerrando panel de opciones");
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (menuPausa != null) menuPausa.SetActive(true);
    }

    // =========================================================
    // CONFIRMACIÓN DE SALIDA
    // =========================================================

    public void MostrarConfirmacionSalir()
    {
        Debug.Log("[MenuPausa] Mostrando confirmación para salir");
        if (menuPausa != null) menuPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelConfirmacionSalir != null) panelConfirmacionSalir.SetActive(true);
    }

    public void ConfirmarSalir()
    {
        Debug.Log("[MenuPausa] Salida confirmada. Guardando partida...");
        SaveManager.Instance.SaveGame();
        Time.timeScale = 1f;
        juegoPausado = false;
        SceneManager.LoadScene(nombreEscenaMenu);
    }

    public void CancelarSalir()
    {
        Debug.Log("[MenuPausa] Salida cancelada.");
        if (panelConfirmacionSalir != null) panelConfirmacionSalir.SetActive(false);
        if (juegoPausado && menuPausa != null)
            menuPausa.SetActive(true);
    }

    // =========================================================
    // MÉTODOS AUXILIARES
    // =========================================================

    private void OcultarTodosLosPaneles()
    {
        if (menuPausa != null) menuPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelConfirmacionSalir != null) panelConfirmacionSalir.SetActive(false);
    }

    // Este método es llamado por el botón "Volver"
    public void cerrarPanelAjustes()
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (menuPausa != null) menuPausa.SetActive(true);
    }
}