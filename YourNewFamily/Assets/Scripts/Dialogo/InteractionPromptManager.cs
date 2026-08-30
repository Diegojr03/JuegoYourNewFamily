using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptManager : MonoBehaviour
{
    public static InteractionPromptManager Instance { get; private set; }

    [Header("Referencia a la imagen de tecla F (Canvas)")]
    public Image promptImage; // Imagen F en el Canvas
    public Vector3 promptOffset = new Vector3(0, 1f, 0);

    [Header("Animación de flotación (subir/bajar)")]
    public float floatSpeed = 2f;
    public float floatAmplitude = 5f; // en píxeles (coordenadas de pantalla)

    private Camera mainCamera;
    private Transform player;
    private InteractionPoint activePoint;
    private SimpleDialogueSystem activeDialogue;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        mainCamera = Camera.main;
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (promptImage != null)
            promptImage.gameObject.SetActive(false);
    }

    void Update()
    {
        if (activePoint != null)
            UpdatePromptPosition(activePoint.transform.position + activePoint.promptOffset);
        else if (activeDialogue != null)
            UpdatePromptPosition(activeDialogue.transform.position + activeDialogue.promptOffset);
    }

    public void ShowPrompt(InteractionPoint point)
    {
        activePoint = point;
        activeDialogue = null;
        if (promptImage != null)
        {
            promptImage.gameObject.SetActive(true);
            // La posición se actualizará en Update con la animación
        }
    }

    public void ShowPrompt(SimpleDialogueSystem dialogue)
    {
        activeDialogue = dialogue;
        activePoint = null;
        if (promptImage != null)
        {
            promptImage.gameObject.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (promptImage != null)
            promptImage.gameObject.SetActive(false);

        activePoint = null;
        activeDialogue = null;
    }

    private void UpdatePromptPosition(Vector3 worldPosition)
    {
        if (promptImage == null || mainCamera == null) return;

        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        // Si está detrás de la cámara, ocultar
        if (screenPosition.z < 0)
        {
            promptImage.gameObject.SetActive(false);
            return;
        }

        // Asegurar que la imagen esté activa
        if (!promptImage.gameObject.activeSelf)
            promptImage.gameObject.SetActive(true);

        // Calcular offset de flotación
        float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        Vector3 finalPos = screenPosition + new Vector3(0, offsetY, 0);

        promptImage.transform.position = finalPos;
    }
}