
using UnityEngine;

public class InventoryItem : MonoBehaviour
{
    [Header("Datos del Item")]
    public string itemId = "item_unique_id";
    public string itemName = "Nombre del Item";
    public Sprite itemIcon; // Este sprite aparecerá en la mochila
    [TextArea(3, 5)]
    public string description = "Descripción del item";

    [Header("Configuración")]
    public bool isUsable = true;
    public bool isConsumable = true; // Si desaparece al usarse

    [Header("Efectos/Recompensas")]
    public int healthRestore = 0;
    public int scorePoints = 0;

    // Método llamado cuando se usa el item
    public virtual void UseItem()
    {
        Debug.Log($"Usando item: {itemName}");

        // Aquí puedes añadir efectos específicos
        if (healthRestore > 0)
        {
            // Ejemplo: restaurar salud al jugador
            // PlayerHealth.Instance?.Heal(healthRestore);
        }

        if (scorePoints > 0)
        {
            // GameManager.Instance?.AddScore(scorePoints);
        }

        if (isConsumable)
        {
            // Remover del inventario
            InventorySystem.Instance?.RemoveItem(itemId);
        }
    }

    // Método llamado cuando se muestra información detallada
    public virtual string GetDescription()
    {
        return description;
    }
}