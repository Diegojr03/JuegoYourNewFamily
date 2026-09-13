using UnityEngine;

public class SaveableObject : MonoBehaviour
{
    [Tooltip("ID único para este objeto. Genera uno nuevo con el botón.")]
    public string objectId;

    [ContextMenu("Generate New ID")]
    private void GenerateId()
    {
        objectId = System.Guid.NewGuid().ToString();
        Debug.Log($"🆔 ID generado para {gameObject.name}: {objectId}");
    }

    void Reset()
    {
        GenerateId();
    }

    // Antes aqui se registraba el objeto como inactivo al destruirse. Se ha
    // quitado a proposito: OnDestroy tambien se dispara al descargar la
    // escena (al salir al menu, por ejemplo), y la guarda de scene.isLoaded
    // no es fiable en ese momento, asi que media escena quedaba marcada como
    // inactiva en memoria. La destruccion real se registra de forma explicita
    // con RegisterObjectDestroyed desde quien destruye el objeto, y el estado
    // del mundo se reconstruye al cargar desde los eventos de progreso.
}