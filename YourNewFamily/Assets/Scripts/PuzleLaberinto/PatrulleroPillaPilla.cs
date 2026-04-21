using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrulleroPillaPilla : MonoBehaviour
{
    [Header("Puntos de Ruta")]
    [SerializeField] private Transform[] puntosRuta; // Array de puntos en el inspector

    [Header("Configuración de Movimiento")]
    [SerializeField] private float velocidadMovimiento = 5f; // Velocidad del objeto
    [SerializeField] private ComportamientoFinal comportamientoFinal; // Qué hacer al llegar al último punto

    [Header("Objetos a Activar/Desactivar al Completar/Ser Pillado")]
    [SerializeField] private GameObject[] objetosAActivar; // Objetos a activar al completar ruta o ser pillado
    [SerializeField] private GameObject[] objetosADesactivar; // Objetos a desactivar al completar ruta o ser pillado

    [Header("Colliders de Avance (Triggers)")]
    [SerializeField] private Collider2D[] collidersAvance; // Orden: collider0 avanza al punto1, collider1 avanza al punto2, etc.

    private int puntoActual = 0; // Índice del punto donde está actualmente
    private int siguientePuntoIndex = 1; // Índice del punto al que debe ir cuando se active el collider
    private bool enMovimiento = false; // Si se está moviendo entre puntos
    private bool haCompletadoRuta = false; // Si ya completó toda la ruta
    private bool haSidoPillado = false; // Si el personaje lo atrapó

    private Collider2D miCollider; // Collider del patrullero (para que el player pueda atraparlo)

    void Start()
    {
        // Validar que hay puntos en el array
        if (puntosRuta == null || puntosRuta.Length < 2)
        {
            Debug.LogError("El patrullero necesita al menos 2 puntos de ruta asignados");
            enabled = false;
            return;
        }

        // Obtener el collider del patrullero (para detección de atrapada)
        miCollider = GetComponent<Collider2D>();
        if (miCollider == null)
        {
            Debug.LogWarning("El patrullero no tiene un Collider2D para detectar cuando el player lo atrapa");
        }

        // Iniciar en el primer punto
        puntoActual = 0;
        transform.position = puntosRuta[0].position;
        siguientePuntoIndex = 1;

        // Validar que los colliders de avance coincidan con la cantidad de movimientos necesarios
        int movimientosNecesarios = puntosRuta.Length - 1;
        if (collidersAvance == null || collidersAvance.Length != movimientosNecesarios)
        {
            Debug.LogWarning($"Se esperaban {movimientosNecesarios} colliders de avance, pero hay {(collidersAvance == null ? 0 : collidersAvance.Length)}");
        }
    }

    void Update()
    {
        // Este método se puede usar para debug si es necesario
    }

    // Método público para que los colliders de avance llamen cuando el player entra
    public void AvanzarAlSiguientePunto()
    {
        // No hacer nada si ya completó la ruta, ya fue pillado, o ya se está moviendo
        if (haCompletadoRuta || haSidoPillado || enMovimiento)
            return;

        // Verificar que aún hay puntos por recorrer
        if (siguientePuntoIndex >= puntosRuta.Length)
            return;

        // Iniciar el movimiento hacia el siguiente punto
        StartCoroutine(MoverHaciaPunto(puntosRuta[siguientePuntoIndex].position, siguientePuntoIndex));
    }

    IEnumerator MoverHaciaPunto(Vector3 destino, int indiceDestino)
    {
        enMovimiento = true;

        // Moverse hacia el destino
        while (Vector3.Distance(transform.position, destino) > 0.01f && !haSidoPillado)
        {
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidadMovimiento * Time.deltaTime);
            yield return null;
        }

        // Si fue pillado durante el movimiento, detener todo
        if (haSidoPillado)
        {
            enMovimiento = false;
            yield break;
        }

        // Asegurar posición exacta
        transform.position = destino;

        // Actualizar punto actual
        puntoActual = indiceDestino;
        siguientePuntoIndex = indiceDestino + 1;

        enMovimiento = false;

        // Verificar si se completó la ruta (llegó al último punto)
        if (puntoActual == puntosRuta.Length - 1)
        {
            CompletarRuta();
        }
    }

    void CompletarRuta()
    {
        haCompletadoRuta = true;
        enMovimiento = false;

        // Activar/Desactivar objetos según configuración
        ActivarDesactivarObjetos();

        // Comportamiento según selección
        switch (comportamientoFinal)
        {
            case ComportamientoFinal.Destruirse:
                Debug.Log("Patrullero completó la ruta y se destruye");
                Destroy(gameObject);
                break;
            case ComportamientoFinal.QuedarseQuieto:
                Debug.Log("Patrullero completó la ruta y se queda quieto");
                // El collider sigue activo para que el player pueda atraparlo
                break;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Verificar que es el personaje y que no ha sido pillado ni ha completado ruta
        if (other.CompareTag("Player") && !haSidoPillado && !haCompletadoRuta)
        {
            // Si el personaje toca el collider del patrullero, es atrapado
            if (other == miCollider || other == GetComponent<Collider2D>())
            {
                SerPillado();
            }
        }
    }

    void SerPillado()
    {
        haSidoPillado = true;
        enMovimiento = false;
        StopAllCoroutines();

        ActivarDesactivarObjetos();

        Debug.Log("¡El patrullero ha sido pillado! Se detiene en: " + transform.position);
    }

    void ActivarDesactivarObjetos()
    {
        // Activar objetos especificados
        if (objetosAActivar != null)
        {
            foreach (GameObject obj in objetosAActivar)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }

        // Desactivar objetos especificados
        if (objetosADesactivar != null)
        {
            foreach (GameObject obj in objetosADesactivar)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
    }

    // Método público para reiniciar el patrullero (opcional)
    public void ReiniciarPatrullero()
    {
        if (puntosRuta == null || puntosRuta.Length < 2) return;

        haSidoPillado = false;
        haCompletadoRuta = false;
        enMovimiento = false;
        puntoActual = 0;
        siguientePuntoIndex = 1;
        transform.position = puntosRuta[0].position;

        StopAllCoroutines();
    }

    // Método para dibujar los puntos en el editor (visual)
    void OnDrawGizmosSelected()
    {
        if (puntosRuta == null) return;

        Gizmos.color = Color.yellow;
        foreach (Transform punto in puntosRuta)
        {
            if (punto != null)
                Gizmos.DrawWireSphere(punto.position, 0.3f);
        }

        // Dibujar líneas entre puntos
        Gizmos.color = Color.gray;
        for (int i = 0; i < puntosRuta.Length - 1; i++)
        {
            if (puntosRuta[i] != null && puntosRuta[i + 1] != null)
                Gizmos.DrawLine(puntosRuta[i].position, puntosRuta[i + 1].position);
        }
    }
}

// Enum para el comportamiento al final de la ruta
public enum ComportamientoFinal
{
    Destruirse,
    QuedarseQuieto
}