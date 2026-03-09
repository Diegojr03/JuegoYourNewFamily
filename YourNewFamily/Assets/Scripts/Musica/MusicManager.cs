using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private float defaultFadeDuration = 2f;
    [SerializeField][Range(0, 1)] private float maxVolume = 1f;
    [SerializeField] private float loopFadeDuration = 0.5f;

    [Header("Configuración por Escena")]
    [SerializeField] private string[] scenesToStopMusic = { "MenuInicial" }; // Escenas donde la música se detiene
    [SerializeField] private string[] scenesToResumeMusic = { "SampleScene" }; // Escenas donde la música se reanuda

    private AudioSource musicSource1;
    private AudioSource musicSource2;
    private AudioSource currentSource;
    private AudioSource nextSource;
    private Coroutine fadeCoroutine;
    private Coroutine loopFadeCoroutine;

    private float globalVolume = 1f;
    private AudioClip lastPlayedClip;
    private float lastPlayedTime = 0f;
    private bool wasPlaying = false;
    private string currentScene;

    // Diccionario para volúmenes específicos por clip
    private Dictionary<string, float> clipVolumes = new Dictionary<string, float>();

    // Cache de audio clips precargados
    private Dictionary<string, AudioClip> loadedClips = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateAudioSources();

            globalVolume = PlayerPrefs.GetFloat("VolumenMusica", 1f);

            // Suscribirse al evento de cambio de escena
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
        // Limpiar la suscripción al evento
        SceneManager.sceneLoaded -= OnSceneLoaded;

        foreach (var clip in loadedClips.Values)
        {
            if (clip != null)
                clip.UnloadAudioData();
        }
        loadedClips.Clear();
        clipVolumes.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Escena cargada: {scene.name}");
        currentScene = scene.name;
        CheckSceneMusicState(currentScene);
    }

    private void CheckSceneMusicState(string sceneName)
    {
        // Verificar si la escena actual está en la lista de detener música
        bool shouldStopMusic = false;
        foreach (string scene in scenesToStopMusic)
        {
            if (scene == sceneName)
            {
                shouldStopMusic = true;
                break;
            }
        }

        // Verificar si la escena actual está en la lista de reanudar música
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
            // Reanudar la última música que estaba sonando
            ChangeMusic(lastPlayedClip, 0.5f);

            // Restaurar el tiempo si es necesario
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

            // Guardar el tiempo actual para posible reanudación
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

        float targetVolume = GetFinalVolume(currentSource.clip);

        while (elapsedTime < loopFadeDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            currentSource.volume = Mathf.Lerp(0, targetVolume, elapsedTime / (loopFadeDuration / 2));
            yield return null;
        }

        currentSource.volume = targetVolume;
        loopFadeCoroutine = null;
    }

    private float GetFinalVolume(AudioClip clip)
    {
        float specificVolume = maxVolume;

        if (clip != null && clipVolumes.ContainsKey(clip.name))
        {
            specificVolume = clipVolumes[clip.name];
        }

        return specificVolume * globalVolume;
    }

    private void UpdateAllVolumes()
    {
        if (currentSource != null && currentSource.isPlaying)
        {
            currentSource.volume = GetFinalVolume(currentSource.clip);
        }

        if (nextSource != null && nextSource.isPlaying)
        {
            nextSource.volume = GetFinalVolume(nextSource.clip);
        }
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

    public void SetClipVolume(AudioClip clip, float volume)
    {
        if (clip == null) return;

        volume = Mathf.Clamp01(volume);

        if (clipVolumes.ContainsKey(clip.name))
        {
            clipVolumes[clip.name] = volume;
        }
        else
        {
            clipVolumes.Add(clip.name, volume);
        }

        UpdateAllVolumes();
    }

    public float GetClipVolume(AudioClip clip)
    {
        if (clip == null) return maxVolume;

        if (clipVolumes.ContainsKey(clip.name))
        {
            return clipVolumes[clip.name];
        }

        return maxVolume;
    }

    public void ResetClipVolume(AudioClip clip)
    {
        if (clip == null) return;

        if (clipVolumes.ContainsKey(clip.name))
        {
            clipVolumes.Remove(clip.name);
        }

        UpdateAllVolumes();
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

        // Si el nuevo clip es null, detener la música
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

        float targetVolume = GetFinalVolume(newClip);
        float currentStartVolume = currentSource.volume;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            if (currentSource.isPlaying)
                currentSource.volume = Mathf.Lerp(currentStartVolume, 0, t);

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

    // Método para verificar si hay música reproduciéndose
    public bool IsMusicPlaying()
    {
        return currentSource != null && currentSource.isPlaying;
    }

    // Método para obtener el clip actual
    public AudioClip GetCurrentClip()
    {
        return currentSource != null ? currentSource.clip : null;
    }
}