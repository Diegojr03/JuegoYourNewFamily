using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraphicSettings : MonoBehaviour
{
    [Header("Referencias UI")]
    public Toggle fullscreenToggle;
    public Toggle vsyncToggle;

    private Resolution[] resolutions;

    void Start()
    {
    }


    void ApplyAllGraphicsSettings()
    {
        SetFullscreen(fullscreenToggle.isOn);
        SetVSync(vsyncToggle.isOn);
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
}
