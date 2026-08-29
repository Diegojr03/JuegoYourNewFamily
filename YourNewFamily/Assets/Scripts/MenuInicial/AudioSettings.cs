using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    public AudioSource musicSource;

    public Slider musicSlider; // Único slider: control de música

    [Header("Configuración de Música")]
    public AudioClip[] musicTracks; // Arrastra aquí tus canciones
    public bool shuffleTracks = false;

    private int currentTrackIndex = 0;
    private bool isMusicPlaying = false;

    void Start()
    {
        // Cargar el valor guardado o usar 0.5 por defecto (50%)
        float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        musicSlider.value = savedMusicVolume;

        // Aplicar el volumen cargado
        SetMusicVolume(savedMusicVolume);

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

    // Método llamado por el slider para cambiar el volumen de la música
    public void SetMusicVolume(float value)
    {
        Debug.Log($"Music Volume cambiado a: {value}");
        if (musicSource != null)
        {
            musicSource.volume = value;
        }
        PlayerPrefs.SetFloat("MusicVolume", value);
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