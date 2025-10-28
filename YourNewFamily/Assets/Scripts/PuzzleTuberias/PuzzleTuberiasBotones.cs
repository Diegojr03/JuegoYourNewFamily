using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections;

public class PuzzleTuberiasBotones : MonoBehaviour
{
    [System.Serializable]
    public class TuberiaConfig
    {
        public Button botonTuberia;
        public Image imagenTuberia;
        public DireccionTuberia direccionActual;
        public DireccionTuberia direccionCorrecta;

        [Header("Sprites para cada dirección")]
        public Sprite spriteArriba;
        public Sprite spriteAbajo;
        public Sprite spriteIzquierda;
        public Sprite spriteDerecha;
    }

    public enum DireccionTuberia { Arriba, Abajo, Izquierda, Derecha }

    [Header("CONFIGURACIÓN TUBERÍAS")]
    public TuberiaConfig[] tuberias = new TuberiaConfig[5];

    [Header("INTERFAZ Y INTERACCIÓN")]
    public GameObject interfazTuberias;
    public float distanciaInteraccion = 2f;
    public KeyCode teclaInteraccion = KeyCode.E;
    public GameObject textoInteraccion;
    public Sprite spriteTeclaE;

    [Header("CONFIGURACIÓN TECLA E")]
    public Vector3 posicionTeclaE = new Vector3(0, 1.5f, 0);
    public Vector3 escalaTeclaE = new Vector3(0.25f, 0.25f, 0.25f);
    public float velocidadAnimacion = 3f;
    public float amplitudAnimacion = 0.1f;

    [Header("CONFIGURACIÓN JUGADOR")]
    public MonoBehaviour scriptMovimientoJugador;
    private Rigidbody2D rbJugador;
    private Vector2 velocidadAntesDeBloquear;

    [Header("REFERENCIAS")]
    public Camera camaraJugador;

    [Header("CONFIGURACIÓN COMPLETADO")]
    public float tiempoCongeladoAlCompletar = 1.5f;
    public bool activarDialogoAlCompletar = true;
    public AudioClip sonidoCompletado;

    [Header("DIÁLOGO AL COMPLETAR")]
    public GameObject panelDialogo;
    public TextMeshProUGUI textoDialogo;
    public string[] lineasDialogo;
    public float velocidadTexto = 0.05f;
    public float tiempoAutoAvance = 2f;
    public GameObject[] objetosParaActivar;
    public bool destruirDespuesDeDialogo = true;
    public GameObject speakerContainer;
    public TextMeshProUGUI speakerText;

    private bool dialogoActivo = false;
    private int lineaActual = 0;
    private Coroutine corrutinaEscritura;

    // Eventos
    public event Action<PuzzleTuberiasBotones> OnEstadoCambiado;
    public event Action<bool> OnInterfazAbierta;

    private bool estaMirando = false;
    private bool interfazAbierta = false;
    private bool puzzleCompletado = false;
    private GameObject jugador;
    private SpriteRenderer spriteTeclaERenderer;
    private GameObject teclaEObj;

    private void Start()
    {
        InicializarTuberias();
        OcultarInterfaz();

        if (camaraJugador == null)
            camaraJugador = Camera.main;

        // Buscar jugador automáticamente
        jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            rbJugador = jugador.GetComponent<Rigidbody2D>();
            if (scriptMovimientoJugador == null)
            {
                scriptMovimientoJugador = jugador.GetComponent<MonoBehaviour>();
            }
        }

        if (textoInteraccion != null)
            textoInteraccion.SetActive(false);

