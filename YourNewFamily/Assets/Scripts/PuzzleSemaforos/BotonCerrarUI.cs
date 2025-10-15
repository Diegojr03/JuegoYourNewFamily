using UnityEngine;
using UnityEngine.UI;

public class BotonCerrarUI : MonoBehaviour
{
    private Button boton;
    private SemaforoPuzzle semaforoPadre;

    private void Start()
    {
        boton = GetComponent<Button>();
        semaforoPadre = GetComponentInParent<SemaforoPuzzle>();

        if (boton != null && semaforoPadre != null)
        {
            boton.onClick.AddListener(() => semaforoPadre.CerrarInterfaz());
        }
    }
}