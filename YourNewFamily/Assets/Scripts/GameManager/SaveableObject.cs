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

    // Solo registramos si el objeto es destruido expresamente durante la partida en vivo
    void OnDestroy()
    {
        if (SaveManager.Instance != null && !SaveManager.Instance.IsLoading)
        {
            // Verificamos que la escena no se esté descargando
            if (gameObject.scene.isLoaded)
            {
                SaveManager.Instance.RegisterObjectState(objectId, false);
            }
        }
    }
}