        // Configurar collider como trigger
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // Crear el sistema de tecla E
        CrearSistemaTeclaE();
    }

    private void CrearSistemaTeclaE()
    {
        if (spriteTeclaE != null)
        {
            teclaEObj = new GameObject("TeclaE_Indicator");
            teclaEObj.transform.SetParent(transform);
            teclaEObj.transform.localPosition = posicionTeclaE;
            teclaEObj.transform.localScale = escalaTeclaE;

            spriteTeclaERenderer = teclaEObj.AddComponent<SpriteRenderer>();
            spriteTeclaERenderer.sprite = spriteTeclaE;
            spriteTeclaERenderer.sortingOrder = 10;
            spriteTeclaERenderer.enabled = false;

            Debug.Log($"Sistema Tecla E creado para {name}");
        }
    }

    private void Update()
    {
        if (!interfazAbierta && !puzzleCompletado)
        {
            VerificarProximidadJugador();
        }

        ManejarInputInteraccion();

        // Animación tecla E
        if (estaMirando && !interfazAbierta && !puzzleCompletado && spriteTeclaERenderer != null && spriteTeclaERenderer.enabled)
        {
            float offsetY = Mathf.Sin(Time.time * velocidadAnimacion) * amplitudAnimacion;
            Vector3 nuevaPosicion = posicionTeclaE + new Vector3(0, offsetY, 0);
            if (teclaEObj != null)
            {
                teclaEObj.transform.localPosition = nuevaPosicion;
            }
        }

        // Verificar puzzle completado
        if (interfazAbierta && VerificarPuzzleCompletado())
        {
            PuzzleCompletado();
        }

        // AÑADE ESTA LÍNEA:
        if (dialogoActivo) UpdateDialogo();
    }

    private void InicializarTuberias()
    {
        // Configurar botones y sus eventos
        for (int i = 0; i < tuberias.Length; i++)
        {
            if (tuberias[i].botonTuberia != null)
            {
                int index = i; // Importante para closure
                tuberias[i].botonTuberia.onClick.AddListener(() => RotarTuberia(index));
                ActualizarSpriteTuberia(i);
            }
        }
    }

    private void VerificarProximidadJugador()
    {
        if (jugador == null || puzzleCompletado) return;

        float distancia = Vector2.Distance(jugador.transform.position, transform.position);
        bool nuevoEstadoMirando = (distancia <= distanciaInteraccion);

        if (nuevoEstadoMirando != estaMirando)
        {
            estaMirando = nuevoEstadoMirando;
            MostrarTeclaE(estaMirando);
        }
    }

    private void MostrarTeclaE(bool mostrar)
    {
        if (spriteTeclaERenderer != null)
        {
            spriteTeclaERenderer.enabled = mostrar && !puzzleCompletado;
        }

        if (textoInteraccion != null)
        {
            textoInteraccion.SetActive(mostrar && !puzzleCompletado);
        }
    }

    private void ManejarInputInteraccion()
    {
        if (puzzleCompletado) return;

        if (estaMirando && Input.GetKeyDown(teclaInteraccion))
        {
            if (!interfazAbierta)
            {
                AbrirInterfaz();
            }
            else
            {
                CerrarInterfaz();
            }
        }

        if (interfazAbierta && Input.GetKeyDown(KeyCode.Escape))
        {
            CerrarInterfaz();
        }
    }

    private void BloquearMovimientoJugador(bool bloquear)
    {
        if (bloquear)
        {
            if (rbJugador != null)
            {
                velocidadAntesDeBloquear = rbJugador.linearVelocity;
                rbJugador.linearVelocity = Vector2.zero;
                rbJugador.angularVelocity = 0f;
            }
        }
        else
        {
            if (rbJugador != null)
            {
                rbJugador.linearVelocity = velocidadAntesDeBloquear;
            }
        }

        if (scriptMovimientoJugador != null)
        {
            scriptMovimientoJugador.enabled = !bloquear;
        }

        Debug.Log($"Movimiento del jugador {(bloquear ? "BLOQUEADO" : "DESBLOQUEADO")}");
    }

    public void AbrirInterfaz()
    {
        if (puzzleCompletado) return;

        interfazAbierta = true;
        MostrarInterfaz();
        MostrarTeclaE(false);
        BloquearMovimientoJugador(true);
        OnInterfazAbierta?.Invoke(true);

        Debug.Log($"Interfaz ABIERTA: {gameObject.name}");
    }

    public void CerrarInterfaz()
    {
        interfazAbierta = false;
        OcultarInterfaz();
        BloquearMovimientoJugador(false);
        OnInterfazAbierta?.Invoke(false);

        if (estaMirando && !puzzleCompletado)
        {
            MostrarTeclaE(true);
        }

        Debug.Log($"Interfaz CERRADA: {gameObject.name}");
    }

    private void MostrarInterfaz()
    {
        if (interfazTuberias != null)
        {
            interfazTuberias.SetActive(true);
        }
    }

    private void OcultarInterfaz()
    {
        if (interfazTuberias != null)
            interfazTuberias.SetActive(false);
    }

    private void RotarTuberia(int index)
    {
        if (index < 0 || index >= tuberias.Length) return;

        switch (tuberias[index].direccionActual)
        {
            case DireccionTuberia.Arriba:
                tuberias[index].direccionActual = DireccionTuberia.Derecha;
                break;
            case DireccionTuberia.Derecha:
                tuberias[index].direccionActual = DireccionTuberia.Abajo;
                break;
            case DireccionTuberia.Abajo:
                tuberias[index].direccionActual = DireccionTuberia.Izquierda;
                break;
            case DireccionTuberia.Izquierda:
                tuberias[index].direccionActual = DireccionTuberia.Arriba;
                break;
        }

        ActualizarSpriteTuberia(index);
        OnEstadoCambiado?.Invoke(this);
        Debug.Log($"Tubería {index} rotada hacia: {tuberias[index].direccionActual}");
    }

    private void ActualizarSpriteTuberia(int index)
    {
        if (tuberias[index].imagenTuberia != null)
        {
            switch (tuberias[index].direccionActual)
            {
                case DireccionTuberia.Arriba:
                    tuberias[index].imagenTuberia.sprite = tuberias[index].spriteArriba;
                    break;
                case DireccionTuberia.Abajo:
                    tuberias[index].imagenTuberia.sprite = tuberias[index].spriteAbajo;
                    break;
                case DireccionTuberia.Izquierda:
                    tuberias[index].imagenTuberia.sprite = tuberias[index].spriteIzquierda;
                    break;
                case DireccionTuberia.Derecha:
                    tuberias[index].imagenTuberia.sprite = tuberias[index].spriteDerecha;
                    break;
            }
        }
    }

    private bool VerificarPuzzleCompletado()
    {
        if (puzzleCompletado) return false; // No verificar si ya está completado

        for (int i = 0; i < tuberias.Length; i++)
        {
            if (tuberias[i].direccionActual != tuberias[i].direccionCorrecta)
            {
                return false;
            }
        }
        return true;
    }

    private void IniciarDialogo()
    {
        if (!activarDialogoAlCompletar || lineasDialogo.Length == 0) return;

        speakerContainer.SetActive(false);
        speakerText.text = "";
        dialogoActivo = true;
        lineaActual = 0;

        if (panelDialogo != null)
        {
            panelDialogo.SetActive(true);
        }

        MostrarLineaDialogo(lineaActual);
    }

    private void MostrarLineaDialogo(int index)
    {
        if (index >= lineasDialogo.Length) return;

        if (corrutinaEscritura != null)
        {
            StopCoroutine(corrutinaEscritura);
        }
        corrutinaEscritura = StartCoroutine(EscribirTexto(lineasDialogo[index]));
    }

    private IEnumerator EscribirTexto(string texto)
    {
        textoDialogo.text = "";

        foreach (char letra in texto.ToCharArray())
        {
            textoDialogo.text += letra;
            yield return new WaitForSeconds(velocidadTexto);
        }

        corrutinaEscritura = null;
        StartCoroutine(AutoAvanzarDialogo());
    }

    private IEnumerator AutoAvanzarDialogo()
    {
        yield return new WaitForSeconds(tiempoAutoAvance);
        AvanzarDialogo();
    }

    private void AvanzarDialogo()
    {
        lineaActual++;

        if (lineaActual < lineasDialogo.Length)
        {
            MostrarLineaDialogo(lineaActual);
        }
        else
        {
            FinalizarDialogo();
        }
    }

    private void FinalizarDialogo()
    {
        dialogoActivo = false;

        if (panelDialogo != null)
        {
            panelDialogo.SetActive(false);
        }

        // REPRODUCIR SONIDO ANTES DE ACTIVAR OBJETOS
        if (sonidoCompletado != null)
        {
            AudioSource.PlayClipAtPoint(sonidoCompletado, transform.position);
        }

        // Pequeña pausa para que suene el sonido antes de activar objetos
        StartCoroutine(CompletarDespuesDeSonido());
    }

    private IEnumerator CompletarDespuesDeSonido()
    {
        // Esperar un frame para que el sonido empiece
        yield return null;

        // Activar objetos
        foreach (GameObject obj in objetosParaActivar)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        // Destruir si está configurado
        if (destruirDespuesDeDialogo)
        {
            Destroy(gameObject);
        }
    }

    // Para permitir skip con Space
    private void UpdateDialogo()
    {
        if (dialogoActivo && Input.GetKeyDown(KeyCode.Space))
        {
            if (corrutinaEscritura != null)
            {
                StopCoroutine(corrutinaEscritura);
                textoDialogo.text = lineasDialogo[lineaActual];
                corrutinaEscritura = null;
                StartCoroutine(AutoAvanzarDialogo());
            }
            else
            {
                AvanzarDialogo();
            }
        }
    }

    private void PuzzleCompletado()
    {
        puzzleCompletado = true;
        Debug.Log("¡Puzzle de tuberías completado!");

        // Deshabilitar todos los botones para que no se puedan pulsar
        foreach (TuberiaConfig tuberia in tuberias)
        {
            if (tuberia.botonTuberia != null)
                tuberia.botonTuberia.interactable = false;
        }

        // Iniciar la pausa antes de cerrar la interfaz
        StartCoroutine(PausaAntesDeCerrar());
    }

    private IEnumerator PausaAntesDeCerrar()
    {
        // Esperar el tiempo configurado en el inspector
        yield return new WaitForSeconds(tiempoCongeladoAlCompletar);

        // Ahora cerrar la interfaz y continuar con el flujo normal
        CerrarInterfaz();

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        MostrarTeclaE(false);
        if (teclaEObj != null) Destroy(teclaEObj);

        if (activarDialogoAlCompletar && lineasDialogo.Length > 0)
        {
            IniciarDialogo();
        }
        else
        {
            // AÑADE EL SONIDO TAMBIÉN AQUÍ
            if (sonidoCompletado != null)
            {
                AudioSource.PlayClipAtPoint(sonidoCompletado, transform.position);
            }

            // Activar objetos directamente
            foreach (GameObject obj in objetosParaActivar)
            {
                if (obj != null) obj.SetActive(true);
            }
            if (destruirDespuesDeDialogo)
            {
                // Pequeña pausa antes de destruir
                StartCoroutine(DestruirDespuesDeSonido());
            }
        }
    }

    private IEnumerator DestruirDespuesDeSonido()
    {
        yield return new WaitForSeconds(0.1f); // Pausa mínima para que suene el sonido
        Destroy(gameObject);
    }

    // Métodos públicos
    public bool IsInterfazAbierta() => interfazAbierta;
    public bool IsPuzzleCompletado() => puzzleCompletado;

    [ContextMenu("Debug Estado")]
    public void DebugEstado()
    {
        Debug.Log($"=== DEBUG {name} ===");
        for (int i = 0; i < tuberias.Length; i++)
        {
            Debug.Log($"Tubería {i} - Actual: {tuberias[i].direccionActual}, Correcta: {tuberias[i].direccionCorrecta}");
        }
    }

    // Triggers
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (puzzleCompletado) return;
        if (other.CompareTag("Player"))
        {
            jugador = other.gameObject;
            estaMirando = true;
            MostrarTeclaE(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            estaMirando = false;
            MostrarTeclaE(false);
            if (interfazAbierta) CerrarInterfaz();
        }
    }

    private void OnDestroy()
    {
        if (teclaEObj != null) Destroy(teclaEObj);
    }
}