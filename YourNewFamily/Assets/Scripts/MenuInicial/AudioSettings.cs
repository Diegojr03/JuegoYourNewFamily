using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioSource sfxSource;

    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Configuración de Música")]
    public AudioClip[] musicTracks; // Arrastra aquí tus 3 canciones
    public bool shuffleTracks = false; // Opcional: reproducir en orden aleatorio

    private int currentTrackIndex = 0;
    private bool isMusicPlaying = false;

    void Start()
    {
        // Cargar valores guardados
        masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Aplicar los valores cargados
        SetMasterVolume(masterSlider.value);
        SetMusicVolume(musicSlider.value);
        SetSFXVolume(sfxSlider.value);

        // Iniciar reproducción de música si hay tracks
        if (musicTracks != null && musicTracks.Length > 0)
        {
            StartMusicPlaylist();
        }
    }

    void Update()
    {
        // Verificar si la canción actual terminó y pasar a la siguiente
        if (isMusicPlaying && musicSource != null && !musicSource.isPlaying)
        {
            PlayNextTrack();
        }
    }

    private void StartMusicPlaylist()
    {
        if (shuffleTracks)
        {
            currentTrackIndex = Random.Range(0, musicTracks.Length);
        }
        else
        {
            currentTrackIndex = 0;
        }

        PlayCurrentTrack();
        isMusicPlaying = true;
    }

    private void PlayCurrentTrack()
    {
        if (musicSource != null && musicTracks.Length > 0 && currentTrackIndex < musicTracks.Length)
        {
            musicSource.clip = musicTracks[currentTrackIndex];
            musicSource.Play();
            Debug.Log($"Reproduciendo: {musicTracks[currentTrackIndex].name} - Volumen: {musicSource.volume}");
        }
    }

    private void PlayNextTrack()
    {
        if (musicTracks.Length == 0) return;

        if (shuffleTracks)
        {
            currentTrackIndex = Random.Range(0, musicTracks.Length);
        }
        else
        {
            currentTrackIndex = (currentTrackIndex + 1) % musicTracks.Length;
        }

        PlayCurrentTrack();
    }

    // Método público para forzar cambio de canción (opcional)
    public void SkipToNextTrack()
    {
        if (isMusicPlaying)
        {
            PlayNextTrack();
        }
    }

    public void SetMasterVolume(float value)
    {
        Debug.Log($"Master Volume cambiado a: {value}");
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        Debug.Log($"Music Volume cambiado a: {value}");
        if (musicSource != null)
        {
            musicSource.volume = value;
            Debug.Log($"Volumen actual del MusicSource: {musicSource.volume}");
        }
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        Debug.Log($"SFX Volume cambiado a: {value}");
        if (sfxSource != null)
            sfxSource.volume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    // Métodos adicionales para control de música
    public void PauseMusic()
    {
        if (musicSource != null)
        {
            musicSource.Pause();
            isMusicPlaying = false;
        }
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.Play();
            isMusicPlaying = true;
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            isMusicPlaying = false;
        }
    }

    // Para reiniciar la playlist desde el principio
    public void RestartPlaylist()
    {
        if (musicTracks.Length > 0)
        {
            currentTrackIndex = 0;
            PlayCurrentTrack();
            isMusicPlaying = true;
        }
    }
}
