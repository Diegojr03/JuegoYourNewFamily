using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraphicSettings : MonoBehaviour
{
    [Header("Referencias UI")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToggle;
    public Toggle vsyncToggle;

    private Resolution[] resolutions;

    void Start()
    {
        // Inicializar todas las configuraciones
        SetupResolutionDropdown();
        SetupQualityDropdown();
        LoadSavedSettings();

        Debug.Log("GraphicsManager inicializado correctamente");
    }

    void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            Debug.LogError("ResolutionDropdown no asignado en el inspector");
            return;
        }

        // Obtener todas las resoluciones disponibles
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        // Filtrar resoluciones únicas (evitar duplicados)
        var uniqueResolutions = resolutions
            .Select(res => new { res.width, res.height })
            .Distinct()
            .ToArray();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < uniqueResolutions.Length; i++)
        {
            string option = uniqueResolutions[i].width + " x " + uniqueResolutions[i].height;
            options.Add(option);

            // Encontrar la resolución actual
            if (uniqueResolutions[i].width == Screen.currentResolution.width &&
                uniqueResolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        Debug.Log("Dropdown de resoluciones configurado con " + options.Count + " opciones");
    }

    void SetupQualityDropdown()
    {
        if (qualityDropdown == null)
        {
            Debug.LogError("QualityDropdown no asignado en el inspector");
            return;
        }

        qualityDropdown.ClearOptions();

        // Obtener nombres de los niveles de calidad
        string[] qualityNames = QualitySettings.names;
        List<string> options = new List<string>();

        foreach (string name in qualityNames)
        {
            options.Add(name);
        }

        qualityDropdown.AddOptions(options);
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();

        Debug.Log("Dropdown de calidad configurado con " + options.Count + " niveles");
    }

    void LoadSavedSettings()
    {
        // Cargar configuración guardada o usar valores por defecto
        fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        vsyncToggle.isOn = PlayerPrefs.GetInt("VSync", QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;

        int savedResolution = PlayerPrefs.GetInt("Resolution", -1);
        if (savedResolution != -1 && savedResolution < resolutionDropdown.options.Count)
        {
            resolutionDropdown.value = savedResolution;
            resolutionDropdown.RefreshShownValue();
        }

        int savedQuality = PlayerPrefs.GetInt("Quality", QualitySettings.GetQualityLevel());
        if (savedQuality < qualityDropdown.options.Count)
        {
            qualityDropdown.value = savedQuality;
            qualityDropdown.RefreshShownValue();
        }

        // Aplicar configuración cargada
        ApplyAllGraphicsSettings();

        Debug.Log("Configuración gráfica cargada");
    }

    void ApplyAllGraphicsSettings()
    {
        SetResolution(resolutionDropdown.value);
        SetFullscreen(fullscreenToggle.isOn);
        SetQuality(qualityDropdown.value);
        SetVSync(vsyncToggle.isOn);
    }

    public void SetResolution(int index)
    {
        if (resolutions == null || index < 0 || index >= resolutionDropdown.options.Count)
            return;

        // Encontrar la resolución correspondiente
        string[] resolutionParts = resolutionDropdown.options[index].text.Split('x');
        if (resolutionParts.Length == 2)
        {
            int width = int.Parse(resolutionParts[0].Trim());
            int height = int.Parse(resolutionParts[1].Trim());

            Screen.SetResolution(width, height, Screen.fullScreen);
            PlayerPrefs.SetInt("Resolution", index);

            Debug.Log("Resolución cambiada a: " + width + " x " + height);
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);

        Debug.Log("Pantalla completa: " + isFullscreen);
    }

    public void SetQuality(int qualityIndex)
    {
        if (qualityIndex >= 0 && qualityIndex < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
            PlayerPrefs.SetInt("Quality", qualityIndex);

            Debug.Log("Calidad gráfica cambiada a: " + QualitySettings.names[qualityIndex]);
        }
    }

    public void SetVSync(bool vSync)
    {
        QualitySettings.vSyncCount = vSync ? 1 : 0;
        PlayerPrefs.SetInt("VSync", vSync ? 1 : 0);

        Debug.Log("VSync: " + (vSync ? "Activado" : "Desactivado"));
    }

    // Método para resetear a configuración por defecto
    public void ResetToDefault()
    {
        // Valores por defecto
        resolutionDropdown.value = resolutionDropdown.options.Count - 1; // Resolución más alta
        qualityDropdown.value = QualitySettings.names.Length - 1; // Calidad más alta
        fullscreenToggle.isOn = true;
        vsyncToggle.isOn = true;

        // Aplicar cambios
        ApplyAllGraphicsSettings();

        Debug.Log("Configuración gráfica restaurada a valores por defecto");
    }
}
