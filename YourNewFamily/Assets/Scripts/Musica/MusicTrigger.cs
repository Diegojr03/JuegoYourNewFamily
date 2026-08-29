using UnityEngine;
using System.Collections;

public class MusicTrigger : MonoBehaviour
{
    [Header("Música a reproducir")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private float fadeDuration = -1; // -1 usa el valor por defecto

    [Header("Configuración del Trigger")]
    [SerializeField] private bool disableOnActivate = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Control de Música")]
    [SerializeField] private bool stopMusicOnTrigger = false; // Si true, detiene la música

    [Header("Restauración al salir (opcional)")]
    [SerializeField] private bool restorePreviousMusicOnExit = false; // Vuelve a la canción anterior al salir

    private bool hasBeenActivated = false;
    private Collider triggerCollider;
    private Collider2D triggerCollider2D;
    private AudioClip previousClip;

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

        // Precargar la música si hay clip y no es modo stop
        if (MusicManager.Instance != null && musicClip != null && !stopMusicOnTrigger)
        {
            MusicManager.Instance.PreloadMusic(musicClip);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenActivated) return;
        if (other.CompareTag(playerTag)) ActivateMusic();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenActivated) return;
        if (other.CompareTag(playerTag)) ActivateMusic();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!hasBeenActivated) return;
        if (other.CompareTag(playerTag)) DeactivateMusic();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!hasBeenActivated) return;
        if (other.CompareTag(playerTag)) DeactivateMusic();
    }

    private void ActivateMusic()
    {
        if (MusicManager.Instance == null) return;

        hasBeenActivated = true;

        // Modo STOP
        if (stopMusicOnTrigger)
        {
            Debug.Log($"Deteniendo música por trigger: {gameObject.name}");
            previousClip = MusicManager.Instance.GetCurrentClip();
            MusicManager.Instance.StopMusic(fadeDuration > 0 ? fadeDuration : 0.5f);

            if (disableOnActivate)
                DesactivarCollider();

            return;
        }

        // Modo REPRODUCIR
        if (musicClip == null)
        {
            Debug.LogWarning("No hay clip de música asignado en " + gameObject.name);
            return;
        }

        if (disableOnActivate)
            DesactivarCollider();

        if (fadeDuration > 0)
            MusicManager.Instance.ChangeMusic(musicClip, fadeDuration);
        else
            MusicManager.Instance.ChangeMusic(musicClip);

        if (disableOnActivate)
            StartCoroutine(DesactivarGameObject());
    }

    private void DeactivateMusic()
    {
        if (MusicManager.Instance == null) return;

        // Si es modo stop y queremos restaurar la música anterior al salir
        if (stopMusicOnTrigger && restorePreviousMusicOnExit && previousClip != null)
        {
            Debug.Log($"Restaurando música anterior: {previousClip.name}");
            if (fadeDuration > 0)
                MusicManager.Instance.ChangeMusic(previousClip, fadeDuration);
            else
                MusicManager.Instance.ChangeMusic(previousClip);
        }
    }

    private void DesactivarCollider()
    {
        if (triggerCollider != null)
            triggerCollider.enabled = false;
        if (triggerCollider2D != null)
            triggerCollider2D.enabled = false;
    }

    private IEnumerator DesactivarGameObject()
    {
        yield return null;
        gameObject.SetActive(false);
    }

    // ===== GIZMOS PARA VISUALIZAR EN EL EDITOR =====
    private void OnDrawGizmos()
    {
        Gizmos.color = stopMusicOnTrigger ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f);

        if (triggerCollider != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            if (triggerCollider is BoxCollider box)
                Gizmos.DrawCube(box.center, box.size);
            else if (triggerCollider is SphereCollider sphere)
                Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
        else if (triggerCollider2D != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            if (triggerCollider2D is BoxCollider2D box2D)
                Gizmos.DrawCube(box2D.offset, box2D.size);
            else if (triggerCollider2D is CircleCollider2D circle2D)
                Gizmos.DrawSphere(circle2D.offset, circle2D.radius);
        }
    }
}