using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;

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

    [Header("Configuración Jugador")]
    public MonoBehaviour scriptMovimientoJugador; // Script que controla el movimiento del jugador
    public MonoBehaviour scriptInputJugador; // Script que maneja el input del jugador (para frenado más brusco)

    [Header("Feedback 2D")]
    public AudioClip sonidoCompletado;
    public ParticleSystem particulasCompletado;

    [Header("Diálogo de Completado")]
    public bool mostrarMensajeCompletado = true;
    public GameObject panelDialogoCompletado;
    public TextMeshProUGUI textoDialogoCompletado;
    public string mensajeCompletado = "Has completado el puzzle de los semáforos";
    public float tiempoMostrarMensaje = 3f;

    [Header("Objetos a Activar/Desactivar")]
    public GameObject[] objetosParaActivar;
    public GameObject[] objetosParaDesactivar;
    public GameObject[] objetosParaDestruir;

    private bool puzzleCompletado = false;
    private AudioSource audioSource;
    private Rigidbody2D rbJugador; // Para frenado más brusco
    private Vector2 velocidadAntesDeBloquear; // Guardar velocidad antes de bloquear

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Obtener referencia al Rigidbody2D del jugador para frenado brusco
        if (scriptMovimientoJugador != null)
        {
            rbJugador = scriptMovimientoJugador.GetComponent<Rigidbody2D>();
        }

        // IMPORTANTE: Ocultar el panel de diálogo al inicio
        if (panelDialogoCompletado != null)
        {
            panelDialogoCompletado.SetActive(false);
        }

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
                semaforo.OnInterfazAbierta += OnInterfazSemaforoAbierta;
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

    private void OnInterfazSemaforoAbierta(bool abierta)
    {
        // Bloquear/desbloquear movimiento del jugador cuando se abre/cierra la interfaz
        BloquearMovimientoJugador(abierta);
    }

    private void BloquearMovimientoJugador(bool bloquear)
    {
        if (bloquear)
        {
            // Guardar velocidad actual antes de bloquear
            if (rbJugador != null)
            {
                velocidadAntesDeBloquear = rbJugador.linearVelocity;
            }

            // Frenado BRUSCO: detener inmediatamente el movimiento físico
            if (rbJugador != null)
            {
                rbJugador.linearVelocity = Vector2.zero;
                rbJugador.angularVelocity = 0f;
            }
        }
        else
        {
            // Al desbloquear, restaurar la velocidad anterior (opcional)
            // Si quieres que continúe con la misma velocidad, descomenta:
            // if (rbJugador != null)
            // {
            //     rbJugador.velocity = velocidadAntesDeBloquear;
            // }
        }

        // Bloquear/desbloquear scripts de movimiento
        if (scriptMovimientoJugador != null)
        {
            scriptMovimientoJugador.enabled = !bloquear;
        }

        // Bloquear/desbloquear script de input si está asignado
        if (scriptInputJugador != null)
        {
            scriptInputJugador.enabled = !bloquear;
        }

        Debug.Log($"Movimiento del jugador {(bloquear ? "BLOQUEADO (frenado brusco)" : "DESBLOQUEADO")}");
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

        // Asegurarse de que el movimiento esté desbloqueado
        BloquearMovimientoJugador(false);

        // Desactivar todos los semáforos
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

        // Mostrar mensaje de completado (nuevo flujo)
        StartCoroutine(MostrarMensajeYCompletar());
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

    private void MostrarMensajeCompletado()
    {
        if (!mostrarMensajeCompletado)
        {
            Debug.Log("Mostrar mensaje completado está desactivado");
            return;
        }

        if (panelDialogoCompletado == null)
        {
            Debug.LogError("Panel de diálogo completado no asignado en el inspector");
            return;
        }

        if (textoDialogoCompletado == null)
        {
            Debug.LogError("Texto de diálogo completado no asignado en el inspector");
            return;
        }

        Debug.Log("Mostrando mensaje de completado: " + mensajeCompletado);

        // Configurar el texto
        textoDialogoCompletado.text = mensajeCompletado;

        // Activar el panel
        panelDialogoCompletado.SetActive(true);

        // Ocultar el mensaje después del tiempo configurado
        Invoke("OcultarMensajeCompletado", tiempoMostrarMensaje);
    }

    private void OcultarMensajeCompletado()
    {
        if (panelDialogoCompletado != null)
        {
            panelDialogoCompletado.SetActive(false);
            Debug.Log("Mensaje de completado ocultado");
        }
    }

    private void GestionarObjetos()
    {
        // Activar objetos
        foreach (GameObject obj in objetosParaActivar)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                Debug.Log($"Objeto activado: {obj.name}");
            }
        }

        // Desactivar objetos
        foreach (GameObject obj in objetosParaDesactivar)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Debug.Log($"Objeto desactivado: {obj.name}");
            }
        }

        // Destruir objetos
        foreach (GameObject obj in objetosParaDestruir)
        {
            if (obj != null)
            {
                Destroy(obj);
                Debug.Log($"Objeto destruido: {obj.name}");
            }
        }
    }

    private IEnumerator MostrarMensajeYCompletar()
    {
        // Mostrar panel de mensaje
        if (mostrarMensajeCompletado && panelDialogoCompletado != null)
        {
            textoDialogoCompletado.text = mensajeCompletado;
            panelDialogoCompletado.SetActive(true);
            Debug.Log("Mostrando mensaje de completado: " + mensajeCompletado);
        }

        // Esperar el tiempo configurado
        yield return new WaitForSeconds(tiempoMostrarMensaje);

        // Ocultar el mensaje
        if (panelDialogoCompletado != null)
        {
            panelDialogoCompletado.SetActive(false);
            Debug.Log("Mensaje de completado ocultado");
        }

        // Ahora sí, gestionar los objetos (activar, desactivar o destruir)
        GestionarObjetos();

        // Limpiar eventos (evitar fugas de memoria)
        foreach (var semaforo in semaforos)
        {
            if (semaforo != null)
            {
                semaforo.OnEstadoCambiado -= OnSemaforoCambiado;
                semaforo.OnInterfazAbierta -= OnInterfazSemaforoAbierta;
            }
        }
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
                semaforo.OnInterfazAbierta -= OnInterfazSemaforoAbierta;
            }
        }
    }
}