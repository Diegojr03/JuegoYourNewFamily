using UnityEngine;
using System.Collections;

public class MusicTrigger : MonoBehaviour
{
    [Header("Música a reproducir")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private float fadeDuration = -1;

    [Header("Control de Volumen")]
    [SerializeField][Range(0, 1)] private float volume = 1f;
    [SerializeField] private bool useCustomVolume = false;
    [SerializeField] private bool resetVolumeOnExit = false;

    [Header("Configuración del Trigger")]
    [SerializeField] private bool disableOnActivate = true;
    [SerializeField] private string playerTag = "Player";

    // 🔥 NUEVO: Opción para detener la música
    [Header("Control de Música")]
    [SerializeField] private bool stopMusicOnTrigger = false; // Si es true, detiene la música en lugar de reproducir

    [Header("Eventos Opcionales")]
    [SerializeField] private bool restorePreviousVolumeOnExit = false;

    private bool hasBeenActivated = false;
    private Collider triggerCollider;
    private Collider2D triggerCollider2D;
    private float previousVolume;
    private bool wasVolumeChanged = false;
    private AudioClip previousClip; // 🔥 NUEVO: Para guardar el clip anterior

    private void Start()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider2D = GetComponent<Collider2D>();

        if (triggerCollider == null && triggerCollider2D == null)
        {
            Debug.LogError("MusicTrigger necesita un Collider o Collider2D con Is Trigger activado");
            return;
        }

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

        if (triggerCollider2D != null)
            triggerCollider2D.isTrigger = true;

        // Precargar la música al inicio si hay clip asignado y no es modo stop
        if (MusicManager.Instance != null && musicClip != null && !stopMusicOnTrigger)
        {
            MusicManager.Instance.PreloadMusic(musicClip);

            if (useCustomVolume)
            {
                MusicManager.Instance.SetClipVolume(musicClip, volume);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenActivated) return;

        if (other.CompareTag(playerTag))
        {
            ActivateMusic();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenActivated) return;

        if (other.CompareTag(playerTag))
        {
            ActivateMusic();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!hasBeenActivated) return;

        if (other.CompareTag(playerTag))
        {
            DeactivateMusic();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!hasBeenActivated) return;

        if (other.CompareTag(playerTag))
        {
            DeactivateMusic();
        }
    }

    private void ActivateMusic()
    {
        if (MusicManager.Instance == null) return;

        // 🔥 MODIFICADO: Si stopMusicOnTrigger está activado, detener la música
        if (stopMusicOnTrigger)
        {
            Debug.Log($"Deteniendo música por trigger: {gameObject.name}");

            // Guardar el clip actual antes de detenerlo (por si queremos restaurar después)
            previousClip = MusicManager.Instance.GetCurrentClip();

            // Detener la música
            if (fadeDuration > 0)
            {
                MusicManager.Instance.StopMusic(fadeDuration);
            }
            else
            {
                MusicManager.Instance.StopMusic();
            }

            hasBeenActivated = true;

            // Desactivar collider si está configurado
            if (disableOnActivate)
            {
                if (triggerCollider != null)
                    triggerCollider.enabled = false;

                if (triggerCollider2D != null)
                    triggerCollider2D.enabled = false;
            }

            return;
        }

        // Si no es modo stop, continuar con la lógica normal de reproducción
        if (musicClip == null)
        {
            Debug.LogWarning("No hay clip de música asignado en " + gameObject.name);
            return;
        }

        hasBeenActivated = true;

        if (resetVolumeOnExit || restorePreviousVolumeOnExit)
        {
            previousVolume = MusicManager.Instance.GetClipVolume(musicClip);
            wasVolumeChanged = true;
        }

        if (useCustomVolume)
        {
            MusicManager.Instance.SetClipVolume(musicClip, volume);
        }

        if (disableOnActivate)
        {
            if (triggerCollider != null)
                triggerCollider.enabled = false;

            if (triggerCollider2D != null)
                triggerCollider2D.enabled = false;
        }

        if (fadeDuration > 0)
        {
            MusicManager.Instance.ChangeMusic(musicClip, fadeDuration);
        }
        else
        {
            MusicManager.Instance.ChangeMusic(musicClip);
        }

        if (disableOnActivate)
        {
            StartCoroutine(DisableAfterFrame());
        }
    }

    private void DeactivateMusic()
    {
        if (MusicManager.Instance == null) return;

        // Si es modo stop y queremos restaurar la música anterior al salir
        if (stopMusicOnTrigger && restorePreviousVolumeOnExit && previousClip != null)
        {
            Debug.Log($"Restaurando música anterior: {previousClip.name}");

            if (fadeDuration > 0)
            {
                MusicManager.Instance.ChangeMusic(previousClip, fadeDuration);
            }
            else
            {
                MusicManager.Instance.ChangeMusic(previousClip);
            }
        }

        // Restaurar el volumen anterior si está configurado (solo para modo reproducción normal)
        if (!stopMusicOnTrigger && wasVolumeChanged && (resetVolumeOnExit || restorePreviousVolumeOnExit))
        {
            if (restorePreviousVolumeOnExit)
            {
                MusicManager.Instance.SetClipVolume(musicClip, previousVolume);
            }
            else if (resetVolumeOnExit)
            {
                MusicManager.Instance.ResetClipVolume(musicClip);
            }
        }
    }

    private IEnumerator DisableAfterFrame()
    {
        yield return null;
        gameObject.SetActive(false);
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        if (useCustomVolume && musicClip != null && MusicManager.Instance != null && !stopMusicOnTrigger)
        {
            MusicManager.Instance.SetClipVolume(musicClip, volume);
        }
    }

    public void SetUseCustomVolume(bool use)
    {
        useCustomVolume = use;

        if (musicClip != null && MusicManager.Instance != null && !stopMusicOnTrigger)
        {
            if (use)
            {
                MusicManager.Instance.SetClipVolume(musicClip, volume);
            }
            else
            {
                MusicManager.Instance.ResetClipVolume(musicClip);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Cambiar color según el modo (verde para reproducir, rojo para detener)
        Gizmos.color = stopMusicOnTrigger ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f);

        if (triggerCollider != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            if (triggerCollider is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (triggerCollider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
        else if (triggerCollider2D != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            if (triggerCollider2D is BoxCollider2D box2D)
            {
                Gizmos.DrawCube(box2D.offset, box2D.size);
            }
            else if (triggerCollider2D is CircleCollider2D circle2D)
            {
                Gizmos.DrawSphere(circle2D.offset, circle2D.radius);
            }
        }
    }

    private void OnDestroy()
    {
        if (wasVolumeChanged && musicClip != null && MusicManager.Instance != null && !stopMusicOnTrigger)
        {
            if (resetVolumeOnExit)
            {
                MusicManager.Instance.ResetClipVolume(musicClip);
            }
        }
    }
}