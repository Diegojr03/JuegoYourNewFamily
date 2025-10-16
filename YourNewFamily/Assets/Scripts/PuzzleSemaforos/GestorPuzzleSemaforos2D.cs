using UnityEngine;
using System.Collections.Generic;

public class GestorPuzzleSemaforos2D : MonoBehaviour
{
    [Header("Configuración Semaforos")]
    public List<SemaforoPuzzle2D> semaforos = new List<SemaforoPuzzle2D>();

    [Header("Configuración Puzzle")]
    [Tooltip("Número total de semáforos que deben tener el botón ARRIBA activo")]
    public int requeridosArriba = 2;
    [Tooltip("Número total de semáforos que deben tener el botón MEDIO activo")]
    public int requeridosMedio = 3;
    [Tooltip("Número total de semáforos que deben tener el botón ABAJO activo")]
    public int requeridosAbajo = 1;

    [Header("Objetivos del Puzzle 2D")]
    public GameObject puerta;
    public Collider2D colliderPuerta;
    public Animator animatorPuerta;
    public string triggerAbrirPuerta = "Abrir";

    [Header("Feedback 2D")]
    public AudioClip sonidoCompletado;
    public ParticleSystem particulasCompletado;

    private bool puzzleCompletado = false;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        ConfigurarSemaforos();
        VerificarPuzzle();
    }

    private void ConfigurarSemaforos()
    {
        foreach (var semaforo in semaforos)
        {
            if (semaforo != null)
            {
                semaforo.OnEstadoCambiado += OnSemaforoCambiado;
                Debug.Log($"Semaforo 2D {semaforo.name} configurado en el gestor");
            }
            else
            {
                Debug.LogError("Hay un semáforo nulo en la lista del gestor!");
            }
        }
    }

    private void OnSemaforoCambiado(SemaforoPuzzle2D semaforo)
    {
        if (!puzzleCompletado)
        {
            Debug.Log($"Estado cambiado en {semaforo.name}, verificando puzzle...");
            VerificarPuzzle();
        }
    }

    private void VerificarPuzzle()
    {
        // Contadores para cada tipo de botón
        int contadorArriba = 0;
        int contadorMedio = 0;
        int contadorAbajo = 0;

        // Contar cuántos semáforos tienen cada botón activo
        foreach (var semaforo in semaforos)
        {
            if (semaforo != null)
            {
                if (semaforo.GetArribaActivo()) contadorArriba++;
                if (semaforo.GetMedioActivo()) contadorMedio++;
                if (semaforo.GetAbajoActivo()) contadorAbajo++;
            }
        }

        // Verificar si se cumplen los requisitos
        bool condicionCumplida = contadorArriba == requeridosArriba &&
                                contadorMedio == requeridosMedio &&
                                contadorAbajo == requeridosAbajo;

        Debug.Log($"Estado Puzzle 2D - " +
                 $"Arriba: {contadorArriba}/{requeridosArriba}, " +
                 $"Medio: {contadorMedio}/{requeridosMedio}, " +
                 $"Abajo: {contadorAbajo}/{requeridosAbajo}");

        if (condicionCumplida && !puzzleCompletado)
        {
            CompletarPuzzle();
        }
    }

    private void CompletarPuzzle()
    {
        puzzleCompletado = true;
        Debug.Log("¡Puzzle de semáforos 2D completado!");

        // NUEVO: Desactivar todos los semáforos
        DesactivarTodosLosSemaforos();

        // Abrir puerta en 2D
        if (puerta != null)
        {
            puerta.SetActive(false);
        }

        // Animación de puerta 2D
        if (animatorPuerta != null)
        {
            animatorPuerta.SetTrigger(triggerAbrirPuerta);
        }

        // Sonido
        if (sonidoCompletado != null)
        {
            audioSource.PlayOneShot(sonidoCompletado);
        }

        // Partículas
        if (particulasCompletado != null)
        {
            particulasCompletado.Play();
        }

        // Limpiar suscripciones
        foreach (var semaforo in semaforos)
        {
            if (semaforo != null)
            {
                semaforo.OnEstadoCambiado -= OnSemaforoCambiado;
            }
        }
    }

    // NUEVO MÉTODO: Desactivar todos los semáforos
    private void DesactivarTodosLosSemaforos()
    {
        foreach (var semaforo in semaforos)
        {
            if (semaforo != null)
            {
                semaforo.DesactivarSemaforo();
            }
        }
        Debug.Log("Todos los semáforos han sido desactivados");
    }

    [ContextMenu("Forzar Verificación")]
    public void ForzarVerificacion()
    {
        VerificarPuzzle();
    }

    [ContextMenu("Reiniciar Puzzle 2D")]
    public void ReiniciarPuzzle()
    {
        puzzleCompletado = false;

        // Reactivar todos los semáforos
        foreach (var semaforo in semaforos)
        {
            if (semaforo != null)
            {
                // Aquí necesitaríamos un método para reactivar los semáforos
                // Por ahora solo reseteamos el estado del puzzle
                Debug.Log($"Semaforo 2D {semaforo.name} - Estado actual: Arriba:{semaforo.GetArribaActivo()}, Medio:{semaforo.GetMedioActivo()}, Abajo:{semaforo.GetAbajoActivo()}");
            }
        }

        // Resuscribirse a eventos
        ConfigurarSemaforos();
        VerificarPuzzle();
    }

    private void OnDestroy()
    {
        foreach (var semaforo in semaforos)
        {
            if (semaforo != null)
            {
                semaforo.OnEstadoCambiado -= OnSemaforoCambiado;
            }
        }
    }
}