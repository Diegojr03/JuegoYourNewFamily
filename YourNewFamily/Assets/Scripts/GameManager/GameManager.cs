using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Estado del Juego")]
    public bool TieneNieve = false;

    [Header("Objetos a Activar")]
    public GameObject objetoSinNieve;   // Se activa si TieneNieve = false
    public GameObject objetoConNieve;    // Se activa si TieneNieve = true

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ActualizarObjetos();
    }

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
