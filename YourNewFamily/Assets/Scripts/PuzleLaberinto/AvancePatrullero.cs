using UnityEngine;

public class AvancePatrullero : MonoBehaviour
{
    [SerializeField] private PatrulleroPillaPilla patrullero;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            patrullero.AvanzarAlSiguientePunto();
        }
    }
}