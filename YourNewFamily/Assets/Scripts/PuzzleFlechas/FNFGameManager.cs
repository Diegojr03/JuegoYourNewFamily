using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FNFGameManager : MonoBehaviour
{
    public static FNFGameManager Instance;
    public event Action OnPuzzleCompleted;

    [Header("Configuración de Flechas")]
    public GameObject[] arrowPrefabs;
    public RectTransform[] spawnPoints;
    public RectTransform hitLine;
    public float arrowSpeed = 800f;
    public float spawnInterval = 1f;
    public float hitRange = 50f;

    [Header("Puntuación")]
    public int targetScore = 500;
    public int pointsPerHit = 50;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip puzzleMusic;

    [Header("Fade de Salida")]
    public float fadeOutDuration = 0.5f;
    public CanvasGroup puzzleCanvasGroup;

    [Header("Acciones al Completar Puzzle")]
    public GameObject[] objectsToActivate;
    public GameObject[] objectsToDeactivate;
    public bool disablePuzzlePanelOnComplete = true;
    public float delayBeforeActions = 1f;

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI completionText;
    public GameObject puzzlePanel;

    private int score = 0;
    private bool puzzleActive = false;
    private Coroutine spawnCoroutine;
    private bool puzzleCompleted = false;
    private Coroutine autoCompleteCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (arrowPrefabs == null || arrowPrefabs.Length != 4)
        {
            Debug.LogError("Arrow prefabs not properly configured!");
        }

        if (spawnPoints == null || spawnPoints.Length != 4)
        {
            Debug.LogError("Spawn points not properly configured!");
        }

        if (puzzleCanvasGroup == null && puzzlePanel != null)
        {
            puzzleCanvasGroup = puzzlePanel.GetComponent<CanvasGroup>();
            if (puzzleCanvasGroup == null)
            {
                puzzleCanvasGroup = puzzlePanel.AddComponent<CanvasGroup>();
            }
        }
    }

    public void StartPuzzle()
    {
        if (puzzleActive) return;

        score = 0;
        puzzleActive = true;
        puzzleCompleted = false;
        UpdateScore();

        if (puzzleCanvasGroup != null)
        {
            puzzleCanvasGroup.alpha = 1f;
        }

        if (completionText != null)
        {
            completionText.gameObject.SetActive(false);
        }

        ClearExistingArrows();
        spawnCoroutine = StartCoroutine(SpawnArrows());

        if (audioSource != null && puzzleMusic != null)
        {
            audioSource.clip = puzzleMusic;
            audioSource.loop = false;
            audioSource.Play();

            if (autoCompleteCoroutine != null) StopCoroutine(autoCompleteCoroutine);
            autoCompleteCoroutine = StartCoroutine(AutoCompleteWhenMusicEnds());
        }
    }

    IEnumerator AutoCompleteWhenMusicEnds()
    {
        while (audioSource != null && audioSource.isPlaying)
        {
            yield return null;
        }

        if (puzzleActive && !puzzleCompleted)
        {
            StartCoroutine(CompletePuzzleWithDelay());
        }
    }

    IEnumerator SpawnArrows()
    {
        while (puzzleActive)
        {
            yield return new WaitForSecondsRealtime(spawnInterval);
            if (puzzleActive)
            {
                SpawnSingleArrow();
            }
        }
    }

    void SpawnSingleArrow()
    {
        try
        {
            int randomArrow = UnityEngine.Random.Range(0, 4);

            if (arrowPrefabs == null || arrowPrefabs.Length <= randomArrow || arrowPrefabs[randomArrow] == null)
            {
                Debug.LogError($"Missing arrow prefab for index {randomArrow}");
                return;
            }

            if (spawnPoints == null || spawnPoints.Length <= randomArrow || spawnPoints[randomArrow] == null)
            {
                Debug.LogError($"Missing spawn point for index {randomArrow}");
                return;
            }

            GameObject newArrow = Instantiate(
                arrowPrefabs[randomArrow],
                spawnPoints[randomArrow].position,
                Quaternion.identity,
                puzzlePanel != null ? puzzlePanel.transform : null
            );

            ArrowController arrowController = newArrow.GetComponent<ArrowController>();
            if (arrowController == null)
            {
                Debug.LogError("Arrow prefab missing ArrowController component!");
                Destroy(newArrow);
                return;
            }

            arrowController.Setup(randomArrow, arrowSpeed);
            newArrow.SetActive(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error spawning arrow: {e.Message}");
        }
    }

    private void Update()
    {
        if (!puzzleActive) return;

        if (Input.GetKeyDown(KeyCode.UpArrow)) CheckArrowHit(0);
        if (Input.GetKeyDown(KeyCode.DownArrow)) CheckArrowHit(1);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) CheckArrowHit(2);
        if (Input.GetKeyDown(KeyCode.RightArrow)) CheckArrowHit(3);
    }

    void CheckArrowHit(int arrowType)
    {
        ArrowController[] arrows = FindObjectsByType<ArrowController>(FindObjectsSortMode.None);
        foreach (ArrowController arrow in arrows)
        {
            if (arrow == null || arrow.arrowType != arrowType) continue;

            float distanceToHitLine = Mathf.Abs(
                arrow.GetComponent<RectTransform>().anchoredPosition.y
                - hitLine.anchoredPosition.y
            );

            if (distanceToHitLine <= hitRange)
            {
                Destroy(arrow.gameObject);
                AddScore(pointsPerHit);
                break;
            }
        }
    }

    void AddScore(int points)
    {
        score += points;
        UpdateScore();

        if (score >= targetScore && puzzleActive && !puzzleCompleted)
        {
            StartCoroutine(CompletePuzzleWithDelay());
        }
    }

    void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = $"PUNTOS: {score}/{targetScore}";
        }
    }

    IEnumerator CompletePuzzleWithDelay()
    {
        puzzleCompleted = true;
        puzzleActive = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        if (autoCompleteCoroutine != null)
        {
            StopCoroutine(autoCompleteCoroutine);
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        ClearExistingArrows();

        // Fade out
        if (puzzleCanvasGroup != null && fadeOutDuration > 0)
        {
            float elapsed = 0;
            float startAlpha = puzzleCanvasGroup.alpha;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeOutDuration;
                puzzleCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, t);
                yield return null;
            }

            puzzleCanvasGroup.alpha = 0;
        }

        yield return new WaitForSecondsRealtime(delayBeforeActions);

        if (objectsToActivate != null)
        {
            foreach (GameObject obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }

        if (objectsToDeactivate != null)
        {
            foreach (GameObject obj in objectsToDeactivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        OnPuzzleCompleted?.Invoke();

        if (disablePuzzlePanelOnComplete && puzzlePanel != null)
        {
            puzzlePanel.SetActive(false);
        }
    }

    void ClearExistingArrows()
    {
        ArrowController[] arrows = FindObjectsByType<ArrowController>(FindObjectsSortMode.None);
        foreach (ArrowController arrow in arrows)
        {
            if (arrow != null)
            {
                Destroy(arrow.gameObject);
            }
        }
    }
}