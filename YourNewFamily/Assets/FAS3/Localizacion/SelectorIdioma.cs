using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YNF.Localizacion
{
    /// <summary>
    /// Desplegable de idioma para el menu de opciones.
    ///
    /// Se arrastra sobre el mismo GameObject que tenga el TMP_Dropdown (o se le
    /// asigna a mano) y se rellena solo con los idiomas declarados en
    /// LocalizationManager.IdiomasDisponibles. No hay nada que configurar.
    ///
    /// Si en el menu se prefiere un boton que rota entre idiomas en vez de un
    /// desplegable, se puede asignar el boton en 'botonRotar'.
    /// </summary>
    [AddComponentMenu("Localizacion/Selector de idioma")]
    public class SelectorIdioma : MonoBehaviour
    {
        [Header("Interfaz")]
        [Tooltip("Desplegable de idioma. Si se deja vacio se busca en este mismo objeto.")]
        [SerializeField] private TMP_Dropdown desplegable;

        [Tooltip("Alternativa al desplegable: un boton que va rotando entre los idiomas.")]
        [SerializeField] private Button botonRotar;

        [Tooltip("Etiqueta que muestra el idioma activo cuando se usa el boton rotar.")]
        [SerializeField] private TMP_Text etiquetaIdioma;

        private void Awake()
        {
            if (desplegable == null) desplegable = GetComponent<TMP_Dropdown>();
        }

        private void OnEnable()
        {
            if (desplegable != null)
            {
                Avisar();
                Rellenar();
                desplegable.onValueChanged.AddListener(AlElegir);
            }
            else if (botonRotar == null)
            {
                Debug.LogError($"[Localizacion] '{name}' no tiene desplegable ni boton. " +
                               "Asigna un TMP_Dropdown en el inspector, o pon este componente " +
                               "sobre el propio objeto del desplegable.", this);
            }

            if (botonRotar != null) botonRotar.onClick.AddListener(Rotar);

            LocalizationManager.OnIdiomaCambiado += RefrescarEtiqueta;
            RefrescarEtiqueta();
        }

        private void OnDisable()
        {
            if (desplegable != null) desplegable.onValueChanged.RemoveListener(AlElegir);
            if (botonRotar != null) botonRotar.onClick.RemoveListener(Rotar);
            LocalizationManager.OnIdiomaCambiado -= RefrescarEtiqueta;
        }

        /// <summary>
        /// Un TMP_Dropdown anadido con "Add Component" sobre un objeto vacio no
        /// pinta nada: le faltan la plantilla, la etiqueta y el fondo, que solo
        /// se crean al usar GameObject > UI > Dropdown - TextMeshPro. Como el
        /// sintoma es una pantalla en blanco y no un error, conviene decirlo.
        /// </summary>
        private void Avisar()
        {
            if (desplegable.template == null || desplegable.captionText == null)
            {
                Debug.LogError(
                    $"[Localizacion] El desplegable de '{name}' esta incompleto " +
                    "(sin Template o sin Caption Text), asi que no se vera nada en pantalla. " +
                    "Borra este objeto y crealo con GameObject > UI > Dropdown - TextMeshPro, " +
                    "que monta la jerarquia entera, y anadele despues el Selector de idioma.",
                    this);
            }
        }

        private void Rellenar()
        {
            var opciones = new List<string>();
            int seleccionado = 0;

            for (int i = 0; i < LocalizationManager.IdiomasDisponibles.Length; i++)
            {
                var idioma = LocalizationManager.IdiomasDisponibles[i];
                opciones.Add(idioma.NombreNativo);
                if (idioma.Codigo == LocalizationManager.IdiomaActual) seleccionado = i;
            }

            desplegable.ClearOptions();
            desplegable.AddOptions(opciones);
            desplegable.SetValueWithoutNotify(seleccionado);
            desplegable.RefreshShownValue();
        }

        private void AlElegir(int indice)
        {
            var idiomas = LocalizationManager.IdiomasDisponibles;
            if (indice < 0 || indice >= idiomas.Length) return;
            LocalizationManager.CambiarIdioma(idiomas[indice].Codigo);
        }

        private void Rotar()
        {
            var idiomas = LocalizationManager.IdiomasDisponibles;
            int actual = 0;
            for (int i = 0; i < idiomas.Length; i++)
                if (idiomas[i].Codigo == LocalizationManager.IdiomaActual) { actual = i; break; }

            LocalizationManager.CambiarIdioma(idiomas[(actual + 1) % idiomas.Length].Codigo);
        }

        private void RefrescarEtiqueta()
        {
            if (etiquetaIdioma == null) return;
            foreach (var idioma in LocalizationManager.IdiomasDisponibles)
                if (idioma.Codigo == LocalizationManager.IdiomaActual)
                {
                    etiquetaIdioma.text = idioma.NombreNativo;
                    return;
                }
        }
    }
}
