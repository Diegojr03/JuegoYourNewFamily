using UnityEngine;

public class ButtonController : MonoBehaviour
{
    [Header("Tubería Controlada por este Botón")]
    [Tooltip("La única tubería que este botón controlará")]
    public PipeController tuberiaControlada;

    [Header("Configuración Visual (Opcional)")]
    public Sprite spriteNormal;
    public Sprite spritePresionado;

    private SpriteRenderer spriteRenderer;
    private UnityEngine.UI.Button botonComponente;
    private bool animandoPresion = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        botonComponente = GetComponent<UnityEngine.UI.Button>();

        if (botonComponente != null)
        {
            botonComponente.onClick.AddListener(OnButtonPressed);
        }
    }

    public void OnButtonPressed()
    {
        Debug.Log($"Botón {gameObject.name} presionado - Girando {tuberiaControlada?.name} a la DERECHA");

        if (tuberiaControlada != null)
            tuberiaControlada.GirarDerecha();  // Aquí es derecha
    }

    private System.Collections.IEnumerator AnimarPresion()
    {
        animandoPresion = true;
        Sprite spriteOriginal = spriteRenderer.sprite;
        spriteRenderer.sprite = spritePresionado;

        yield return new WaitForSeconds(0.1f);

        spriteRenderer.sprite = spriteOriginal;
        animandoPresion = false;
    }
}