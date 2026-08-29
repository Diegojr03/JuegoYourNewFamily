using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private float defaultFadeDuration = 2f;
    [SerializeField] private float loopFadeDuration = 0.5f;

    [Header("Biblioteca de Canciones (Opcional)")]
    [SerializeField] private List<AudioClip> allMusicClips = new List<AudioClip>();

    [Header("Configuración por Escena")]
    [SerializeField] private string[] scenesToStopMusic = { "MenuInicial" };
    [SerializeField] private string[] scenesToResumeMusic = { "SampleScene" };

    private AudioSource musicSource1;
    private AudioSource musicSource2;
    private AudioSource currentSource;
    private AudioSource nextSource;
    private Coroutine fadeCoroutine;
    private Coroutine loopFadeCoroutine;

    private float globalVolume = 0.5f; // valor por defecto: 50%
    private AudioClip lastPlayedClip;
    private float lastPlayedTime = 0f;
    private bool wasPlaying = false;
    private string currentScene;

    private Dictionary<string, AudioClip> loadedClips = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateAudioSources();

            // Precargar clips asignados en el Inspector
            foreach (var clip in allMusicClips)
            {
                if (clip != null && !loadedClips.ContainsKey(clip.name))
                {
                    loadedClips.Add(clip.name, clip);
                }
            }

            // Cargar volumen guardado, o usar 0.5 si no existe
            globalVolume = PlayerPrefs.GetFloat("VolumenMusica", 0.5f);

            SceneManager.sceneLoaded += OnSceneLoaded;

            Debug.Log($"MusicManager Awake - Volumen inicial: {globalVolume}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetVolume(globalVolume);
        currentScene = SceneManager.GetActiveScene().name;
        CheckSceneMusicState(currentScene);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        foreach (var clip in loadedClips.Values)
        {
            if (clip != null)
                clip.UnloadAudioData();
        }
        loadedClips.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Escena cargada: {scene.name}");
        currentScene = scene.name;

        if (SaveManager.Instance != null && SaveManager.Instance.IsLoading)
            return;

        CheckSceneMusicState(currentScene);
    }

    private void CheckSceneMusicState(string sceneName)
    {
        bool shouldStopMusic = false;
        foreach (string scene in scenesToStopMusic)
        {
            if (scene == sceneName)
            {
                shouldStopMusic = true;
                break;
            }
        }

        bool shouldResumeMusic = false;
        foreach (string scene in scenesToResumeMusic)
        {
            if (scene == sceneName)
            {
                shouldResumeMusic = true;
                break;
            }
        }

        if (shouldStopMusic)
        {
            Debug.Log($"Escena {sceneName} detectada - Deteniendo música");
            StopMusic(0.5f);
            wasPlaying = false;
        }
        else if (shouldResumeMusic && lastPlayedClip != null)
        {
            Debug.Log($"Escena {sceneName} detectada - Reanudando música");
            ChangeMusic(lastPlayedClip, 0.5f);

            if (currentSource != null && lastPlayedTime > 0)
            {
                currentSource.time = lastPlayedTime;
            }
        }
    }

    private void CreateAudioSources()
    {
        musicSource1 = gameObject.AddComponent<AudioSource>();
        musicSource2 = gameObject.AddComponent<AudioSource>();

        musicSource1.loop = true;
        musicSource2.loop = true;
        musicSource1.playOnAwake = false;
        musicSource2.playOnAwake = false;

        musicSource1.priority = 0;
        musicSource2.priority = 0;

        musicSource1.volume = 0;
        musicSource2.volume = 0;

        currentSource = musicSource1;
        nextSource = musicSource2;
    }

    private void Update()
    {
        if (currentSource != null && currentSource.isPlaying && currentSource.clip != null)
        {
            float timeLeft = currentSource.clip.length - currentSource.time;

            if (timeLeft <= loopFadeDuration && timeLeft > 0 && loopFadeCoroutine == null)
            {
                loopFadeCoroutine = StartCoroutine(LoopFade());
            }

            lastPlayedTime = currentSource.time;
            lastPlayedClip = currentSource.clip;
            wasPlaying = true;
        }
    }

    private IEnumerator LoopFade()
    {
        float elapsedTime = 0;
        float startVolume = currentSource.volume;

        while (elapsedTime < loopFadeDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            currentSource.volume = Mathf.Lerp(startVolume, 0, elapsedTime / (loopFadeDuration / 2));
            yield return null;
        }

        currentSource.volume = 0;
        yield return new WaitForSeconds(0.05f);

        elapsedTime = 0;
        float targetVolume = globalVolume; // ahora siempre es globalVolume

        while (elapsedTime < loopFadeDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            currentSource.volume = Mathf.Lerp(0, targetVolume, elapsedTime / (loopFadeDuration / 2));
            yield return null;
        }

        currentSource.volume = targetVolume;
        loopFadeCoroutine = null;
    }

    private void UpdateAllVolumes()
    {
        if (currentSource != null && currentSource.isPlaying)
            currentSource.volume = globalVolume;

        if (nextSource != null && nextSource.isPlaying)
            nextSource.volume = globalVolume;
    }

    public void PreloadMusic(AudioClip clip)
    {
        if (clip == null) return;

        string clipName = clip.name;

        if (!loadedClips.ContainsKey(clipName))
        {
            loadedClips.Add(clipName, clip);
            clip.LoadAudioData();
        }
    }

    public void ChangeMusicByName(string clipName, float fadeDuration = 0.5f)
    {
        if (string.IsNullOrEmpty(clipName)) return;

        if (currentSource != null && currentSource.isPlaying && currentSource.clip != null && currentSource.clip.name == clipName)
            return;

        if (loadedClips.TryGetValue(clipName, out AudioClip clip))
        {
            ChangeMusic(clip, fadeDuration);
            return;
        }

        foreach (var c in allMusicClips)
        {
            if (c != null && c.name == clipName)
            {
                ChangeMusic(c, fadeDuration);
                return;
            }
        }

        AudioClip resClip = Resources.Load<AudioClip>(clipName);
        if (resClip != null)
        {
            ChangeMusic(resClip, fadeDuration);
            return;
        }

        Debug.LogWarning($"MusicManager: No se pudo encontrar ningún AudioClip con el nombre '{clipName}'.");
    }

    public void ChangeMusic(AudioClip newClip)
    {
        ChangeMusic(newClip, defaultFadeDuration);
    }

    public void ChangeMusic(AudioClip newClip, float fadeDuration)
    {
        if (loopFadeCoroutine != null)
        {
            StopCoroutine(loopFadeCoroutine);
            loopFadeCoroutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (newClip == null)
        {
            Debug.Log("Cambiando a silencio (sin música)");
            StopMusic(fadeDuration);
            lastPlayedClip = null;
            wasPlaying = false;
            return;
        }

        if (!loadedClips.ContainsKey(newClip.name))
        {
            loadedClips.Add(newClip.name, newClip);
            newClip.LoadAudioData();
        }

        lastPlayedClip = newClip;
        wasPlaying = true;
        fadeCoroutine = StartCoroutine(Crossfade(newClip, fadeDuration));
    }

    private IEnumerator Crossfade(AudioClip newClip, float duration)
    {
        yield return null;

        AudioSource newSource = (currentSource == musicSource1) ? musicSource2 : musicSource1;

        newSource.clip = newClip;
        newSource.volume = 0;
        newSource.Play();

        float elapsedTime = 0;
        float targetVolume = globalVolume;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            if (currentSource.isPlaying)
                currentSource.volume = Mathf.Lerp(currentSource.volume, 0, t);

            newSource.volume = Mathf.Lerp(0, targetVolume, t);

            yield return null;
        }

        if (currentSource.isPlaying)
        {
            currentSource.volume = 0;
            currentSource.Stop();
        }

        newSource.volume = targetVolume;

        currentSource = newSource;
        nextSource = (currentSource == musicSource1) ? musicSource2 : musicSource1;

        fadeCoroutine = null;
    }

    public void StopMusic()
    {
        StopMusic(defaultFadeDuration);
    }

    public void StopMusic(float fadeDuration)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (loopFadeCoroutine != null)
        {
            StopCoroutine(loopFadeCoroutine);
            loopFadeCoroutine = null;
        }

        fadeCoroutine = StartCoroutine(FadeOut(fadeDuration));
    }

    private IEnumerator FadeOut(float duration)
    {
        float elapsedTime = 0;
        float startVolume = currentSource.volume;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            currentSource.volume = Mathf.Lerp(startVolume, 0, elapsedTime / duration);
            yield return null;
        }

        currentSource.volume = 0;
        currentSource.Stop();
        fadeCoroutine = null;
    }

    public void SetVolume(float volume)
    {
        globalVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
        PlayerPrefs.SetFloat("VolumenMusica", globalVolume);
        PlayerPrefs.Save();
        Debug.Log($"Volumen global cambiado a: {globalVolume}");
    }

    public float GetVolume()
    {
        return globalVolume;
    }

    public void SetLoopFadeDuration(float duration)
    {
        loopFadeDuration = duration;
    }

    public bool IsMusicPlaying()
    {
        return currentSource != null && currentSource.isPlaying;
    }

    public AudioClip GetCurrentClip()
    {
        return currentSource != null ? currentSource.clip : null;
    }
}