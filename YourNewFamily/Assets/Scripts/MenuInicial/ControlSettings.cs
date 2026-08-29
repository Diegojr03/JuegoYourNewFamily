using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ControlMapping
{
    public string actionName;
    public KeyCode currentKey;
    public KeyCode defaultKey;
}

public class ControlSettings : MonoBehaviour
{
    [Header("Mapeo de Controles")]
    public List<ControlMapping> controlMappings = new List<ControlMapping>();

    [Header("Referencias UI")]
    public Transform controlsContainer; // Padre de todas las filas (dentro de un ScrollView)
    public GameObject controlRowPrefab; // Prefab con ActionText y KeyText (sin botón)

    void Start()
    {
        LoadControlMappings();      // Carga las teclas guardadas (si las hay)
        CreateControlUI();          // Genera la lista
    }

    void CreateControlUI()
    {
        if (controlsContainer == null || controlRowPrefab == null)
        {
            Debug.LogError("Faltan referencias UI en ControlManager");
            return;
        }

        // Limpiar contenedor
        foreach (Transform child in controlsContainer)
            Destroy(child.gameObject);

        // Crear una fila por cada control
        foreach (ControlMapping mapping in controlMappings)
        {
            GameObject newRow = Instantiate(controlRowPrefab, controlsContainer);
            SetupControlRow(newRow, mapping);
        }
    }

    void SetupControlRow(GameObject row, ControlMapping mapping)
    {
        // Solo necesitamos los textos
        TextMeshProUGUI actionText = row.transform.Find("ActionText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI keyText = row.transform.Find("KeyText")?.GetComponent<TextMeshProUGUI>();

        if (actionText != null)
            actionText.text = mapping.actionName;

        if (keyText != null)
            keyText.text = mapping.currentKey.ToString();

        // Si el prefab tiene un botón, lo ocultamos (para que no se vea)
        Button rebindButton = row.transform.Find("RebindButton")?.GetComponent<Button>();
        if (rebindButton != null)
            rebindButton.gameObject.SetActive(false);
    }

    void LoadControlMappings()
    {
        // Cargar controles guardados (si existen) para mostrar las teclas actuales
        for (int i = 0; i < controlMappings.Count; i++)
        {
            string savedKey = PlayerPrefs.GetString("Control_" + controlMappings[i].actionName,
                                                    controlMappings[i].defaultKey.ToString());
            if (Enum.TryParse(savedKey, out KeyCode loadedKey))
                controlMappings[i].currentKey = loadedKey;
        }
    }

    // Método público para que otros scripts consulten la tecla asignada a una acción
    public KeyCode GetKeyForAction(string actionName)
    {
        ControlMapping mapping = controlMappings.Find(m => m.actionName == actionName);
        return mapping != null ? mapping.currentKey : KeyCode.None;
    }
}