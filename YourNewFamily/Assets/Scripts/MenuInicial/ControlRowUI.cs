using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YNF.Localizacion;

public class ControlRowUI : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI actionText;
    public TextMeshProUGUI keyText;
    public Button rebindButton;

    void Awake()
    {
        // Obtener referencias automáticamente si están vacías
        if (actionText == null)
            actionText = transform.Find("ActionText")?.GetComponent<TextMeshProUGUI>();
        if (keyText == null)
            keyText = transform.Find("KeyText")?.GetComponent<TextMeshProUGUI>();
        if (rebindButton == null)
            rebindButton = transform.Find("RebindButton")?.GetComponent<Button>();
    }

    public void Setup(string action, string key)
    {
        if (actionText != null)
            actionText.text = Loc.T(action);
        if (keyText != null)
            keyText.text = key;   // loc-ok: es el nombre de una tecla, no se traduce
    }
}
