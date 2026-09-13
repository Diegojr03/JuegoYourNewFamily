using UnityEngine;
using System.Collections.Generic;
using TMPro;
using YNF.Progreso;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Estado del Juego")]
    public bool TieneNieve = false;

    [Header("Objetos a Activar (Nieve)")]
    public GameObject objetoSinNieve;
    public GameObject objetoConNieve;

    [Header("Configuración de Misión por Tags")]
    public string tagObjetosMision = "ObjetoLin";
    public GameObject npcAAfecivar;
    public GameObject dialogoAActivar;
    public GameObject npcADesactivar;
    public GameObject dialogoADesactivar;
    
    private bool misionCompletada = false;
    private bool objetosDetectadosAlMenosUnaVez = false;
    public TextMeshProUGUI textoMision;

    void Awake()
    {
        // Instance se declaraba pero no se asignaba nunca, asi que
        // GameManager.Instance era siempre null. Nadie lo usaba todavia, pero
        // era una trampa esperando a que alguien lo hiciera.
        Instance = this;
    }

    void Start()
    {
        // AQUI ESTABA EL BLOQUEO DEL TUTORIAL.
        //
        // Start() corre DESPUES de OnSceneLoaded, o sea despues de que el
        // guardado haya restaurado la foto del mundo y reproducido el
        // progreso. TieneNieve y misionCompletada no se guardan en ninguna
        // parte, asi que al cargar volvian a su valor del inspector (false) y
        // estas tres lineas deshacian lo que el guardado acababa de montar:
        // apagaban ConversacionTraperoConNieve (la unica conversacion viva
        // con Cairen, porque la version SinNieve ya estaba destruida) y
        // apagaban el NPC de Lin en la segunda parte. El jugador se quedaba
        // sin poder hablar con ellos para siempre.
        //
        // En partida nueva estas lineas si hacen falta: fijan el estado de
        // salida. Al cargar, manda el guardado.
        if (SaveManager.Instance != null && SaveManager.Instance.EscenaVieneDePartidaGuardada)
        {
            LogProgreso.Info("GameManager: la escena viene de una partida guardada, " +
                             "se respeta el estado reconstruido y no se fuerza el inicial.");
            return;
        }

        ActualizarObjetos();

        // Aseguramos que el NPC empiece desactivado
        if (npcAAfecivar != null) npcAAfecivar.SetActive(false);
    }

    void Update()
    {
        // Solo comprobamos si la misión no ha terminado ya
        if (!misionCompletada)
        {
            VerificarObjetosPorTag();

        }
    }

    void VerificarObjetosPorTag()
    {
        // Buscamos objetos activos con el tag "ObjetoLin"
        GameObject[] objetosRestantes = GameObject.FindGameObjectsWithTag(tagObjetosMision);

        // 1. Primero confirmamos que los objetos existen en la escena
        if (!objetosDetectadosAlMenosUnaVez && objetosRestantes.Length > 0)
        {
            objetosDetectadosAlMenosUnaVez = true;
            Debug.Log("Objetos de misión detectados. Esperando a su destrucción...");
        }

        // 2. Solo si ya existían y ahora la cuenta es 0, completamos la misión
        // Esto garantiza que se han DESTRUIDO (ya que FindGameObjectsWithTag no los encontrará)
        if (objetosDetectadosAlMenosUnaVez && objetosRestantes.Length == 0)
        {
            CompletarMision();
        }
    }

    void CompletarMision()
    {
        misionCompletada = true;

        // Activar nuevos elementos
        if (npcAAfecivar != null) npcAAfecivar.SetActive(true);
        if (dialogoAActivar != null) dialogoAActivar.SetActive(true);

        // Desactivar elementos antiguos
        if (npcADesactivar != null) npcADesactivar.SetActive(false);
        if (dialogoADesactivar != null) Destroy(dialogoADesactivar);

        textoMision.text = "VE A HABLAR CON LIN DE NUEVO";

        Debug.Log("Todos los objetos 'ObjetoLin' han sido destruidos. Cambiando NPCs y Diálogos.");
    }

    // --- Tus métodos anteriores ---
    public void SetTieneNieve(bool valor)
    {
        TieneNieve = valor;
        ActualizarObjetos();
    }

    void ActualizarObjetos()
    {
        if (objetoSinNieve != null)
        {
            objetoSinNieve.SetActive(!TieneNieve);
            LogProgreso.Info($"GameManager: {objetoSinNieve.name} -> {!TieneNieve} (TieneNieve={TieneNieve})",
                             objetoSinNieve);
        }

        if (objetoConNieve != null)
        {
            objetoConNieve.SetActive(TieneNieve);
            LogProgreso.Info($"GameManager: {objetoConNieve.name} -> {TieneNieve} (TieneNieve={TieneNieve})",
                             objetoConNieve);
        }
    }
}