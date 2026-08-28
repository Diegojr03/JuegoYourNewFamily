using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SaveData
{
    public string sceneName;
    public float playerX;
    public float playerY;
    public string currentMusicName = ""; // 👈 NUEVO: Nombre de la última música sonando
    public List<string> dialoguesCompleted = new List<string>();
    public List<string> puzzlesCompleted = new List<string>();
    public List<string> completedPaths = new List<string>();
    public List<ObjectState> objectStates = new List<ObjectState>();
    public List<string> unlockedZones = new List<string>();
    public List<string> destroyedObjects = new List<string>();
    public float cameraX;
    public float cameraY;
    public float cameraSize;

    public string lastMissionText = "";
    public bool hasSavedMissionText = false;

    public BacklogSaveData backlogData = new BacklogSaveData();
}

[System.Serializable]
public class ObjectState
{
    public string objectId;
    public bool isActive;
}

[System.Serializable]
public class BacklogSaveData
{
    public List<DialogueEntryData> entries = new List<DialogueEntryData>();
    public string selectedCharacter = "Todos";
}

[System.Serializable]
public class DialogueEntryData
{
    public string speakerName;
    public string dialogueText;
    public string timestamp;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string savePath;
    private SaveData currentSave = new SaveData();
    private bool isLoadingFromSave = false;

    public bool IsLoading => isLoadingFromSave;

