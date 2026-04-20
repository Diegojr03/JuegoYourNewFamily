using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AutoSaveSystem : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private KeyCode saveKey = KeyCode.F5;
    [SerializeField] private KeyCode loadKey = KeyCode.F9;

    private const string SAVE_PREFIX = "GameState_";

    void Update()
    {
        if (Input.GetKeyDown(saveKey))
            SaveGame();

        if (Input.GetKeyDown(loadKey))
            LoadGame();
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();

        SaveAllGameObjects(data);
        SaveSceneState(data);

        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(SAVE_PREFIX + "CurrentSave", json);
        PlayerPrefs.Save();

        Debug.Log("¡Juego guardado exitosamente!");
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey(SAVE_PREFIX + "CurrentSave"))
        {
            Debug.LogWarning("No hay partida guardada");
            return;
        }

        string json = PlayerPrefs.GetString(SAVE_PREFIX + "CurrentSave");
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        LoadAllGameObjects(data);
        LoadSceneState(data);

        Debug.Log("¡Partida cargada exitosamente!");
    }

    private void SaveAllGameObjects(SaveData data)
    {
        // MÉTODO CORREGIDO - Usar Object.FindObjectsByType en Unity 2023+
#if UNITY_2023_1_OR_NEWER
        // Para Unity 2023.1 y superior
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
        // Para versiones anteriores de Unity
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
#endif

        foreach (GameObject obj in allObjects)
        {
            // Filtrar objetos que no queremos guardar
            if (!ShouldSaveObject(obj))
                continue;

            GameObjectState state = new GameObjectState();
            state.path = GetGameObjectPath(obj);
            state.isActive = obj.activeSelf;
            state.name = obj.name;
            state.tag = obj.tag;
            state.layer = obj.layer;

            // Guardar posición, rotación y escala
            state.position = obj.transform.position;
            state.rotation = obj.transform.rotation.eulerAngles;
            state.scale = obj.transform.localScale;

            // Guardar componentes importantes
            SaveComponentStates(obj, state);

            data.gameObjects.Add(state);
        }
    }

    private bool ShouldSaveObject(GameObject obj)
    {
        // No guardar el sistema de guardado
        if (obj == this.gameObject || obj.transform.IsChildOf(this.transform))
            return false;

        // No guardar objetos de Unity internos
        if (obj.hideFlags != HideFlags.None)
            return false;

        // No guardar objetos con "DontDestroyOnLoad"
        if (obj.scene.name == null)
            return false;

        // No guardar objetos del sistema de UI si no quieres
        if (obj.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
            return false;

        // No guardar la cámara principal (opcional)
        if (obj.GetComponent<Camera>() != null && obj.CompareTag("MainCamera"))
            return false;

        return true;
    }

    private void SaveComponentStates(GameObject obj, GameObjectState state)
    {
        // Guardar estado de Renderer
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            state.hasRenderer = true;
            state.isRendererEnabled = renderer.enabled;
            state.materialColor = renderer.material != null ? renderer.material.color : Color.white;
        }

        // Guardar estado de Collider
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            state.hasCollider = true;
            state.isColliderEnabled = collider.enabled;
        }

        // Guardar Rigidbody
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            state.hasRigidbody = true;
            state.isKinematic = rb.isKinematic;
            state.useGravity = rb.useGravity;
        }

        // Guardar MonoBehaviour personalizados
        MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && script != this && script.enabled)
            {
                // Guardar solo scripts importantes (puedes filtrar por nombre)
                string scriptName = script.GetType().Name;
                if (!scriptName.StartsWith("AutoSave") && scriptName != "UISaveButtons")
                {
                    ComponentState compState = new ComponentState();
                    compState.typeName = script.GetType().AssemblyQualifiedName;
                    compState.enabled = script.enabled;
                    state.components.Add(compState);
                }
            }
        }
    }

    private void LoadAllGameObjects(SaveData data)
    {
        // Primero restaurar estado de objetos existentes
        foreach (GameObjectState state in data.gameObjects)
        {
            GameObject obj = FindGameObjectByPath(state.path);
            if (obj != null)
            {
                // Restaurar transform
                obj.transform.position = state.position;
                obj.transform.rotation = Quaternion.Euler(state.rotation);
                obj.transform.localScale = state.scale;

                // Restaurar estado activo
                obj.SetActive(state.isActive);
                obj.tag = state.tag;
                obj.layer = state.layer;

                // Restaurar componentes
                LoadComponentStates(obj, state);
            }
            else
            {
                Debug.LogWarning($"No se encontró el objeto: {state.path}");
            }
        }

        // Desactivar objetos que no están en el guardado
        CleanupExtraObjects(data);
    }

    private void LoadComponentStates(GameObject obj, GameObjectState state)
    {
        // Restaurar Renderer
        if (state.hasRenderer)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = state.isRendererEnabled;
                if (renderer.material != null)
                    renderer.material.color = state.materialColor;
            }
        }

        // Restaurar Collider
        if (state.hasCollider)
        {
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = state.isColliderEnabled;
        }

        // Restaurar Rigidbody
        if (state.hasRigidbody)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = state.isKinematic;
                rb.useGravity = state.useGravity;
            }
        }

        // Restaurar MonoBehaviour
        foreach (ComponentState compState in state.components)
        {
            System.Type type = System.Type.GetType(compState.typeName);
            if (type != null)
            {
                Component comp = obj.GetComponent(type);
                if (comp is MonoBehaviour script)
                {
                    script.enabled = compState.enabled;
                }
            }
        }
    }

    private void CleanupExtraObjects(SaveData data)
    {
        // Obtener todos los objetos actuales
#if UNITY_2023_1_OR_NEWER
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
#endif

        foreach (GameObject obj in allObjects)
        {
            if (!ShouldSaveObject(obj))
                continue;

            string path = GetGameObjectPath(obj);
            bool shouldExist = data.gameObjects.Any(go => go.path == path);

            if (!shouldExist)
            {
                // Los objetos que no están en el guardado se desactivan
                if (obj.activeSelf)
                {
                    obj.SetActive(false);
                    Debug.Log($"Objeto desactivado por no estar en guardado: {obj.name}");
                }
            }
        }
    }

    private void SaveSceneState(SaveData data)
    {
        data.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;


    }

    private void LoadSceneState(SaveData data)
    {
        Debug.Log($"Cargando partida de: {data.saveTime}");
        Debug.Log($"Escena: {data.sceneName}");


    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        Transform current = obj.transform;

        while (current.parent != null)
        {
            current = current.parent;
            path = "/" + current.name + path;
        }

        return path;
    }

    private GameObject FindGameObjectByPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        string[] names = path.Split('/');
        if (names.Length < 2)
            return null;

        // El primer elemento es vacío por el slash inicial
        GameObject current = GameObject.Find(names[1]);
        if (current == null)
            return null;

        for (int i = 2; i < names.Length; i++)
        {
            Transform child = current.transform.Find(names[i]);
            if (child == null)
                return null;
            current = child.gameObject;
        }

        return current;
    }

    // Método para borrar la partida guardada
    public void DeleteSave()
    {
        if (PlayerPrefs.HasKey(SAVE_PREFIX + "CurrentSave"))
        {
            PlayerPrefs.DeleteKey(SAVE_PREFIX + "CurrentSave");
            Debug.Log("Partida eliminada");
        }
    }
}

// Clases de datos
[System.Serializable]
public class SaveData
{
    public List<GameObjectState> gameObjects = new List<GameObjectState>();
    public string saveTime;
    public string sceneName;
    public float gameTime;
    public Dictionary<string, object> customData = new Dictionary<string, object>();
}

[System.Serializable]
public class GameObjectState
{
    public string path;
    public string name;
    public string tag;
    public int layer;
    public bool isActive;

    // Transform data
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;

    // Component states
    public bool hasRenderer;
    public bool isRendererEnabled;
    public Color materialColor;

    public bool hasCollider;
    public bool isColliderEnabled;

    public bool hasRigidbody;
    public bool isKinematic;
    public bool useGravity;

    public List<ComponentState> components = new List<ComponentState>();
}

[System.Serializable]
public class ComponentState
{
    public string typeName;
    public bool enabled;
}