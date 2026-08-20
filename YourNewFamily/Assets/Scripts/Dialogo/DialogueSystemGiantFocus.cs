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

    [Header("Identificador de Diálogo")]
    public string dialogueId = ""; // 🔥 NUEVO

    [Header("Posicionamiento")]
    [Range(0f, 1f)]
    public float verticalPositionPercent = 0.65f;
    public float moveSpeed = 5f;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public GameObject speakerContainer;

    [Header("Visuales")]
    public Color dimmedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public float dialogueCooldown = 0.05f;

    [Header("Configuración Post-Diálogo")]
    public GameObject[] objectsToActivateAfter;
    public GameObject[] objectsToDestroyAfter;
    public bool destroyAfterDialogue = false;

    public List<DialogueLine> dialogues = new List<DialogueLine>();

    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(3, 5)] public string dialogueText;
        public bool isGiantSpeaking;
    }

    private bool isDialogueActive = false;
    private Camera mainCamera;
    private Vector2 targetPosition;
    private Vector2 hiddenPosition;
    private MovimientoPersonaje playerMovement;
    private Rigidbody2D playerRigidbody;
    private Vector2 originalVelocity;

    void Start()
    {
        mainCamera = Camera.main;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<MovimientoPersonaje>();
            playerRigidbody = player.GetComponent<Rigidbody2D>();
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (speakerContainer != null) speakerContainer.SetActive(false);

        CalculatePositions();
        if (giantCharacter != null) giantCharacter.position = hiddenPosition;
    }

    void CalculatePositions()
    {
        if (mainCamera == null) return;
        targetPosition = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, verticalPositionPercent, mainCamera.nearClipPlane));
        float cameraHeight = mainCamera.orthographicSize * 2f;
        hiddenPosition = new Vector2(targetPosition.x, targetPosition.y - cameraHeight * 1.5f);
    }

    public void StartDialogue()
    {
        if (isDialogueActive || dialogues.Count == 0) return;

        // 🔥 Registrar diálogo completado
        if (!string.IsNullOrEmpty(dialogueId) && SaveManager.Instance != null)
            SaveManager.Instance.RegisterDialogueCompleted(dialogueId);

        StartCoroutine(DialogueSequence());
    }

    private IEnumerator DialogueSequence()
    {
        isDialogueActive = true;

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerRigidbody != null)
        {
            originalVelocity = playerRigidbody.linearVelocity;
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        CalculatePositions();
        yield return StartCoroutine(MoveGiant(targetPosition));

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        foreach (DialogueLine line in dialogues)
        {
            UpdateCharacterVisuals(line.isGiantSpeaking);
            yield return StartCoroutine(ShowLine(line));
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (speakerContainer != null) speakerContainer.SetActive(false);

        yield return StartCoroutine(MoveGiant(hiddenPosition));

        // 🔥 Activar objetos y registrar estado
        foreach (GameObject obj in objectsToActivateAfter)
        {
            if (obj != null)
            {
                SaveableObject saveable = obj.GetComponent<SaveableObject>();
                if (saveable != null && SaveManager.Instance != null)
                    SaveManager.Instance.RegisterObjectState(saveable.objectId, true);
                obj.SetActive(true);
            }
        }

        foreach (GameObject obj in objectsToDestroyAfter)
        {
            if (obj != null)
            {
                SaveableObject saveable = obj.GetComponent<SaveableObject>();
                if (saveable != null && SaveManager.Instance != null)
                {
                    SaveManager.Instance.RegisterObjectDestroyed(saveable.objectId);
                }
                Destroy(obj);
            }
        }

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerRigidbody != null) playerRigidbody.linearVelocity = originalVelocity;

        isDialogueActive = false;
        if (destroyAfterDialogue)
        {
            SaveableObject thisSaveable = GetComponent<SaveableObject>();
            if (thisSaveable != null && SaveManager.Instance != null)
                SaveManager.Instance.RegisterObjectDestroyed(thisSaveable.objectId);
            Destroy(gameObject);
        }
    }

    private void UpdateCharacterVisuals(bool isGiantSpeaking)
    {
        if (giantSprite == null) return;
        giantSprite.color = isGiantSpeaking ? Color.white : dimmedColor;
        giantCharacter.localScale = giantScale;
    }

    private IEnumerator ShowLine(DialogueLine line)
    {
        if (speakerText != null) speakerText.text = line.speakerName;
        if (speakerContainer != null) speakerContainer.SetActive(!string.IsNullOrEmpty(line.speakerName));

        string fullText = line.dialogueText;
        Coroutine typingCoroutine = StartCoroutine(TypeText(fullText));

        bool typingCompleted = false;

        while (!typingCompleted)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StopCoroutine(typingCoroutine);
                dialogueText.text = fullText;
                typingCompleted = true;
            }
            if (dialogueText.text == fullText) typingCompleted = true;
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);
        bool nextLineRequested = false;
        while (!nextLineRequested)
        {
            if (Input.GetKeyDown(KeyCode.Space)) nextLineRequested = true;
            yield return null;
        }
    }

    private IEnumerator TypeText(string text)
    {
        dialogueText.text = "";
        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueCooldown);
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
        if (other.CompareTag("Player") && !isDialogueActive) StartDialogue();
    }
}