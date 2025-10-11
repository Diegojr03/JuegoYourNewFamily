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
    public Transform controlsContainer; // Padre de todas las filas de controles
    public GameObject controlRowPrefab; // Prefab para cada fila

    private ControlMapping currentRebinding;
    private GameObject currentRebindingButton;

    void Start()
    {
        LoadControlMappings();
        CreateControlUI();
    }

    void Update()
    {
        // Detectar entrada durante el reasignado
        if (currentRebinding != null && Input.anyKeyDown)
        {
            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    // No permitir teclas especiales
                    if (keyCode == KeyCode.Escape || keyCode == KeyCode.Return || keyCode == KeyCode.KeypadEnter)
                    {
                        CancelRebinding();
                        return;
                    }

                    AssignNewKey(keyCode);
                    return;
                }
            }
        }
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
        {
            Destroy(child.gameObject);
        }

        // Crear una fila por cada control
        foreach (ControlMapping mapping in controlMappings)
        {
            GameObject newRow = Instantiate(controlRowPrefab, controlsContainer);
            SetupControlRow(newRow, mapping);
        }
    }

    void SetupControlRow(GameObject row, ControlMapping mapping)
    {
        // Referencias a los textos y botón
        TextMeshProUGUI actionText = row.transform.Find("ActionText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI keyText = row.transform.Find("KeyText")?.GetComponent<TextMeshProUGUI>();
        Button rebindButton = row.transform.Find("RebindButton")?.GetComponent<Button>();

        if (actionText != null)
            actionText.text = mapping.actionName;

        if (keyText != null)
            keyText.text = mapping.currentKey.ToString();

        if (rebindButton != null)
        {
            rebindButton.onClick.RemoveAllListeners();
            rebindButton.onClick.AddListener(() => StartRebinding(mapping, rebindButton.gameObject, keyText));
        }
    }

    public void StartRebinding(ControlMapping mapping, GameObject button, TextMeshProUGUI keyText)
    {
        currentRebinding = mapping;
        currentRebindingButton = button;

        // Feedback visual
        if (keyText != null)
            keyText.text = "Pulsa una tecla...";

        button.GetComponent<Button>().interactable = false;

        Debug.Log("Reasignando: " + mapping.actionName);
    }

    void AssignNewKey(KeyCode newKey)
    {
        if (currentRebinding != null)
        {
            // Verificar si la tecla ya está en uso
            if (IsKeyAlreadyUsed(newKey))
            {
                Debug.LogWarning("La tecla " + newKey + " ya está en uso");
                CancelRebinding();
                return;
            }

            currentRebinding.currentKey = newKey;
            SaveControlMappings();
            RefreshUI();

            Debug.Log(currentRebinding.actionName + " asignado a: " + newKey);
        }

        currentRebinding = null;
        currentRebindingButton = null;
    }

    void CancelRebinding()
    {
        if (currentRebinding != null && currentRebindingButton != null)
        {
            RefreshUI();
            currentRebindingButton.GetComponent<Button>().interactable = true;
        }

        currentRebinding = null;
        currentRebindingButton = null;
    }

    bool IsKeyAlreadyUsed(KeyCode key)
    {
        foreach (ControlMapping mapping in controlMappings)
        {
            if (mapping.currentKey == key && mapping != currentRebinding)
                return true;
        }
        return false;
    }

    void RefreshUI()
    {
        CreateControlUI();
    }

    void LoadControlMappings()
    {
        // Cargar controles guardados o usar defaults
        for (int i = 0; i < controlMappings.Count; i++)
        {
            string savedKey = PlayerPrefs.GetString("Control_" + controlMappings[i].actionName, controlMappings[i].defaultKey.ToString());
            if (Enum.TryParse(savedKey, out KeyCode loadedKey))
            {
                controlMappings[i].currentKey = loadedKey;
            }
        }
    }

    void SaveControlMappings()
    {
        foreach (ControlMapping mapping in controlMappings)
        {
            PlayerPrefs.SetString("Control_" + mapping.actionName, mapping.currentKey.ToString());
        }
        PlayerPrefs.Save();
    }

    // Método para obtener la tecla de una acción (para usar en otros scripts)
    public KeyCode GetKeyForAction(string actionName)
    {
        ControlMapping mapping = controlMappings.Find(m => m.actionName == actionName);
        return mapping != null ? mapping.currentKey : KeyCode.None;
    }

    // Resetear a controles por defecto
    public void ResetToDefaultControls()
    {
        foreach (ControlMapping mapping in controlMappings)
        {
            mapping.currentKey = mapping.defaultKey;
        }
        SaveControlMappings();
        RefreshUI();

        Debug.Log("Controles restaurados a valores por defecto");
    }
}
