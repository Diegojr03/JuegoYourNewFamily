using UnityEngine;
using System.Collections;

public class FootstepManager : MonoBehaviour
{
    [Header("Configuración de Pasos")]
    [Tooltip("Intervalo base entre pasos (segundos)")]
    public float stepInterval = 0.5f;
    [Tooltip("Multiplicador para cuando el personaje está esprintando (menor = pasos más rápidos)")]
    public float sprintStepMultiplier = 0.7f;
    [Tooltip("Volumen de los pasos")]
    public float volume = 0.7f;
    [Tooltip("Tono fijo (sin variación)")]
    public float fixedPitch = 1f;

    [Header("Sonido por Defecto (cuando no hay zona)")]
    public AudioClip defaultFootstepSound;

    private AudioSource audioSource;
    private FootstepZone currentZone;
    private bool isPlaying = false;

    void Awake()
    {
        // Crear AudioSource si no existe
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
            audioSource.pitch = fixedPitch; // Tono fijo
        }
    }

    void Start()
    {
        if (defaultFootstepSound == null)
        {
            Debug.LogWarning("No se ha asignado un sonido de pisada por defecto en " + gameObject.name);
        }
    }

    // Devuelve el intervalo actual (considerando si está esprintando)
    public float GetCurrentInterval(bool isSprinting)
    {
        return isSprinting ? stepInterval * sprintStepMultiplier : stepInterval;
    }

    // Reproducir un paso
    public void PlayFootstep()
    {
        AudioClip clip = GetCurrentFootstepSound();
        if (clip == null) return;

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = fixedPitch; // Siempre fijo
        audioSource.PlayOneShot(clip);
    }

    // Método llamado mientras se mueve (para verificar si cambió la zona)
    public void EnsureFootstepsActive()
    {
        if (currentZone != null && currentZone.HasChanged())
        {
            StopFootsteps();
        }
    }

    public void StopFootsteps()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();
        isPlaying = false;
    }

    private AudioClip GetCurrentFootstepSound()
    {
        if (currentZone != null && currentZone.footstepSound != null)
            return currentZone.footstepSound;
        else
            return defaultFootstepSound;
    }

    // Cambiar de zona (llamado desde los triggers)
    public void SetZone(FootstepZone newZone)
    {
        if (currentZone != newZone)
        {
            currentZone = newZone;
            StopFootsteps();
            if (newZone != null && newZone.playOnEnter)
            {
                PlayFootstep();
            }
        }
    }

    public void ExitZone()
    {
        currentZone = null;
        StopFootsteps();
    }

    // Método para ajustar el volumen dinámicamente (opcional)
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
    }
}