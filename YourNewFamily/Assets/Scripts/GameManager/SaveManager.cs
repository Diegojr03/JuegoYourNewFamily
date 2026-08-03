using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SaveData
{
    public string sceneName;                // Nombre de la escena actual
    public float playerX;                  // Posición X del jugador
    public float playerY;                  // Posición Y del jugador
    public List<string> dialoguesCompleted = new List<string>(); // IDs de diálogos ya vistos
    public List<string> puzzlesCompleted = new List<string>();   // IDs de puzzles resueltos
    public List<ObjectState> objectStates = new List<ObjectState>();  // Estados de objetos
    public List<string> unlockedZones = new List<string>(); // Zonas del mapa desbloqueadas
    public float cameraX;
    public float cameraY;
    public float cameraSize;
}

[System.Serializable]
public class ObjectState
{
    public string objectId;
    public bool isActive;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string savePath;
    private SaveData currentSave = new SaveData();
    private bool isLoadingFromSave = false; // 🔥 NUEVA BANDERA

    void Awake()
    {
        // Patrón Singleton: solo debe existir uno en toda la partida
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persiste entre escenas

        // Ruta donde se guardará el archivo
        savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        Debug.Log("📁 Ruta de guardado: " + savePath);

        // Suscribirse al evento de carga de escenas
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        Debug.Log("✅ SaveManager iniciado correctamente.");
    }

    void Update()
    {
        // Guardar con F5
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("🔹 G Presionada - Guardando...");
            SaveGame();
        }

        // Cargar con F9
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("🔹 H Presionada - Cargando...");
            LoadGame();
        }
    }

    // ---------- GUARDAR ----------
    public void SaveGame()
    {
        // 1. Guardar nombre de la escena actual
        currentSave.sceneName = SceneManager.GetActiveScene().name;

        // 2. Guardar posición del jugador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 pos = player.transform.position;
            currentSave.playerX = pos.x;
            currentSave.playerY = pos.y;
        }

        // Guardar cámara
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            currentSave.cameraX = mainCamera.transform.position.x;
            currentSave.cameraY = mainCamera.transform.position.y;
            currentSave.cameraSize = mainCamera.orthographicSize;
            Debug.Log($"📷 Cámara guardada: ({currentSave.cameraX}, {currentSave.cameraY}) size {currentSave.cameraSize}");
        }
        else
        {
            Debug.LogWarning("No se encontró Camera.main al guardar.");
        }

        // 3. Convertir el JSON a texto y guardarlo en disco
        string json = JsonUtility.ToJson(currentSave, true);
        File.WriteAllText(savePath, json);
        Debug.Log("💾 Partida guardada en: " + savePath);
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

        Debug.Log("📂 Partida cargada correctamente. Escena: " + currentSave.sceneName);

        // 🔥 MARCAR QUE ESTAMOS CARGANDO DESDE GUARDADO
        isLoadingFromSave = true;

        SceneManager.LoadScene(currentSave.sceneName);
        return true;
    }

    // Evento que se dispara cuando se carga una escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("🔄 OnSceneLoaded: " + scene.name);

        if (isLoadingFromSave)
        {
            // Restaurar jugador (inmediato, como antes)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(currentSave.playerX, currentSave.playerY, 0);
            }

            // Restaurar cámara con un frame de retraso
            StartCoroutine(RestoreCameraAfterFrame());

            isLoadingFromSave = false;
        }
        else
        {
            Debug.Log("ℹ️ Carga de escena normal (sin restauración de posición).");
        }
    }

    // ---------- MÉTODOS PARA REGISTRAR PROGRESO ----------

    public void RegisterDialogueCompleted(string dialogueId)
    {
        if (!currentSave.dialoguesCompleted.Contains(dialogueId))
        {
            currentSave.dialoguesCompleted.Add(dialogueId);
            Debug.Log($"📝 Diálogo registrado: {dialogueId}");
        }
    }

    public void RegisterPuzzleCompleted(string puzzleId)
    {
        if (!currentSave.puzzlesCompleted.Contains(puzzleId))
        {
            currentSave.puzzlesCompleted.Add(puzzleId);
            Debug.Log($"🧩 Puzzle registrado: {puzzleId}");
        }
    }

    public void RegisterZoneUnlocked(string zoneName)
    {
        if (!currentSave.unlockedZones.Contains(zoneName))
        {
            currentSave.unlockedZones.Add(zoneName);
            Debug.Log($"🗺️ Zona registrada: {zoneName}");
        }
    }

    public bool IsDialogueCompleted(string dialogueId)
    {
        return currentSave.dialoguesCompleted.Contains(dialogueId);
    }

    public bool IsPuzzleCompleted(string puzzleId)
    {
        return currentSave.puzzlesCompleted.Contains(puzzleId);
    }

    // ---------- ELIMINAR GUARDADO ----------
    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("🗑️ Partida eliminada.");
        }
        currentSave = new SaveData();
        isLoadingFromSave = false; // 🔥 Aseguramos que no se restaure nada
    }

    void OnDestroy()
    {
        // Limpiar suscripción al evento
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private IEnumerator RestoreCameraAfterFrame()
    {
        // Esperar un frame para que todos los scripts de inicio terminen
        yield return null;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 camPos = new Vector3(currentSave.cameraX, currentSave.cameraY, mainCamera.transform.position.z);
            mainCamera.transform.position = camPos;
            mainCamera.orthographicSize = currentSave.cameraSize;
            Debug.Log($"📷 Cámara restaurada (después de frame): ({currentSave.cameraX}, {currentSave.cameraY}) size {currentSave.cameraSize}");
        }
    }
}