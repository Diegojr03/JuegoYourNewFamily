using TMPro;
using UnityEngine;

namespace YNF.Localizacion
{
    /// <summary>
    /// Localiza una etiqueta de texto fija: titulos de menu, botones, creditos,
    /// cabeceras de opciones... todo lo que esta escrito directamente en el
    /// componente de texto y no lo pinta ningun script.
    ///
    /// Guarda el texto espanol como origen y una clave estable. Repinta al
    /// arrancar y cada vez que cambia el idioma.
    ///
    /// No hay que ponerlo a mano en cada etiqueta: el menu
    /// Tools > Localizacion > Preparar etiquetas de la escena abierta
    /// lo inyecta en todas de golpe, y Asignar claves rellena las claves.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Localizacion/Texto localizado (TMP)")]
    public class LocalizedTMP : MonoBehaviour
    {
        [Tooltip("Texto espanol original. Se puede reescribir sin miedo: la traduccion " +
                 "va enganchada a la clave, no a este texto.")]
        [TextArea(2, 6)]
        [SerializeField] private string textoOrigen;

        [ClaveLocalizacion(nameof(textoOrigen))]
        [SerializeField] private string locKey;

        private TMP_Text _texto;

        public string TextoOrigen
        {
            get => textoOrigen;
            set { textoOrigen = value; Repintar(); }
        }

        public string LocKey
        {
            get => locKey;
            set => locKey = value;
        }

        private void Awake() => _texto = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            if (_texto == null) _texto = GetComponent<TMP_Text>();
            LocalizationManager.OnIdiomaCambiado += Repintar;
            Repintar();
        }

        private void OnDisable() => LocalizationManager.OnIdiomaCambiado -= Repintar;

        private void Repintar()
        {
            if (_texto == null || string.IsNullOrEmpty(textoOrigen)) return;
            _texto.text = Loc.T(locKey, textoOrigen);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            // Al anadir el componente, captura el texto que ya tenga la etiqueta.
            var tmp = GetComponent<TMP_Text>();
            if (tmp != null && string.IsNullOrEmpty(textoOrigen)) textoOrigen = tmp.text;
        }

        /// <summary>Usado por la herramienta de editor que inyecta el componente.</summary>
        public void CapturarOrigenDesdeEtiqueta()
        {
            var tmp = GetComponent<TMP_Text>();
            if (tmp != null) textoOrigen = tmp.text;
        }

        /// <summary>
        /// Escribe en la etiqueta el texto que toque, sin tocar textoOrigen.
        /// Lo usa la previsualizacion de idioma en la escena.
        /// </summary>
        public void PintarEnEditor(string texto)
        {
            var tmp = GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = texto;
        }
#endif
    }
}
