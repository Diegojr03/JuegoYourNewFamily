using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrulleroPillaPilla : MonoBehaviour
{
    [Header("Puntos de Ruta")]
    [SerializeField] private Transform[] puntosRuta; // Array de puntos en el inspector

    [Header("Configuración de Movimiento")]
    [SerializeField] private float velocidadMovimiento = 5f; // Velocidad del objeto
    [SerializeField] private int primerosPuntos = 3; // Cuántos primeros puntos recorrer en orden

    [Header("Objetos a Activar/Desactivar")]
    [SerializeField] private GameObject[] objetosAActivar; // Objetos a activar al ser pillado
    [SerializeField] private GameObject[] objetosADesactivar; // Objetos a desactivar al ser pillado

    private int puntoActual = 0;
    private bool moviendoHaciaAdelante = true;
    private bool haSidoPillado = false;
    private bool enMovimiento = true;
    private bool enMovimientoForzado = false; // Para saber si está en movimiento forzado de 2 puntos
    private int puntosRestantesForzados = 0; // Cuántos puntos le quedan por recorrer en el movimiento forzado
    private int direccionForzada = 0; // 1 = hacia adelante, -1 = hacia atrás

    void Start()
    {
        // Validar que hay puntos en el array
        if (puntosRuta == null || puntosRuta.Length == 0)
        {
            Debug.LogError("No hay puntos de ruta asignados al patrullero");
            enMovimiento = false;
            return;
        }

        // Asegurarse de que no se pidan más primeros puntos de los que existen
        if (primerosPuntos > puntosRuta.Length)
        {
            primerosPuntos = puntosRuta.Length;
            Debug.LogWarning("primerosPuntos excede la cantidad de puntos, se ajustó a: " + primerosPuntos);
        }

        // Iniciar en el primer punto
        puntoActual = 0;
        transform.position = puntosRuta[0].position;

        // Comenzar el movimiento
        StartCoroutine(MoverEntrePuntos());
    }

    IEnumerator MoverEntrePuntos()
    {
        while (enMovimiento && !haSidoPillado)
        {
            int siguientePunto;

            // Si estamos en movimiento forzado, continuar en la misma dirección
            if (enMovimientoForzado)
            {
                siguientePunto = puntoActual + direccionForzada;
                puntosRestantesForzados--;

                // Si ya terminó el movimiento forzado
                if (puntosRestantesForzados <= 0)
                {
                    enMovimientoForzado = false;
                }
            }
            else
            {
                // Determinar el siguiente punto según la fase normal
                siguientePunto = ObtenerSiguientePunto();

                // Verificar si debemos iniciar un movimiento forzado
                // Esto ocurre cuando se sale de la fase inicial y se cambia de dirección
                if (!moviendoHaciaAdelante && siguientePunto != puntoActual)
                {
                    // Detectar si vamos a cambiar de dirección respecto al movimiento anterior
                    int nuevaDireccion = (siguientePunto > puntoActual) ? 1 : -1;

                    // Si hay un cambio de dirección (no es continuación del mismo movimiento)
                    if (movimientoAnterior != 0 && nuevaDireccion != movimientoAnterior)
                    {
                        // Iniciar movimiento forzado de 2 puntos en la nueva dirección
                        enMovimientoForzado = true;
                        direccionForzada = nuevaDireccion;
                        puntosRestantesForzados = 2;

                        // El siguiente punto ya está calculado, continuamos
                    }

                    movimientoAnterior = nuevaDireccion;
                }
            }

            // Mover hacia el siguiente punto
            yield return StartCoroutine(MoverHaciaPunto(puntosRuta[siguientePunto].position));

            // Actualizar punto actual
            puntoActual = siguientePunto;

            // Pequeña pausa entre movimientos (opcional)
            yield return new WaitForSeconds(0.1f);
        }
    }

    private int movimientoAnterior = 0; // Para detectar cambios de dirección

    int ObtenerSiguientePunto()
    {
        // FASE 1: Recorrer los primeros X puntos en orden
        if (puntoActual < primerosPuntos - 1 && moviendoHaciaAdelante)
        {
            return puntoActual + 1;
        }

        // Si llegamos al final de los primeros puntos, cambiamos a fase aleatoria
        if (puntoActual == primerosPuntos - 1 && moviendoHaciaAdelante)
        {
            moviendoHaciaAdelante = false;
        }

        // FASE 2: Movimiento aleatorio entre punto anterior y siguiente
        if (!moviendoHaciaAdelante)
        {
            // Opciones disponibles: punto anterior o punto siguiente (si existen)
            List<int> opciones = new List<int>();

            // Punto anterior (si existe)
            if (puntoActual > 0)
                opciones.Add(puntoActual - 1);

            // Punto siguiente (si existe)
            if (puntoActual < puntosRuta.Length - 1)
                opciones.Add(puntoActual + 1);

            // Si hay opciones, elegir una aleatoria
            if (opciones.Count > 0)
            {
                int indiceAleatorio = Random.Range(0, opciones.Count);
                return opciones[indiceAleatorio];
            }

            // Si no hay opciones (solo hay 1 punto en total), mantener el mismo
            return puntoActual;
        }

        return puntoActual;
    }

    IEnumerator MoverHaciaPunto(Vector3 destino)
    {
        // Si ha sido pillado durante el movimiento, detener inmediatamente
        while (Vector3.Distance(transform.position, destino) > 0.01f && !haSidoPillado)
        {
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidadMovimiento * Time.deltaTime);
            yield return null;
        }

        // Solo ajustar la posición si no fue pillado
        if (!haSidoPillado)
        {
            transform.position = destino;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Verificar que es el personaje
        if (other.CompareTag("Player") && !haSidoPillado)
        {
            haSidoPillado = true;
            enMovimiento = false;
            enMovimientoForzado = false;

            // Detener cualquier corrutina en curso
            StopAllCoroutines();

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

            Debug.Log("¡Pillado! El patrullero se ha detenido en su posición actual: " + transform.position);
        }
    }

    // Método público para reiniciar el patrullero
    public void ReiniciarPatrullero()
    {
        haSidoPillado = false;
        enMovimiento = true;
        enMovimientoForzado = false;
        puntoActual = 0;
        moviendoHaciaAdelante = true;
        movimientoAnterior = 0;

        if (puntosRuta != null && puntosRuta.Length > 0)
        {
            transform.position = puntosRuta[0].position;
            StartCoroutine(MoverEntrePuntos());
        }
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