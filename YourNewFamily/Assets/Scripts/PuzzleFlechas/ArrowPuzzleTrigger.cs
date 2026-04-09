using TMPro;
using UnityEngine;

public class ArrowPuzzleTrigger : MonoBehaviour
{
    [Header("Referencias Obligatorias")]
    [SerializeField] private GameObject puzzlePanel; // Panel del juego de flechas
    [SerializeField] private Collider2D puzzleCollider; // Collider que activa el puzzle
    [SerializeField] private TextMeshProUGUI completionText; // Texto de completado
    public TextMeshProUGUI completadoText;
    private void Start()
    {
        // Asegurarse que el panel está desactivado al inicio
        if (puzzlePanel != null)
        {
            puzzlePanel.SetActive(false);
        }

        // Desactivar el texto de completado
        if (completionText != null)
        {
            completionText.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && puzzlePanel != null)
        {
            ActivatePuzzle();
        }
    }

    private void ActivatePuzzle()
    {
        // Pausar el juego principal
        Time.timeScale = 0f;

        // Activar el panel del puzzle
        puzzlePanel.SetActive(true);

        // Iniciar el juego de flechas
        if (FNFGameManager.Instance != null)
        {
            FNFGameManager.Instance.StartPuzzle();
            FNFGameManager.Instance.OnPuzzleCompleted += HandlePuzzleCompletion;
        }
        else
        {
            Debug.LogError("FNFGameManager instance not found!");
        }
    }

    private void HandlePuzzleCompletion()
    {
        // Mostrar texto de completado si existe
        if (completionText != null)
        {
            completionText.gameObject.SetActive(true);
        }
        completadoText.text = "COMPLETADO";
        // Programar el cierre después de 3 segundos (usando tiempo real)
        Invoke("DeactivatePuzzle", 0f);
    }

    private void DeactivatePuzzle()
    {

        // Desactivar y destruir el panel si existe
        if (puzzlePanel != null)
        {
            puzzlePanel.SetActive(false);
            Destroy(puzzlePanel);
        }

        // Desactivar el collider si existe
        if (puzzleCollider != null)
        {
            puzzleCollider.enabled = false;
        }

        // Limpiar el evento si el GameManager existe
        if (FNFGameManager.Instance != null)
        {
            FNFGameManager.Instance.OnPuzzleCompleted -= HandlePuzzleCompletion;
        }
        // Reanudar el juego principal
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        // Limpieza adicional por si el objeto se destruye
        if (FNFGameManager.Instance != null)
        {
            FNFGameManager.Instance.OnPuzzleCompleted -= HandlePuzzleCompletion;
        }
    }
}