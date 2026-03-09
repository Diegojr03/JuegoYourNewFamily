using UnityEngine;
using System.Collections.Generic;
using TMPro;

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

    void Start()
    {
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
        if (dialogoADesactivar != null) dialogoADesactivar.SetActive(false);

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
            objetoSinNieve.SetActive(!TieneNieve);

        if (objetoConNieve != null)
            objetoConNieve.SetActive(TieneNieve);
    }
}