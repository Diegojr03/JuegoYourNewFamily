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
    public GameObject[] arrowPrefabs; // 0=Up, 1=Down, 2=Left, 3=Right
    public RectTransform[] spawnPoints; // 0=Up, 1=Down, 2=Left, 3=Right
    public RectTransform hitLine;
    public float arrowSpeed = 800f;
    public float spawnInterval = 1f;
    public float hitRange = 50f;

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI completionText;
    public GameObject puzzlePanel; // Panel padre del juego de flechas

    private int score = 0;
    private bool puzzleActive = false;
    private Coroutine spawnCoroutine;

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

        // Validación inicial
        if (arrowPrefabs == null || arrowPrefabs.Length != 4)
        {
            Debug.LogError("Arrow prefabs not properly configured!");
        }

        if (spawnPoints == null || spawnPoints.Length != 4)
        {
            Debug.LogError("Spawn points not properly configured!");
        }
    }

    public void StartPuzzle()
    {
        if (puzzleActive) return;

        score = 0;
        puzzleActive = true;
        UpdateScore();

        if (completionText != null)
        {
            completionText.gameObject.SetActive(false);
        }

        ClearExistingArrows();
        spawnCoroutine = StartCoroutine(SpawnArrows());
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

            // Validación de arrays
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
                AddScore(50);
                break;
            }
        }
    }

    void AddScore(int points)
    {
        score += points;
        UpdateScore();

        if (score >= 500 && puzzleActive)
        {
            StartCoroutine(CompletePuzzleWithDelay());
        }
    }

    void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Puntos: {score}";
        }
    }

    IEnumerator CompletePuzzleWithDelay()
    {
        puzzleActive = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        ClearExistingArrows();

        // Esperar 1 segundo antes de desactivar el panel y disparar el evento
        yield return new WaitForSecondsRealtime(1f);

        OnPuzzleCompleted?.Invoke();

        if (puzzlePanel != null)
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