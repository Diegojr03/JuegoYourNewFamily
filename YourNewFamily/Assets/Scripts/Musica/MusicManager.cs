using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private float defaultFadeDuration = 2f;
    [SerializeField][Range(0, 1)] private float maxVolume = 1f;
    [SerializeField] private float loopFadeDuration = 0.5f; // Duración del fade en loop

    private AudioSource musicSource1;
    private AudioSource musicSource2;
    private AudioSource currentSource;
    private AudioSource nextSource;
    private Coroutine fadeCoroutine;
    private Coroutine loopFadeCoroutine;

    // Diccionario para almacenar volúmenes específicos por clip
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
        }
        else
        {
            Destroy(gameObject);
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

        // Configuración para carga optimizada
        musicSource1.priority = 0;
        musicSource2.priority = 0;

        musicSource1.volume = 0;
        musicSource2.volume = 0;

        currentSource = musicSource1;
        nextSource = musicSource2;
    }

    private void Update()
    {
        // Detectar cuando una canción está cerca de terminar para hacer fade en loop
        if (currentSource != null && currentSource.isPlaying && currentSource.clip != null)
        {
            float timeLeft = currentSource.clip.length - currentSource.time;

            // Si quedan menos de {loopFadeDuration} segundos y no hay corrutina de loop activa
            if (timeLeft <= loopFadeDuration && timeLeft > 0 && loopFadeCoroutine == null)
            {
                loopFadeCoroutine = StartCoroutine(LoopFade());
            }
        }
    }

    private IEnumerator LoopFade()
    {
        float elapsedTime = 0;
        float startVolume = currentSource.volume;

        // Fade out rápido
        while (elapsedTime < loopFadeDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            currentSource.volume = Mathf.Lerp(startVolume, 0, elapsedTime / (loopFadeDuration / 2));
            yield return null;
        }

        // Pequeña pausa en el silencio (opcional, para que se note menos el corte)
        currentSource.volume = 0;
        yield return new WaitForSeconds(0.05f);

        elapsedTime = 0;

        // Obtener el volumen específico para este clip o usar el máximo por defecto
        float targetVolume = maxVolume;
        if (currentSource.clip != null && clipVolumes.ContainsKey(currentSource.clip.name))
        {
            targetVolume = clipVolumes[currentSource.clip.name];
        }

        // Fade in rápido
        while (elapsedTime < loopFadeDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            currentSource.volume = Mathf.Lerp(0, targetVolume, elapsedTime / (loopFadeDuration / 2));
            yield return null;
        }

        currentSource.volume = targetVolume;
        loopFadeCoroutine = null;
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

    public void PreloadMusicList(AudioClip[] clips)
    {
        foreach (var clip in clips)
        {
            PreloadMusic(clip);
        }
    }

    // Nuevo método para establecer el volumen específico de un clip
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

        // Si este clip está sonando actualmente, actualizar su volumen
        if (currentSource != null && currentSource.clip != null && currentSource.clip.name == clip.name)
        {
            currentSource.volume = volume;
        }
    }

    // Nuevo método para obtener el volumen específico de un clip
    public float GetClipVolume(AudioClip clip)
    {
        if (clip == null) return maxVolume;

        if (clipVolumes.ContainsKey(clip.name))
        {
            return clipVolumes[clip.name];
        }

        return maxVolume;
    }

    // Nuevo método para resetear el volumen de un clip al máximo por defecto
    public void ResetClipVolume(AudioClip clip)
    {
        if (clip == null) return;

        if (clipVolumes.ContainsKey(clip.name))
        {
            clipVolumes.Remove(clip.name);
        }

        // Si este clip está sonando actualmente, restaurar al volumen máximo
        if (currentSource != null && currentSource.clip != null && currentSource.clip.name == clip.name)
        {
            currentSource.volume = maxVolume;
        }
    }

    public void ChangeMusic(AudioClip newClip)
    {
        ChangeMusic(newClip, defaultFadeDuration);
    }

    public void ChangeMusic(AudioClip newClip, float fadeDuration)
    {
        if (newClip == null)
        {
            Debug.LogWarning("No se ha asignado ningún clip de audio");
            return;
        }

        // Cancelar loop fade si está activo
        if (loopFadeCoroutine != null)
        {
            StopCoroutine(loopFadeCoroutine);
            loopFadeCoroutine = null;
        }

        if (!loadedClips.ContainsKey(newClip.name))
        {
            loadedClips.Add(newClip.name, newClip);
            newClip.LoadAudioData();
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

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

        // Obtener el volumen objetivo para el nuevo clip
        float targetVolume = maxVolume;
        if (clipVolumes.ContainsKey(newClip.name))
        {
            targetVolume = clipVolumes[newClip.name];
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            if (currentSource.isPlaying)
                currentSource.volume = Mathf.Lerp(maxVolume, 0, t);

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
        maxVolume = Mathf.Clamp01(volume);
        if (currentSource != null && currentSource.isPlaying)
        {
            currentSource.volume = maxVolume;
        }
    }

    // Nueva función para ajustar el fade del loop
    public void SetLoopFadeDuration(float duration)
    {
        loopFadeDuration = duration;
    }

    private void OnDestroy()
    {
        foreach (var clip in loadedClips.Values)
        {
            if (clip != null)
                clip.UnloadAudioData();
        }
        loadedClips.Clear();
        clipVolumes.Clear();
    }
}