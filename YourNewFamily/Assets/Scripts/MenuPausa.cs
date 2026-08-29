using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPausa : MonoBehaviour
{
    // =========================================================
    // REFERENCIAS DE UI (se asignan en cada escena)
    // =========================================================
    [Header("Paneles principales")]
    public GameObject menuPausa;               // Panel del menú de pausa
    public GameObject panelOpciones;           // Panel de opciones
    public GameObject panelConfirmacionSalir;  // Panel de confirmación para salir

    [Header("Botones del menú de pausa")]
    public Button botonReanudar;
    public Button botonOpciones;
    public Button botonSalir;
    public Button BotonVolver;

    [Header("Botones de confirmación")]
    public Button botonSi;
    public Button botonNo;

    [Header("Controles")]
    public Slider sliderVolumen;               // Slider de volumen

    [Header("Configuración")]
    public string nombreEscenaMenu = "MainMenu";

    // =========================================================
    // ESTADO INTERNO
    // =========================================================
    private bool juegoPausado = false;
    private MusicManager musicManager;

    // =========================================================
    // SINGLETON
    // =========================================================
    private static MenuPausa instance;

    // =========================================================
    // MÉTODOS DE UNITY
    // =========================================================

    private void Awake()
    {
        // Si no hay instancia, ésta es la principal y persiste
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[MenuPausa] Instancia persistente creada.");
        }
        // Si ya existe, actualizamos sus referencias con las de esta nueva escena
        else if (instance != this)
        {
            Debug.Log("[MenuPausa] Nueva escena detectada. Actualizando referencias de UI...");
            instance.ActualizarReferenciasUI(this);
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Solo la instancia persistente ejecuta la inicialización
        if (instance != this) return;

        // Configurar la UI (slider, listeners, etc.)
        ConfigurarUI();

        // Estado inicial: juego en marcha, menús ocultos
        Time.timeScale = 1f;
        juegoPausado = false;
        OcultarTodosLosPaneles();
    }

    private void Update()
    {
        // Solo la instancia persistente procesa entrada
        if (instance != this) return;

        // Tecla ESC para pausa / reanudar / cerrar subpaneles
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
    // CONFIGURACIÓN DE UI (slider + botones)
    // =========================================================

    private void ConfigurarUI()
    {
        // 1. Slider
        ConfigurarSlider();

        // 2. Botones del menú de pausa
        ConfigurarBoton(botonReanudar, ReanudarJuego);
        ConfigurarBoton(botonOpciones, BotonOpciones);
        ConfigurarBoton(botonSalir, MostrarConfirmacionSalir);

        // 3. Botones de confirmación
        ConfigurarBoton(botonSi, ConfirmarSalir);
        ConfigurarBoton(botonNo, CancelarSalir);

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

        // Determinar el valor inicial del slider
        float valorInicial;
        if (PlayerPrefs.HasKey("VolumenMusica"))
        {
            // Si ya hay un volumen guardado, lo usamos (limitado al rango)
            float saved = PlayerPrefs.GetFloat("VolumenMusica", 0.5f);
            valorInicial = Mathf.Min(saved, 0.5f);
        }
        else
        {
            // Primera vez: establecemos 0.25 (mitad del rango)
            valorInicial = 0.25f;
            // Guardamos este valor en el MusicManager para que persista
            musicManager.SetVolume(valorInicial);
        }

        // Asignamos el valor al slider y sincronizamos el volumen real
        sliderVolumen.value = valorInicial;
        musicManager.SetVolume(valorInicial);

        // Listener para cuando el usuario mueva el slider
        sliderVolumen.onValueChanged.RemoveAllListeners();
        sliderVolumen.onValueChanged.AddListener((valor) =>
        {
            if (musicManager != null)
            {
                musicManager.SetVolume(valor); // valor entre 0 y 0.5
            }
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

        // Copiar todas las referencias del nuevo Canvas
        menuPausa = nuevoMenu.menuPausa;
        panelOpciones = nuevoMenu.panelOpciones;
        panelConfirmacionSalir = nuevoMenu.panelConfirmacionSalir;

        botonReanudar = nuevoMenu.botonReanudar;
        botonOpciones = nuevoMenu.botonOpciones;
        botonSalir = nuevoMenu.botonSalir;
        botonSi = nuevoMenu.botonSi;
        botonNo = nuevoMenu.botonNo;

        sliderVolumen = nuevoMenu.sliderVolumen;

        // (Opcional) copiar nombreEscenaMenu si es diferente
        // nombreEscenaMenu = nuevoMenu.nombreEscenaMenu;

        // Reconfigurar la UI con las nuevas referencias
        ConfigurarUI();

        // Asegurar que los paneles estén ocultos y el tiempo normal
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
            // Actualizar slider con el volumen actual (limitado a 0.5)
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
        // Ocultar menú de pausa y opciones, mostrar solo confirmación
        if (menuPausa != null) menuPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelConfirmacionSalir != null) panelConfirmacionSalir.SetActive(true);
    }

    public void ConfirmarSalir()
    {
        Debug.Log("[MenuPausa] Salida confirmada. Guardando partida...");
        SaveManager.Instance.SaveGame();   // <-- directo
        Time.timeScale = 1f;
        juegoPausado = false;
        SceneManager.LoadScene(nombreEscenaMenu);
    }

    public void CancelarSalir()
    {
        Debug.Log("[MenuPausa] Salida cancelada.");
        if (panelConfirmacionSalir != null) panelConfirmacionSalir.SetActive(false);
        // Volver a mostrar el menú de pausa (si el juego sigue pausado)
        if (juegoPausado && menuPausa != null)
        {
            menuPausa.SetActive(true);
        }
    }

    // =========================================================
    // GUARDADO DE PARTIDA
    // =========================================================

    private void GuardarPartida()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
            Debug.Log("[MenuPausa] Partida guardada correctamente.");
        }
        else
        {
            Debug.LogError("[MenuPausa] SaveManager no disponible.");
        }
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

    public void cerrarPanelAjustes()
    {
        panelOpciones.SetActive(false);
        menuPausa.SetActive(true);
    }
}