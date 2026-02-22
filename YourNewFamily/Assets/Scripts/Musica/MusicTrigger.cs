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

    [Header("Eventos Opcionales")]
    [SerializeField] private bool restorePreviousVolumeOnExit = false;

    private bool hasBeenActivated = false;
    private Collider triggerCollider;
    private Collider2D triggerCollider2D;
    private float previousVolume;
    private bool wasVolumeChanged = false;

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

        // Precargar la música al inicio para evitar el freeze
        if (MusicManager.Instance != null && musicClip != null)
        {
            MusicManager.Instance.PreloadMusic(musicClip);

            // Si usamos volumen personalizado, lo aplicamos desde el inicio
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
        if (musicClip == null)
        {
            Debug.LogWarning("No hay clip de música asignado en " + gameObject.name);
            return;
        }

        if (MusicManager.Instance != null)
        {
            hasBeenActivated = true;

            // Guardar el volumen actual antes de cambiarlo si es necesario
            if (resetVolumeOnExit || restorePreviousVolumeOnExit)
            {
                previousVolume = MusicManager.Instance.GetClipVolume(musicClip);
                wasVolumeChanged = true;
            }

            // Aplicar el volumen personalizado si está activado
            if (useCustomVolume)
            {
                MusicManager.Instance.SetClipVolume(musicClip, volume);
            }

            // Desactivar collider inmediatamente si solo queremos una activación única
            if (disableOnActivate)
            {
                if (triggerCollider != null)
                    triggerCollider.enabled = false;

                if (triggerCollider2D != null)
                    triggerCollider2D.enabled = false;
            }

            // Iniciar la música (ahora sin freeze porque está precargada)
            if (fadeDuration > 0)
            {
                MusicManager.Instance.ChangeMusic(musicClip, fadeDuration);
            }
            else
            {
                MusicManager.Instance.ChangeMusic(musicClip);
            }

            // Desactivar el objeto si está configurado
            if (disableOnActivate)
            {
                StartCoroutine(DisableAfterFrame());
            }
        }
    }

    private void DeactivateMusic()
    {
        // Restaurar el volumen anterior si está configurado
        if (wasVolumeChanged && (resetVolumeOnExit || restorePreviousVolumeOnExit))
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
        // Esperar un frame para asegurar que todo se procesó
        yield return null;
        gameObject.SetActive(false);
    }

    // Método público para cambiar el volumen en tiempo de ejecución
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        if (useCustomVolume && musicClip != null && MusicManager.Instance != null)
        {
            MusicManager.Instance.SetClipVolume(musicClip, volume);
        }
    }

    // Método público para activar/desactivar el volumen personalizado
    public void SetUseCustomVolume(bool use)
    {
        useCustomVolume = use;

        if (musicClip != null && MusicManager.Instance != null)
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
        Gizmos.color = new Color(0, 1, 0, 0.3f);

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
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
    }

    private void OnDestroy()
    {
        // Limpiar el volumen si el objeto es destruido y habíamos cambiado algo
        if (wasVolumeChanged && musicClip != null && MusicManager.Instance != null)
        {
            if (resetVolumeOnExit)
            {
                MusicManager.Instance.ResetClipVolume(musicClip);
            }
        }
    }
}