    private Dictionary<string, bool> objectStates = new Dictionary<string, bool>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        Debug.Log("📁 Ruta de guardado: " + savePath);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        Debug.Log("✅ SaveManager iniciado correctamente.");
    }

    void Update()
    {
        // Guardar con G o Alt (cualquier tecla Alt)
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            Debug.Log("🔹 Guardando partida (tecla)");
            SaveGame();
        }
    }

    // ---------- GUARDAR ----------
    public void SaveGame()
    {
        currentSave.sceneName = SceneManager.GetActiveScene().name;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 pos = player.transform.position;
            currentSave.playerX = pos.x;
            currentSave.playerY = pos.y;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            currentSave.cameraX = mainCamera.transform.position.x;
            currentSave.cameraY = mainCamera.transform.position.y;
            currentSave.cameraSize = mainCamera.orthographicSize;
        }

        // 🔥 NUEVO: Guardar la música actual que esté sonando
        if (MusicManager.Instance != null)
        {
            AudioClip currentClip = MusicManager.Instance.GetCurrentClip();
            currentSave.currentMusicName = currentClip != null ? currentClip.name : "";
        }

        if (BacklogManager.Instance != null)
            currentSave.backlogData = BacklogManager.Instance.GetBacklogSaveData();
        else
            currentSave.backlogData = new BacklogSaveData();

        SaveableObject[] allSaveables = FindObjectsOfType<SaveableObject>(true);
        foreach (SaveableObject so in allSaveables)
        {
            if (!string.IsNullOrEmpty(so.objectId))
            {
                RegisterObjectState(so.objectId, so.gameObject.activeSelf);
            }
        }

        currentSave.objectStates.Clear();
        foreach (var kvp in objectStates)
        {
            currentSave.objectStates.Add(new ObjectState { objectId = kvp.Key, isActive = kvp.Value });
        }

        string json = JsonUtility.ToJson(currentSave, true);
        File.WriteAllText(savePath, json);
        Debug.Log("💾 Partida guardada en: " + savePath);
    }

    public void RegisterObjectDestroyed(string objectId)
    {
        if (string.IsNullOrEmpty(objectId)) return;
        if (!currentSave.destroyedObjects.Contains(objectId))
            currentSave.destroyedObjects.Add(objectId);
        // Elimina su estado de activación si existía
        objectStates.Remove(objectId);
    }

    // ---------- CARGAR ----------
    public bool LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("⚠️ No hay partida guardada.");
            return false;
        }

        string json = File.ReadAllText(savePath);
        currentSave = JsonUtility.FromJson<SaveData>(json);

        if (currentSave == null)
        {
            Debug.LogError("❌ Error al cargar el archivo de guardado.");
            return false;
        }

        objectStates.Clear();
        foreach (var state in currentSave.objectStates)
        {
            objectStates[state.objectId] = state.isActive;
        }

        Debug.Log("📂 Partida cargada correctamente. Escena: " + currentSave.sceneName);

        isLoadingFromSave = true;
        SceneManager.LoadScene(currentSave.sceneName);
        return true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("🔄 OnSceneLoaded: " + scene.name);

        if (isLoadingFromSave)
        {
            SaveableObject[] allSaveables = FindObjectsOfType<SaveableObject>(true);
            foreach (SaveableObject so in allSaveables)
            {
                if (currentSave.destroyedObjects.Contains(so.objectId))
                {
                    Destroy(so.gameObject);
                }
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(currentSave.playerX, currentSave.playerY, 0);
            }

            StartCoroutine(RestoreCameraAfterFrame());
            RestoreObjectStates();

            // 🔥 NUEVO: Restaurar la música de la partida guardada
            if (MusicManager.Instance != null && !string.IsNullOrEmpty(currentSave.currentMusicName))
            {
                MusicManager.Instance.ChangeMusicByName(currentSave.currentMusicName);
            }

            if (currentSave.backlogData != null && BacklogManager.Instance != null)
            {
                BacklogManager.Instance.LoadBacklogFromSaveData(currentSave.backlogData);
            }

            isLoadingFromSave = false;
        }
    }

    // ---------- MÉTODOS DE RECORRIDOS / TRIGGERS ----------
    public void RegisterPathCompleted(string pathId)
    {
        if (!string.IsNullOrEmpty(pathId) && !currentSave.completedPaths.Contains(pathId))
        {
            currentSave.completedPaths.Add(pathId);
            Debug.Log($"🚩 Recorrido registrado como activado/completado: '{pathId}'");
        }
    }

    public bool IsPathCompleted(string pathId)
    {
        if (string.IsNullOrEmpty(pathId)) return false;
        return currentSave.completedPaths.Contains(pathId);
    }

    // ---------- MÉTODOS DE TEXTO DE MISIÓN ----------
    public void RegisterMissionText(string text)
    {
        currentSave.lastMissionText = text;
        currentSave.hasSavedMissionText = true;
        Debug.Log($"📝 Último texto de misión registrado: '{text}'");
    }

    public bool TryGetLastMissionText(out string text)
    {
        text = currentSave.lastMissionText;
        return currentSave.hasSavedMissionText;
    }

    // ---------- REGISTRAR Y CONSULTAR ESTADO DE OBJETO ----------
    public void RegisterObjectState(string objectId, bool isActive)
    {
        if (string.IsNullOrEmpty(objectId)) return;
        objectStates[objectId] = isActive;
    }

    public bool GetObjectState(string objectId, bool defaultValue = true)
    {
        if (objectStates.TryGetValue(objectId, out bool state))
            return state;
        return defaultValue;
    }

    // ---------- OTROS MÉTODOS DE PROGRESO ----------
    public void RegisterDialogueCompleted(string dialogueId)
    {
        if (!currentSave.dialoguesCompleted.Contains(dialogueId))
            currentSave.dialoguesCompleted.Add(dialogueId);
    }

    public void RegisterPuzzleCompleted(string puzzleId)
    {
        if (!currentSave.puzzlesCompleted.Contains(puzzleId))
            currentSave.puzzlesCompleted.Add(puzzleId);
    }

    public void RegisterZoneUnlocked(string zoneName)
    {
        if (!currentSave.unlockedZones.Contains(zoneName))
            currentSave.unlockedZones.Add(zoneName);
    }

    public bool IsDialogueCompleted(string dialogueId) => currentSave.dialoguesCompleted.Contains(dialogueId);
    public bool IsPuzzleCompleted(string puzzleId) => currentSave.puzzlesCompleted.Contains(puzzleId);

    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("🗑️ Partida eliminada.");
        }
        currentSave = new SaveData();
        objectStates.Clear();
        isLoadingFromSave = false;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private IEnumerator RestoreCameraAfterFrame()
    {
        yield return null;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 camPos = new Vector3(currentSave.cameraX, currentSave.cameraY, mainCamera.transform.position.z);
            mainCamera.transform.position = camPos;
            mainCamera.orthographicSize = currentSave.cameraSize;
        }
    }

    private void RestoreObjectStates()
    {
        SaveableObject[] allSaveables = FindObjectsOfType<SaveableObject>(true);

        int restoredCount = 0;
        foreach (SaveableObject so in allSaveables)
        {
            if (string.IsNullOrEmpty(so.objectId)) continue;

            if (objectStates.TryGetValue(so.objectId, out bool savedIsActive))
            {
                so.gameObject.SetActive(savedIsActive);
                restoredCount++;
            }
        }

        Debug.Log($"🔧 Restaurados {restoredCount} objetos.");
    }

    public bool HasSaveFile()
    {
        return File.Exists(savePath);
    }
}