using UnityEngine;
using UnityEngine.Events;

public class PuzzleManager : MonoBehaviour
{
    [Header("Tuberías del Puzzle")]
    public PipeController[] todasLasTuberias;

    [Header("Eventos")]
    public UnityEvent OnPuzzleCompletado;
    public UnityEvent OnPuzzleReset;

    [Header("Configuración")]
    public bool iniciarAlAwake = true;

    [Header("Referencias UI")]
    public GameObject panelPuzzle;
    public Collider2D colliderPuzzle;

    [Header("Referencias Jugador")]
    public MonoBehaviour controladorJugador;

    [Header("GameObjects para Controlar después del Puzzle")]
    public GameObject[] gameObjectsAActivar; // GameObjects que se activarán al completar
    public GameObject[] gameObjectsADesactivar; // GameObjects que se desactivarán al completar

    private bool puzzleCompletado = false;

    void Awake()
    {
        if (iniciarAlAwake)
            InicializarPuzzle();
    }

    void Update()
    {
        VerificarPuzzleCompleto();
    }

    public void InicializarPuzzle()
    {
        Debug.Log("Puzzle inicializado");

        if (panelPuzzle != null)
            panelPuzzle.SetActive(true);

        ActivarCollider(true);
        puzzleCompletado = false;
        DesactivarMovimientoJugador(true);
    }

    void VerificarPuzzleCompleto()
    {
        if (puzzleCompletado)
            return;

        if (todasLasTuberias == null || todasLasTuberias.Length == 0)
            return;

        bool todasCorrectas = true;

        foreach (var tuberia in todasLasTuberias)
        {
            if (tuberia != null && !tuberia.EstaCorrecta())
            {
                todasCorrectas = false;
                break;
            }
        }

        if (todasCorrectas)
        {
            CompletarPuzzle();
        }
    }

    void CompletarPuzzle()
    {
        if (puzzleCompletado) return;

        puzzleCompletado = true;
        Debug.Log("¡PUZZLE COMPLETADO!");

        MensajeCompletado();
        ActivarCollider(false);

        if (panelPuzzle != null)
        {
            panelPuzzle.SetActive(false);
            DesactivarMovimientoJugador(false);
        }

        // LLAMAR A LA FUNCIÓN PARA CONTROLAR LOS GAMEOBJECTS
        ControlarGameObjectsAlCompletar();

        OnPuzzleCompletado?.Invoke();
        enabled = false;
    }

    // FUNCIÓN PARA ACTIVAR/DESACTIVAR GAMEOBJECTS DESPUÉS DEL PUZZLE
    void ControlarGameObjectsAlCompletar()
    {
        Debug.Log("=== CONTROlando GAMEOBJECTS después del puzzle ===");

        // Activar los GameObjects en el array
        if (gameObjectsAActivar != null)
        {
            foreach (GameObject obj in gameObjectsAActivar)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"GameObject ACTIVADO: {obj.name}");
                }
                else
                {
                    Debug.LogWarning("Hay un GameObject nulo en gameObjectsAActivar");
                }
            }
        }

        // Desactivar los GameObjects en el array
        if (gameObjectsADesactivar != null)
        {
            foreach (GameObject obj in gameObjectsADesactivar)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    Debug.Log($"GameObject DESACTIVADO: {obj.name}");
                }
                else
                {
                    Debug.LogWarning("Hay un GameObject nulo en gameObjectsADesactivar");
                }
            }
        }

        Debug.Log("=== Control de GameObjects completado ===");
    }

    // FUNCIÓN PÚBLICA PARA LLAMAR DESDE OTROS SCRIPTS
    public void ControlarGameObjectsPersonalizado(bool activar, GameObject[] objetos)
    {
        if (objetos != null)
        {
            foreach (GameObject obj in objetos)
            {
                if (obj != null)
                {
                    obj.SetActive(activar);
                    Debug.Log($"GameObject {(activar ? "ACTIVADO" : "DESACTIVADO")}: {obj.name}");
                }
            }
        }
    }

    // FUNCIÓN PARA ACTIVAR UN SOLO GAMEOBJECT
    public void ActivarGameObject(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(true);
            Debug.Log($"GameObject activado: {obj.name}");
        }
    }

    // FUNCIÓN PARA DESACTIVAR UN SOLO GAMEOBJECT
    public void DesactivarGameObject(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(false);
            Debug.Log($"GameObject desactivado: {obj.name}");
        }
    }

    // FUNCIÓN PARA ALTERNAR (ACTIVAR/DESACTIVAR) UN GAMEOBJECT
    public void AlternarGameObject(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(!obj.activeSelf);
            Debug.Log($"GameObject alternado - {obj.name} ahora está {(obj.activeSelf ? "ACTIVADO" : "DESACTIVADO")}");
        }
    }

    void MensajeCompletado()
    {
        Debug.Log("✅ Puzzle completado con éxito!");
    }

    void ActivarCollider(bool activar)
    {
        if (colliderPuzzle != null)
        {
            colliderPuzzle.enabled = activar;
            Debug.Log($"Collider {(activar ? "ACTIVADO" : "DESACTIVADO")}");
        }
    }

    void DesactivarMovimientoJugador(bool desactivar)
    {
        if (controladorJugador != null)
        {
            controladorJugador.enabled = !desactivar;
            Debug.Log($"Movimiento del jugador {(desactivar ? "DESACTIVADO" : "ACTIVADO")}");
        }
    }

    public void ResetearPuzzle()
    {
        Debug.Log("Reseteando puzzle...");
        puzzleCompletado = false;
        OnPuzzleReset?.Invoke();
        ActivarCollider(true);
        enabled = true;
        DesactivarMovimientoJugador(true);
    }
}