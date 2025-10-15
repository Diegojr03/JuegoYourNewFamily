using UnityEngine;
using System.Collections.Generic;

public class GestorPuzzleSemaforos : MonoBehaviour
{
    [Header("Configuración Semaforos")]
    public List<SemaforoPuzzle> semaforos = new List<SemaforoPuzzle>();

    [Header("Configuración Puzzle")]
    [Tooltip("Número total de semáforos que deben tener el botón ARRIBA activo")]
    public int requeridosArriba = 2;
    [Tooltip("Número total de semáforos que deben tener el botón MEDIO activo")]
    public int requeridosMedio = 3;
    [Tooltip("Número total de semáforos que deben tener el botón ABAJO activo")]
    public int requeridosAbajo = 1;

    [Header("Objetivos del Puzzle")]
    public GameObject puerta;
    public Animator animatorPuerta;
    public string triggerAbrirPuerta = "Abrir";

    [Header("Feedback")]
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
            }
        }
    }

    private void OnSemaforoCambiado(SemaforoPuzzle semaforo)
    {
        if (!puzzleCompletado)
        {
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

        // Verificar si se cumplen los requisitos (no importa qué semáforos específicos)
        bool condicionCumplida = contadorArriba == requeridosArriba &&
                                contadorMedio == requeridosMedio &&
                                contadorAbajo == requeridosAbajo;

        Debug.Log($"Estado Puzzle - " +
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
        Debug.Log("¡Puzzle de semáforos completado!");

        // Abrir puerta
        if (puerta != null)
            puerta.SetActive(false);

        // Animación de puerta
        if (animatorPuerta != null)
            animatorPuerta.SetTrigger(triggerAbrirPuerta);

        // Sonido
        if (sonidoCompletado != null)
            audioSource.PlayOneShot(sonidoCompletado);

        // Partículas
        if (particulasCompletado != null)
            particulasCompletado.Play();

        // Limpiar suscripciones
        foreach (var semaforo in semaforos)
        {
            if (semaforo != null)
            {
                semaforo.OnEstadoCambiado -= OnSemaforoCambiado;
            }
        }
    }

    [ContextMenu("Forzar Verificación")]
    public void ForzarVerificacion()
    {
        VerificarPuzzle();
    }

    [ContextMenu("Reiniciar Puzzle")]
    public void ReiniciarPuzzle()
    {
        puzzleCompletado = false;

        foreach (var semaforo in semaforos)
        {
            if (semaforo != null)
            {
                // Resetear todos los botones
                var botones = new List<SemaforoPuzzle.BotonSemaforo>
                {
                    semaforo.botonArriba,
                    semaforo.botonMedio,
                    semaforo.botonAbajo
                };

                foreach (var boton in botones)
                {
                    boton.estaActivo = false;
                    if (boton.imagen != null && boton.spriteApagado != null)
                        boton.imagen.sprite = boton.spriteApagado;
                }
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