using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueChoiceSystem : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(3, 5)] public string dialogueText;
        public bool leftSpeaker = true;
        public Sprite characterSprite;

        public bool endDialogueHere = false;
        public Choice[] choices;

        public bool giveItemAfterThisLine = false;
        public string itemIdToGive = "";
        public string itemNameToGive = "";
        public Sprite itemIconToGive = null;

        // 🔥 Sonido para cada 2 letras
        public AudioClip gibberishClip;
        [Range(0f, 1f)] public float gibberishVolume = 0.07f;
    }

    [System.Serializable]
    public class Choice
    {
        public string choiceText;
        public int nextDialogueIndex = -1;
    }

    [Header("Identificador de Diálogo")]
    public string dialogueId = "";

    [Header("Identificación")]
    public string protagonistName = "Lilith";

    [Header("Sistema de Inventario")]
    public GameObject itemToAddToInventory;
    public Sprite inventoryItemIcon;
    public string inventoryItemId = "item_default";
    public string inventoryItemName = "Nuevo Item";

    [Header("Diálogos")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();

    [Header("UI Principal")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public GameObject speakerContainer;

    [Header("UI de Opciones")]
    public GameObject choicePanel;
    public Transform choicesContainer;
    public GameObject choiceButtonPrefab;

    [Header("Personajes")]
    public Transform characterLeft;
    public Transform characterRight;
    public SpriteRenderer spriteLeft;
    public SpriteRenderer spriteRight;
    public Sprite defaultLeftSprite;
    public Sprite defaultRightSprite;

    [Header("Configuración")]
    public float moveSpeed = 5f;
    public float dialogueCooldown = 0.05f;
    public float horizontalOffsetPercent = 0.3f;
    public float verticalOffsetPercent = 0.2f;

    [Header("Configuración Avanzada")]
    public GameObject[] objectsToActivateAfter;
    public GameObject[] objectsToDestroyAfter;
    public bool destroyAfterDialogue = false;
    public float reuseDelay = 0.5f;

    [Header("Activación")]
    public bool autoActivate = false;

    private int currentIndex = 0;
    private bool isDialogueActive = false;
    private bool typing = false;
    private bool canInteract = false;
    private bool waitingForNextLine = false;
    private bool canReuse = true;

    private MovimientoPersonaje playerMovement;
    private Rigidbody2D playerRb;
    private Vector2 originalVelocity;
    private Camera mainCamera;

    private Vector2 leftCharacterTarget;
    private Vector2 rightCharacterTarget;
    private Vector2 hiddenPosition;
    private bool charactersHidden = true;

    private Coroutine typingCoroutine;

    private AudioSource gibberishAudioSource;

    void Start()
    {
        mainCamera = Camera.main;
        playerMovement = FindObjectOfType<MovimientoPersonaje>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerRb = player.GetComponent<Rigidbody2D>();

        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);

        HideCharacters();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        gibberishAudioSource = gameObject.AddComponent<AudioSource>();
        gibberishAudioSource.loop = false;
        gibberishAudioSource.playOnAwake = false;
    }

    void Update()
    {
        if (charactersHidden && mainCamera != null && !isDialogueActive)
        {
            CalculateHiddenPosition();

            if (characterLeft != null)
                characterLeft.position = Vector2.Lerp(characterLeft.position, hiddenPosition, moveSpeed * Time.deltaTime);
            if (characterRight != null)
                characterRight.position = Vector2.Lerp(characterRight.position, hiddenPosition, moveSpeed * Time.deltaTime);
        }

        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            if (typing)
            {
                SkipTyping();
            }
            else if (waitingForNextLine && !choicePanel.activeSelf)
            {
                waitingForNextLine = false;
                AdvanceDialogue();
            }
        }

        if (!isDialogueActive && canInteract && Input.GetKeyDown(KeyCode.F) && canReuse && !autoActivate)
        {
            StartDialogue();
        }
    }

    void CalculateHiddenPosition()
    {
        if (mainCamera == null) return;

        Vector3 bottom = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f));
        float height = mainCamera.orthographicSize * 2f;

        hiddenPosition = new Vector2(bottom.x, bottom.y - height);
    }

    public void StartDialogue()
    {
        if (isDialogueActive || dialogueLines.Count == 0 || !canReuse) return;

        if (!string.IsNullOrEmpty(dialogueId) && SaveManager.Instance != null)
            SaveManager.Instance.RegisterDialogueCompleted(dialogueId);

        InteractionPromptManager.Instance?.HidePrompt();

        currentIndex = 0;
        isDialogueActive = true;
        waitingForNextLine = false;
        canReuse = false;

        LockPlayer();

        dialoguePanel.SetActive(true);

        charactersHidden = false;

        CalculateTargetPositions();
        StartCoroutine(MoveCharactersToPosition(true));

        ShowDialogueLine(currentIndex);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isDialogueActive && other.CompareTag("Player") && canReuse)
        {
            canInteract = true;

            if (autoActivate)
            {
                StartDialogue();
            }
            else
            {
                InteractionPromptManager.Instance?.ShowPrompt(
                    GetComponent<InteractionPoint>()
                );
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            InteractionPromptManager.Instance?.HidePrompt();
        }
    }

    void ShowDialogueLine(int index)
    {
        DialogueLine line = dialogueLines[index];

        speakerText.text = line.speakerName;
        speakerContainer.SetActive(!string.IsNullOrEmpty(line.speakerName));

        if (BacklogManager.Instance != null)
        {
            string owner = GetConversationOwner();
            BacklogManager.Instance.AddDialogueWithConversationOwner(
                line.speakerName,
                line.dialogueText,
                owner
            );
        }

        HighlightCharacter(line.leftSpeaker, line.characterSprite);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line.dialogueText, line.gibberishClip, line.gibberishVolume));

        if (line.giveItemAfterThisLine)
        {
            GiveInventoryItem(
                line.itemIdToGive,
                line.itemNameToGive,
                line.itemIconToGive
            );
        }
    }

    private void GiveInventoryItem(string itemId, string itemName, Sprite itemIcon)
    {
        if (InventorySystem.Instance != null && !string.IsNullOrEmpty(itemId))
        {
            if (itemToAddToInventory != null)
            {
                InventorySystem.Instance.AddItemFromPrefab(itemToAddToInventory);
            }
            else if (itemIcon != null)
            {
                InventorySystem.Instance.AddSimpleItem(itemId, itemName, itemIcon);
            }
            else
            {
                InventorySystem.Instance.AddSimpleItem(
                    inventoryItemId,
                    inventoryItemName,
                    inventoryItemIcon
                );
            }
        }
    }

    IEnumerator TypeText(string text, AudioClip clip, float volume)
    {
        typing = true;
        waitingForNextLine = false;
        dialogueText.text = "";

        int charIndex = 0;
        foreach (char c in text.ToCharArray())
        {
            dialogueText.text += c;

            // Reproducir cada 2 letras (cuando charIndex sea par)
            if (clip != null && charIndex % 2 == 0)
            {
                gibberishAudioSource.PlayOneShot(clip, volume);
            }

            charIndex++;

            if (!typing)
                break;

            yield return new WaitForSeconds(dialogueCooldown);
        }

        DialogueLine currentLine = dialogueLines[currentIndex];
        dialogueText.text = currentLine.dialogueText;

        typing = false;

        if (currentLine.endDialogueHere)
        {
            waitingForNextLine = true;
            yield break;
        }

        if (currentLine.choices != null && currentLine.choices.Length > 0)
        {
            ShowChoices();
            yield break;
        }

        waitingForNextLine = true;
    }

    void ShowChoices()
    {
        DialogueLine line = dialogueLines[currentIndex];
        if (line.choices == null || line.choices.Length == 0) return;

        choicePanel.SetActive(true);

        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        foreach (Choice choice in line.choices)
        {
            GameObject btnObj = Instantiate(choiceButtonPrefab, choicesContainer);
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = choice.choiceText;

            Button btn = btnObj.GetComponent<Button>();
            int targetIndex = choice.nextDialogueIndex;
            string choiceText = choice.choiceText;

            btn.onClick.AddListener(() =>
            {
                choicePanel.SetActive(false);
                SelectChoice(targetIndex, choiceText);
            });
        }
    }

    void SkipTyping()
    {
        if (!typing) return;

        typing = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        DialogueLine line = dialogueLines[currentIndex];

        dialogueText.text = line.dialogueText;

        if (line.endDialogueHere)
        {
            waitingForNextLine = true;
            return;
        }

        if (line.choices != null && line.choices.Length > 0)
        {
            ShowChoices();
        }
        else
        {
            waitingForNextLine = true;
        }
    }

    IEnumerator WaitForNextLine()
    {
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        DialogueLine line = dialogueLines[currentIndex];
        if (line.endDialogueHere)
        {
            EndDialogue();
            yield break;
        }

        AdvanceDialogue();
    }

    void SelectChoice(int nextIndex, string choiceText)
    {
        if (BacklogManager.Instance != null)
        {
            string owner = GetConversationOwner();
            BacklogManager.Instance.AddDialogueWithConversationOwner(
                protagonistName,
                choiceText,
                owner
            );
        }

        if (nextIndex < 0 || nextIndex >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        currentIndex = nextIndex;
        ShowDialogueLine(currentIndex);
    }

    void AdvanceDialogue()
    {
        DialogueLine previousLine = dialogueLines[currentIndex];

        if (previousLine.endDialogueHere)
        {
            EndDialogue();
            return;
        }

        currentIndex++;

        if (currentIndex >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        ShowDialogueLine(currentIndex);
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);
        waitingForNextLine = false;

        StartCoroutine(EndDialogueSequence());
    }

    private IEnumerator EndDialogueSequence()
    {
        yield return StartCoroutine(MoveCharactersToPosition(false));

        UnlockPlayer();
        isDialogueActive = false;

        ExecuteAdvancedFunctionality();
    }

    void ExecuteAdvancedFunctionality()
    {
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

        if (destroyAfterDialogue)
        {
            SaveableObject thisSaveable = GetComponent<SaveableObject>();
            if (thisSaveable != null && SaveManager.Instance != null)
                SaveManager.Instance.RegisterObjectDestroyed(thisSaveable.objectId);
            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(AllowReuseAfterDelay());
        }
    }

    private IEnumerator AllowReuseAfterDelay()
    {
        yield return new WaitForSeconds(reuseDelay);
        canReuse = true;
    }

    void HighlightCharacter(bool left, Sprite custom)
    {
        if (left)
        {
            spriteLeft.color = Color.white;
            spriteRight.color = Color.gray;

            spriteLeft.sprite = custom != null ? custom : defaultLeftSprite;
            spriteRight.sprite = defaultRightSprite;
        }
        else
        {
            spriteLeft.color = Color.gray;
            spriteRight.color = Color.white;

            spriteRight.sprite = custom != null ? custom : defaultRightSprite;
            spriteLeft.sprite = defaultLeftSprite;
        }
    }

    void CalculateTargetPositions()
    {
        float height = mainCamera.orthographicSize * 2f;
        float width = height * mainCamera.aspect;

        float hOffset = width * horizontalOffsetPercent;
        float vOffset = height * verticalOffsetPercent;

        Vector3 center = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f));
        Vector3 bottom = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f));

        leftCharacterTarget = new Vector2(center.x - hOffset, bottom.y + vOffset);
        rightCharacterTarget = new Vector2(center.x + hOffset, bottom.y + vOffset);

        hiddenPosition = new Vector2(bottom.x, bottom.y - height);
    }

    IEnumerator MoveCharactersToPosition(bool enter)
    {
        Vector2 leftTarget = enter ? leftCharacterTarget : hiddenPosition;
        Vector2 rightTarget = enter ? rightCharacterTarget : hiddenPosition;

        if (!enter)
        {
            CalculateHiddenPosition();
            leftTarget = hiddenPosition;
            rightTarget = hiddenPosition;
        }

        while (Vector2.Distance(characterLeft.position, leftTarget) > 0.1f ||
               Vector2.Distance(characterRight.position, rightTarget) > 0.1f)
        {
            characterLeft.position = Vector2.Lerp(characterLeft.position, leftTarget, moveSpeed * Time.deltaTime);
            characterRight.position = Vector2.Lerp(characterRight.position, rightTarget, moveSpeed * Time.deltaTime);
            yield return null;
        }

        characterLeft.position = leftTarget;
        characterRight.position = rightTarget;

        if (!enter)
        {
            charactersHidden = true;
        }
        else
        {
            charactersHidden = false;
        }
    }

    void HideCharacters()
    {
        if (mainCamera == null) return;

        CalculateHiddenPosition();
        characterLeft.position = hiddenPosition;
        characterRight.position = hiddenPosition;
        charactersHidden = true;
    }

    void LockPlayer()
    {
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerRb != null)
        {
            originalVelocity = playerRb.linearVelocity;
            playerRb.linearVelocity = Vector2.zero;
        }
    }

    void UnlockPlayer()
    {
        if (playerMovement != null)
            playerMovement.enabled = true;

        if (playerRb != null)
            playerRb.linearVelocity = originalVelocity;
    }

    string GetConversationOwner()
    {
        foreach (var line in dialogueLines)
        {
            if (line.leftSpeaker)
                return line.speakerName;
        }
        return "Unknown";
    }

    void OnDestroy()
    {
        if (characterLeft != null)
            Destroy(characterLeft.gameObject);

        if (characterRight != null)
            Destroy(characterRight.gameObject);

        if (spriteLeft != null)
            Destroy(spriteLeft.gameObject);

        if (spriteRight != null)
            Destroy(spriteRight.gameObject);
    }
}