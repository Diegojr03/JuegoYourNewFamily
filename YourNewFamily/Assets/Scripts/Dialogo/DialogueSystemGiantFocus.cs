using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystemGiantFocus : MonoBehaviour
{
    [Header("Personaje Gigante (Derecha)")]
    public Transform giantCharacter;
    public SpriteRenderer giantSprite;
    public Vector3 giantScale = new Vector3(2f, 2f, 1f);

    [Header("Posicionamiento")]
    [Tooltip("0.5 es el centro exacto. Sube este valor (ej. 0.6 o 0.7) para que el personaje suba más.")]
    [Range(0f, 1f)]
    public float verticalPositionPercent = 0.6f;
    public float moveSpeed = 5f;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    [Header("Visuales")]
    public Color dimmedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public float dialogueCooldown = 0.05f;

    public List<DialogueLine> dialogues = new List<DialogueLine>();

    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(3, 5)] public string dialogueText;
        [Tooltip("Si es falso, el gigante se queda oscuro")]
        public bool isGiantSpeaking;
    }

    private bool isDialogueActive = false;
    private Camera mainCamera;
    private Vector2 targetPosition;
    private Vector2 hiddenPosition;
    private MovimientoPersonaje playerMovement;

    void Start()
    {
        mainCamera = Camera.main;
        playerMovement = FindObjectOfType<MovimientoPersonaje>();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        CalculatePositions();
        if (giantCharacter != null) giantCharacter.position = hiddenPosition;
    }

    void CalculatePositions()
    {
        if (mainCamera == null) return;

        // Calculamos el punto destino usando el porcentaje del inspector
        // Viewport (0.5, verticalPositionPercent) -> Mundo
        targetPosition = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, verticalPositionPercent, mainCamera.nearClipPlane));

        // Posición oculta (completamente fuera por abajo)
        float cameraHeight = mainCamera.orthographicSize * 2f;
        hiddenPosition = new Vector2(targetPosition.x, targetPosition.y - cameraHeight * 1.5f);
    }

    public void StartDialogue()
    {
        if (isDialogueActive || dialogues.Count == 0) return;
        StartCoroutine(DialogueSequence());
    }

    private IEnumerator DialogueSequence()
    {
        isDialogueActive = true;

        // Bloqueamos movimiento como en el script original
        if (playerMovement != null) playerMovement.enabled = false;

        // 1. Aparece el gigante
        CalculatePositions(); // Recalcular por si acaso cambió la cámara
        yield return StartCoroutine(MoveGiant(targetPosition));

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        // 2. Ciclo de diálogos
        foreach (DialogueLine line in dialogues)
        {
            UpdateCharacterVisuals(line.isGiantSpeaking);
            yield return StartCoroutine(ShowLine(line));
        }

        // 3. Finalizar y ocultar
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        yield return StartCoroutine(MoveGiant(hiddenPosition));

        if (playerMovement != null) playerMovement.enabled = true;
        isDialogueActive = false;
    }

    private void UpdateCharacterVisuals(bool isGiantSpeaking)
    {
        if (giantSprite == null) return;

        // Cambiamos el color para dar efecto de "apagado"
        giantSprite.color = isGiantSpeaking ? Color.white : dimmedColor;
        giantCharacter.localScale = giantScale;
    }

    private IEnumerator ShowLine(DialogueLine line)
    {
        if (speakerText != null) speakerText.text = line.speakerName;
        dialogueText.text = "";

        // Efecto de escribir texto
        foreach (char letter in line.dialogueText.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueCooldown);
        }

        // Esperar pulsación de espacio para avanzar
        bool skip = false;
        while (!skip)
        {
            if (Input.GetKeyDown(KeyCode.Space)) skip = true;
            yield return null;
        }
    }

    private IEnumerator MoveGiant(Vector2 target)
    {
        while (Vector2.Distance(giantCharacter.position, target) > 0.05f)
        {
            giantCharacter.position = Vector2.Lerp(giantCharacter.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        giantCharacter.position = target;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isDialogueActive)
        {
            StartDialogue();
        }
    }
}