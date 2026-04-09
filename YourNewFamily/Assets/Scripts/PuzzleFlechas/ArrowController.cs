using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image), typeof(RectTransform))]
public class ArrowController : MonoBehaviour
{
    public int arrowType; // 0=Up, 1=Down, 2=Left, 3=Right
    private RectTransform rectTransform;
    private Image arrowImage;
    private float speed;
    private const float destroyYPosition = -388f;

    [Header("Sprite Configuration")]
    [SerializeField] private Sprite[] arrowSprites = new Sprite[4]; // 0=Up, 1=Down, 2=Left, 3=Right

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        arrowImage = GetComponent<Image>();

        if (arrowImage == null)
        {
            Debug.LogError("Missing Image component!", this);
            enabled = false;
        }

        if (rectTransform == null)
        {
            Debug.LogError("Missing RectTransform component!", this);
            enabled = false;
        }
    }

    public void Setup(int type, float newSpeed)
    {
        if (!enabled) return;

        arrowType = type;
        speed = newSpeed;

        // Configurar sprite
        if (arrowSprites != null && type >= 0 && type < arrowSprites.Length)
        {
            if (arrowSprites[type] != null)
            {
                arrowImage.sprite = arrowSprites[type];
                arrowImage.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"Missing sprite for arrow type {type}", this);
                SetDefaultColor(type);
            }
        }
        else
        {
            Debug.LogWarning("Invalid arrow type or sprites not configured", this);
            SetDefaultColor(type);
        }
    }

    private void Update()
    {
        if (!enabled || rectTransform == null) return;

        // Movimiento independiente del timeScale
        rectTransform.anchoredPosition += Vector2.down * speed * Time.unscaledDeltaTime;

        // Destruir cuando salga de la pantalla
        if (rectTransform.anchoredPosition.y < destroyYPosition)
        {
            Destroy(gameObject);
        }
    }

    private void SetDefaultColor(int type)
    {
        if (arrowImage == null) return;

        arrowImage.color = type switch
        {
            0 => Color.green,
            1 => Color.red,
            2 => Color.blue,
            3 => Color.yellow,
            _ => Color.white,
        };
    }
}