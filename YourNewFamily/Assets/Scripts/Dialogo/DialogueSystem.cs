using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    [Header("Personajes del Diálogo")]
    public Transform characterLeft;
    public Transform characterRight;
    public SpriteRenderer spriteLeft;
    public SpriteRenderer spriteRight;

    [Header("Configuración")]
    public float moveSpeed = 5f;
    public float dialogueCooldown = 0.05f;
    public float horizontalOffsetPercent = 0.3f; // 30% del ancho de la cámara
    public float verticalOffsetPercent = 0.2f;   // 20% de la altura de la cámara
    public float autoAdvanceTime = 1.5f;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("Diálogos")]
    public List<Dialogue> dialogues = new List<Dialogue>();

    [System.Serializable]
    public class Dialogue
    {
        public string speakerName;
        [TextArea(3, 5)]
        public string dialogueText;
        public bool leftSpeaker;
    }

    private bool isDialogueActive = false;
    private MovimientoPersonaje playerMovement;
    private Camera mainCamera;
    private Vector2 leftCharacterTarget;
    private Vector2 rightCharacterTarget;
    private Vector2 hiddenPosition;

    void Start()
    {
        playerMovement = FindObjectOfType<MovimientoPersonaje>();
        mainCamera = Camera.main;
        dialoguePanel.SetActive(false);
        HideCharacters();
    }

    void HideCharacters()
    {
        if (characterLeft != null && characterRight != null && mainCamera != null)
        {
            CalculateHiddenPosition();
            characterLeft.position = hiddenPosition;
            characterRight.position = hiddenPosition;
        }
    }

    void CalculateTargetPositions()
    {
        if (mainCamera == null) return;

        // Calcular el tamaño de la cámara en unidades del mundo
        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        // Calcular offsets basados en porcentajes del tamaño de la cámara
        float horizontalOffset = cameraWidth * horizontalOffsetPercent;
        float verticalOffset = cameraHeight * verticalOffsetPercent;

        Vector3 cameraCenter = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, mainCamera.nearClipPlane));
        Vector3 cameraBottom = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, mainCamera.nearClipPlane));

        // Calcular posiciones objetivo relativas al tamaño de la cámara
        leftCharacterTarget = new Vector2(
            cameraCenter.x - horizontalOffset,
            cameraBottom.y + verticalOffset
        );

        rightCharacterTarget = new Vector2(
            cameraCenter.x + horizontalOffset,
            cameraBottom.y + verticalOffset
        );

        CalculateHiddenPosition();
    }

    void CalculateHiddenPosition()
    {
        if (mainCamera == null) return;

        Vector3 cameraBottom = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, mainCamera.nearClipPlane));
        float cameraHeight = mainCamera.orthographicSize * 2f;

        // Esconder personajes una altura completa de cámara más abajo
        hiddenPosition = new Vector2(
            cameraBottom.x,
            cameraBottom.y - cameraHeight
        );
    }

    public void StartDialogue(int dialogueIndex = 0)
    {
        if (!isDialogueActive && dialogues.Count > dialogueIndex)
        {
            CalculateTargetPositions();
            StartCoroutine(DialogueSequence(dialogueIndex));
        }
    }

    private IEnumerator DialogueSequence(int dialogueIndex)
    {
        isDialogueActive = true;

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        yield return StartCoroutine(MoveCharactersToPosition(true));
        dialoguePanel.SetActive(true);

        foreach (Dialogue dialogue in dialogues)
        {
            yield return StartCoroutine(ShowDialogue(dialogue));
            yield return new WaitForSeconds(0.5f);
        }

        dialoguePanel.SetActive(false);
        yield return StartCoroutine(MoveCharactersToPosition(false));

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        isDialogueActive = false;
    }

    private IEnumerator MoveCharactersToPosition(bool enter)
    {
        Vector2 leftTarget = enter ? leftCharacterTarget : hiddenPosition;
        Vector2 rightTarget = enter ? rightCharacterTarget : hiddenPosition;

        while (Vector2.Distance(characterLeft.position, leftTarget) > 0.1f ||
               Vector2.Distance(characterRight.position, rightTarget) > 0.1f)
        {
            characterLeft.position = Vector2.Lerp(characterLeft.position, leftTarget, moveSpeed * Time.deltaTime);
            characterRight.position = Vector2.Lerp(characterRight.position, rightTarget, moveSpeed * Time.deltaTime);
            yield return null;
        }

        characterLeft.position = leftTarget;
        characterRight.position = rightTarget;
    }

    private void HighlightCharacter(bool leftSpeaking)
    {
        if (spriteLeft != null && spriteRight != null)
        {
            spriteLeft.color = leftSpeaking ? Color.white : Color.gray;
            spriteRight.color = leftSpeaking ? Color.gray : Color.white;
        }
    }

    private IEnumerator ShowDialogue(Dialogue dialogue)
    {
        HighlightCharacter(dialogue.leftSpeaker);
        dialogueText.text = "";
        string fullText = dialogue.speakerName + ": " + dialogue.dialogueText;

        foreach (char letter in fullText.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueCooldown);
        }

        yield return StartCoroutine(WaitForInputOrTimeout());
    }

    private IEnumerator WaitForInputOrTimeout()
    {
        float timer = 0f;
        bool inputReceived = false;

        // Esperar hasta que se presione Space o pasen 2 segundos
        while (timer < autoAdvanceTime && !inputReceived)
        {
            timer += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                inputReceived = true;
            }
            yield return null;
        }

        // Pequeña pausa adicional si se usó input manual
        if (inputReceived)
        {
            yield return new WaitForSeconds(0.1f);
        }
    }

    // Método para debug visual en el editor
    void OnDrawGizmosSelected()
    {
        if (mainCamera != null && Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(leftCharacterTarget, 0.3f);
            Gizmos.DrawWireSphere(rightCharacterTarget, 0.3f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hiddenPosition, 0.3f);
        }
    }
}
