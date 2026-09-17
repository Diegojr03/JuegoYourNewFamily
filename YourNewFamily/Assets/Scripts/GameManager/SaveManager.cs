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
    // Version del formato. 1 = solo foto del mundo. 2 = progreso ordenado.
    public int saveVersion = 2;

    // El progreso de verdad: que eventos ha completado el jugador, en orden.
    // El estado del mundo se reconstruye a partir de esto al cargar, en vez
    // de restaurarse desde una foto que puede haberse tomado a medias.
    public List<string> eventosCompletados = new List<string>();

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

    // Clave de localizacion de la linea. Las partidas guardadas antes de
    // existir este campo lo leen vacio, y entonces el backlog busca la
    // traduccion por el texto, que es como funcionaba hasta ahora.
    public string locKey;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string savePath;
    private SaveData currentSave = new SaveData();
    private bool isLoadingFromSave = false;

    public bool IsLoading => isLoadingFromSave;

    // Se enciende cuando la escena actual se ha montado desde una partida
    // guardada, y se queda encendida mientras esa escena dure.
    //
    // isLoadingFromSave no sirve para esto: se apaga al final de
    // OnSceneLoaded, y OnSceneLoaded corre ANTES de los Start() de la escena,
    // que es justo donde hace falta saberlo. Cualquier script que en su
    // Start() fuerce un estado inicial (encender un NPC, apagar un dialogo)
    // esta pisando lo que el guardado acaba de reconstruir, y debe
    // preguntar por esto antes de hacerlo.
    private bool escenaVieneDePartidaGuardada = false;
    public bool EscenaVieneDePartidaGuardada => escenaVieneDePartidaGuardada;

    private Dictionary<string, bool> objectStates = new Dictionary<string, bool>();

    [Header("Diagnostico")]
    [Tooltip("Escribe en consola cada activacion, destruccion, registro y restauracion, "
           + "con el id del objeto. Para cazar estados que se pierden entre partidas.")]
    public bool trazasDeProgreso = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        YNF.Progreso.LogProgreso.Activo = trazasDeProgreso;

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

        currentSave.saveVersion = 2;
        string json = JsonUtility.ToJson(currentSave, true);

        // Escritura atomica: si el juego se cierra a media escritura, el
        // fichero bueno sigue intacto y ademas queda la copia anterior.
        try
        {
            string temporal = savePath + ".tmp";
            File.WriteAllText(temporal, json);
            if (File.Exists(savePath)) File.Copy(savePath, savePath + ".bak", true);
            if (File.Exists(savePath)) File.Delete(savePath);
            File.Move(temporal, savePath);
            Debug.Log("💾 Partida guardada en: " + savePath);
            YNF.Progreso.LogProgreso.Info($"guardado: {currentSave.eventosCompletados.Count} eventos, "
                                        + $"{currentSave.objectStates.Count} estados de objeto.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ No se pudo guardar la partida: " + e.Message);
        }
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

        if (currentSave.eventosCompletados == null)
            currentSave.eventosCompletados = new List<string>();

        // Partida guardada con el formato viejo: se reconstruye la lista de
        // eventos a partir de las tres listas que ya existian.
        if (currentSave.eventosCompletados.Count == 0)
        {
            foreach (string id in currentSave.dialoguesCompleted) RegistrarEvento(id);
            foreach (string id in currentSave.completedPaths) RegistrarEvento(id);
            foreach (string id in currentSave.puzzlesCompleted) RegistrarEvento(id);
            Debug.Log($"🔁 Partida antigua migrada: {currentSave.eventosCompletados.Count} eventos de progreso.");
        }

        Debug.Log("📂 Partida cargada correctamente. Escena: " + currentSave.sceneName);

        isLoadingFromSave = true;
        SceneManager.LoadScene(currentSave.sceneName);
        return true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("🔄 OnSceneLoaded: " + scene.name);

        escenaVieneDePartidaGuardada = isLoadingFromSave;

        if (isLoadingFromSave)
        {
            // 1. La foto de objetos, como base.
            RestoreObjectStates();

            // 2. La reproduccion del progreso manda por encima de la foto.
            //
            //    ORDEN CRITICO: esto va ANTES de destruir los objetos marcados
            //    como destruidos. Si se destruyen primero, los componentes que
            //    declaran las consecuencias de un evento (por ejemplo el dialogo
            //    de Peter, que enciende una conversacion y destruye otra) ya no
            //    existen cuando toca reproducirlos, y sus consecuencias se
            //    pierden para siempre. Era exactamente el bug que dejaba a
            //    Cairen sin conversacion disponible.
            YNF.Progreso.ReproductorDeProgreso.Reproducir(currentSave.eventosCompletados, "(carga)");

            // 3. Lo que siga marcado como destruido y haya sobrevivido a la
            //    reproduccion, se destruye ahora.
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

            // 4. Segunda pasada un fotograma despues, porque OnSceneLoaded corre
            //    ANTES de los Start() de la escena y alguno puede pisar el estado.
            StartCoroutine(ReproducirProgresoTrasUnFrame());

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

    private IEnumerator ReproducirProgresoTrasUnFrame()
    {
        yield return null;
        YNF.Progreso.ReproductorDeProgreso.Reproducir(currentSave.eventosCompletados, "(tras Start)");
    }

    /// <summary>
    /// Apunta un evento en la lista ordenada de progreso. Es lo unico que
    /// hace falta para que el mundo se pueda reconstruir al cargar.
    /// </summary>
    private void RegistrarEvento(string id)
    {
        if (string.IsNullOrEmpty(id) || id == "None") return;
        if (currentSave.eventosCompletados == null)
            currentSave.eventosCompletados = new List<string>();
        if (!currentSave.eventosCompletados.Contains(id))
        {
            currentSave.eventosCompletados.Add(id);
            YNF.Progreso.LogProgreso.Info($"evento completado: {id}");
        }
    }

    /// <summary>Informe de diagnostico de la escena actual.</summary>
    public string InformeDeProgreso() =>
        YNF.Progreso.ReproductorDeProgreso.Informe(currentSave.eventosCompletados);

    // ---------- MÉTODOS DE RECORRIDOS / TRIGGERS ----------
    public void RegisterPathCompleted(string pathId)
    {
        if (!string.IsNullOrEmpty(pathId) && !currentSave.completedPaths.Contains(pathId))
        {
            currentSave.completedPaths.Add(pathId);
            RegistrarEvento(pathId);
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
        YNF.Progreso.LogProgreso.Info($"estado: {objectId} = {isActive}");
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
        RegistrarEvento(dialogueId);
    }

    public void RegisterPuzzleCompleted(string puzzleId)
    {
        if (!currentSave.puzzlesCompleted.Contains(puzzleId))
            currentSave.puzzlesCompleted.Add(puzzleId);
        RegistrarEvento(puzzleId);
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
        YNF.Progreso.LogProgreso.Info($"restaurados {restoredCount} objetos desde la foto.");
    }

    public bool HasSaveFile()
    {
        return File.Exists(savePath);
    }
}