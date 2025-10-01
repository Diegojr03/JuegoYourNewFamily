using UnityEngine;
using System.Collections.Generic;

public class ManagerPuzleEstatuas : MonoBehaviour
{
    [Header("Configuración del Puzle")]
    public Estatua[] estatuas; // Asignar las 4 estatuas en el inspector
    public Estatua.Direccion[] solucionCorrecta; // La combinación ganadora

    [Header("Referencias Puerta")]
    public SpriteRenderer puertaSpriteRenderer;
    public Sprite puertaCerrada;
    public Sprite puertaAbierta;
    public Collider2D colliderPuerta; // Collider para bloquear el paso

    [Header("Efectos Visuales")]
    public ParticleSystem particulasExito;
    public AudioSource audioSource;
    public AudioClip sonidoApertura;

    private bool puzleResuelto = false;

    void Start()
    {
        // Validar que tenemos 4 estatuas
        if (estatuas.Length != 4)
        {
            Debug.LogError("Debe haber exactamente 4 estatuas en el puzle");
        }

        // Validar que tenemos 4 direcciones en la solución
        if (solucionCorrecta.Length != 4)
        {
            Debug.LogError("Debe haber exactamente 4 direcciones en la solución");
        }

        // Configurar las estatuas
        for (int i = 0; i < estatuas.Length; i++)
        {
            estatuas[i].estatuaID = i;
            estatuas[i].OnDireccionCambiada += OnEstatuaRotada;
        }

        // Configurar la puerta inicialmente cerrada
        if (puertaSpriteRenderer != null && puertaCerrada != null)
        {
            puertaSpriteRenderer.sprite = puertaCerrada;
        }

        if (colliderPuerta != null)
        {
            colliderPuerta.enabled = true;
        }

        // Verificar estado inicial
        VerificarSolucion();
    }

    private void OnEstatuaRotada(Estatua estatua)
    {
        Debug.Log($"Estatua {estatua.estatuaID} rotada. Verificando solución...");
        VerificarSolucion();
    }

    private void VerificarSolucion()
    {
        if (puzleResuelto) return;

        bool solucionCorrectaEncontrada = true;

        // Verificar que cada estatua tenga la dirección correcta
        for (int i = 0; i < estatuas.Length; i++)
        {
            if (estatuas[i].DireccionActual != solucionCorrecta[i])
            {
                solucionCorrectaEncontrada = false;
                break;
            }
        }

        // Verificar que todas las direcciones sean diferentes
        if (solucionCorrectaEncontrada)
        {
            HashSet<Estatua.Direccion> direcciones = new HashSet<Estatua.Direccion>();
            foreach (var estatua in estatuas)
            {
                direcciones.Add(estatua.DireccionActual);
            }

            if (direcciones.Count != 4)
            {
                solucionCorrectaEncontrada = false;
                Debug.Log("Todas las estatuas deben apuntar a direcciones diferentes");
            }
        }

        if (solucionCorrectaEncontrada)
        {
            ResolverPuzle();
        }
    }

    private void ResolverPuzle()
    {
        puzleResuelto = true;

        // Abrir puerta
        if (puertaSpriteRenderer != null && puertaAbierta != null)
        {
            puertaSpriteRenderer.sprite = puertaAbierta;
        }

        if (colliderPuerta != null)
        {
            colliderPuerta.enabled = false;
        }

        // Efectos de sonido
        if (audioSource != null && sonidoApertura != null)
        {
            audioSource.PlayOneShot(sonidoApertura);
        }

        // Partículas
        if (particulasExito != null)
        {
            particulasExito.Play();
        }

        Debug.Log("¡Puzle resuelto! La puerta se abre.");
    }

    // Métodos para debugging y control
    public void MostrarEstadoEstatuas()
    {
        string estado = "Estado de las estatuas:\n";
        for (int i = 0; i < estatuas.Length; i++)
        {
            estado += $"Estatua {i}: {estatuas[i].DireccionActual}\n";
        }
        Debug.Log(estado);
    }

    public void ReiniciarPuzle()
    {
        puzleResuelto = false;

        // Cerrar puerta
        if (puertaSpriteRenderer != null && puertaCerrada != null)
        {
            puertaSpriteRenderer.sprite = puertaCerrada;
        }

        if (colliderPuerta != null)
        {
            colliderPuerta.enabled = true;
        }

        // Reiniciar estatuas
        for (int i = 0; i < estatuas.Length; i++)
        {
            estatuas[i].SetDireccion(estatuas[i].direccionInicial);
        }

        Debug.Log("Puzle reiniciado");
    }

    // Método para verificar si el puzle está resuelto
    public bool IsPuzleResuelto()
    {
        return puzleResuelto;
    }
}