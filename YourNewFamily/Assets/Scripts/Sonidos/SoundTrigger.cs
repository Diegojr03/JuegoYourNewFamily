using UnityEngine;

public class SoundTrigger : MonoBehaviour
{
    [Header("Configuración del Sonido")]
    public AudioClip soundToPlay;
    public float volume = 1f;
    public bool playOnce = true;
    public bool destroyAfterSound = false;

    [Header("Objetos a Activar después del Sonido")]
    public GameObject[] objectsToActivate;

    [Header("Objetos a Destruir después del Sonido")]
    public GameObject[] objectsToDestroy;

    private AudioSource audioSource;
    private bool hasPlayed = false;

    void Start()
    {
        // Añadir AudioSource si no existe
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.clip = soundToPlay;
        audioSource.volume = volume;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlaySound();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlaySound();
        }
    }

    void PlaySound()
    {
        if (soundToPlay == null) return;

        // Si está configurado para reproducir solo una vez y ya se reprodujo, salir
        if (playOnce && hasPlayed) return;

        // Reproducir sonido
        audioSource.Play();
        hasPlayed = true;

        // Programar acciones para después del sonido
        if (soundToPlay != null)
        {
            float soundDuration = soundToPlay.length;
            Invoke("ExecuteAfterSound", soundDuration);
        }
        else
        {
            // Si no hay sonido, ejecutar inmediatamente
            ExecuteAfterSound();
        }
    }

    void ExecuteAfterSound()
    {
        // Activar objetos
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        // Destruir objetos
        foreach (GameObject obj in objectsToDestroy)
        {
            if (obj != null)
                Destroy(obj);
        }

        // Destruir este objeto si está configurado
        if (destroyAfterSound)
        {
            Destroy(gameObject);
        }
    }

    // Método público para activar manualmente desde otros scripts
    public void TriggerSoundManually()
    {
        PlaySound();
    }
}
