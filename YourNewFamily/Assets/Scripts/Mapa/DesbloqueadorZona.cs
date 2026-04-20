using UnityEngine;

public class DesbloqueadorZona : MonoBehaviour
{
    [Header("CONFIGURACIÓN ZONA")]
    public string nombreZona;

    private void Start()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;
        else
            Debug.LogError($"DesbloqueadorZona en {name}: No tiene Collider2D");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (MapaManager.Instance != null)
            {
                MapaManager.Instance.DesbloquearZona(nombreZona);
                MapaManager.Instance.ActivarParpadeo(nombreZona);
                Debug.Log($"Zona DESBLOQUEADA: {nombreZona}");
            }
            else
            {
                Debug.LogError("No hay MapaManager en la escena");
            }
        }
    }
}