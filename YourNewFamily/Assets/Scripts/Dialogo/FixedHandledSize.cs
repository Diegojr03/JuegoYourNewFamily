using UnityEngine;
using UnityEngine.UI;

public class FixedHandledSize : MonoBehaviour
{
    [Range(0.05f, 0.5f)]
    public float handleSize = 0.15f;

    private ScrollRect scrollRect;

    void Start()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect.verticalScrollbar != null)
            scrollRect.verticalScrollbar.size = handleSize;
    }

    void Update()
    {
        // Forzar el tamaño en cada frame para que no se recalcule
        if (scrollRect.verticalScrollbar != null)
            scrollRect.verticalScrollbar.size = handleSize;
    }
}
