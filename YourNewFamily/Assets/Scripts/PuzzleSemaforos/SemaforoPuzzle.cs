using UnityEngine;
using UnityEngine.UI;
using System;

public class SemaforoPuzzle : MonoBehaviour
{
    [System.Serializable]
    public class BotonSemaforo
    {
        public Button boton;
        public Image imagen;
        public Sprite spriteApagado;
        public Sprite spriteEncendido;
        [HideInInspector] public bool estaActivo = false;
    }

    [Header("Configuración Botones")]
    public BotonSemaforo botonArriba;
    public BotonSemaforo botonMedio;
    public BotonSemaforo botonAbajo;

    [Header("Interfaz y Interacción")]
    public GameObject interfazSemaforo;
    public float distanciaInteraccion = 3f;
    public KeyCode teclaInteraccion = KeyCode.E;
    public GameObject textoInteraccion;

    [Header("Referencias")]
    public Camera camaraJugador;

    // Eventos
    public event Action<SemaforoPuzzle> OnEstadoCambiado;

    private bool estaMirando = false;
    private bool interfazAbierta = false;

    private void Start()
    {
        ConfigurarBotones();
        OcultarInterfaz();

        if (camaraJugador == null)
            camaraJugador = Camera.main;

        if (textoInteraccion != null)
            textoInteraccion.SetActive(false);
    }

    private void Update()
    {
        VerificarMirada();
        ManejarInputInteraccion();
    }

    private void ConfigurarBotones()
    {
        botonArriba.boton.onClick.AddListener(() => ToggleBoton(botonArriba));
        botonMedio.boton.onClick.AddListener(() => ToggleBoton(botonMedio));
        botonAbajo.boton.onClick.AddListener(() => ToggleBoton(botonAbajo));

        ActualizarSprites();
    }

    private void ToggleBoton(BotonSemaforo boton)
    {
        boton.estaActivo = !boton.estaActivo;
        ActualizarSprites();
        OnEstadoCambiado?.Invoke(this);
    }

    private void ActualizarSprites()
    {
        botonArriba.imagen.sprite = botonArriba.estaActivo ? botonArriba.spriteEncendido : botonArriba.spriteApagado;
        botonMedio.imagen.sprite = botonMedio.estaActivo ? botonMedio.spriteEncendido : botonMedio.spriteApagado;
        botonAbajo.imagen.sprite = botonAbajo.estaActivo ? botonAbajo.spriteEncendido : botonAbajo.spriteApagado;
    }

    private void VerificarMirada()
    {
        if (camaraJugador == null) return;

        Ray ray = new Ray(camaraJugador.transform.position, camaraJugador.transform.forward);
        RaycastHit hit;

        bool nuevoEstadoMirando = false;

        if (Physics.Raycast(ray, out hit, distanciaInteraccion))
        {
            if (hit.collider.gameObject == this.gameObject)
            {
                nuevoEstadoMirando = true;
            }
        }

        if (nuevoEstadoMirando != estaMirando)
        {
            estaMirando = nuevoEstadoMirando;
            MostrarTextoInteraccion(estaMirando && !interfazAbierta);
        }
    }

    private void ManejarInputInteraccion()
    {
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
    }

    public void AbrirInterfaz()
    {
        interfazAbierta = true;
        MostrarInterfaz();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CerrarInterfaz()
    {
        interfazAbierta = false;
        OcultarInterfaz();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void MostrarInterfaz()
    {
        if (interfazSemaforo != null)
            interfazSemaforo.SetActive(true);
    }

    private void OcultarInterfaz()
    {
        if (interfazSemaforo != null)
            interfazSemaforo.SetActive(false);

        if (textoInteraccion != null)
            textoInteraccion.SetActive(false);
    }

    private void MostrarTextoInteraccion(bool mostrar)
    {
        if (textoInteraccion != null)
            textoInteraccion.SetActive(mostrar);
    }

    // Métodos públicos para acceder al estado
    public bool GetArribaActivo() => botonArriba.estaActivo;
    public bool GetMedioActivo() => botonMedio.estaActivo;
    public bool GetAbajoActivo() => botonAbajo.estaActivo;

    public bool IsInterfazAbierta() => interfazAbierta;
}
