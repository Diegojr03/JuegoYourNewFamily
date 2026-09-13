using UnityEditor;
using UnityEngine;

namespace YNF.Localizacion.EditorTools
{
    /// <summary>
    /// Dibuja el campo locKey de forma util.
    ///
    /// En vez de una caja de texto con un hash dentro, que no le dice nada a
    /// nadie, muestra la traduccion del idioma de previsualizacion justo debajo
    /// del texto espanol, y la clave en pequeno por si hace falta buscarla en
    /// la hoja de calculo.
    ///
    /// Con el idioma de previsualizacion en espanol no dibuja nada: el campo
    /// desaparece del inspector y no estorba.
    /// </summary>
    [CustomPropertyDrawer(typeof(ClaveLocalizacionAttribute))]
    public class ClaveLocalizacionDrawer : PropertyDrawer
    {
        private const float AltoLinea = 16f;
        private const float Margen = 2f;

        private static GUIStyle _estiloClave;
        private static GUIStyle _estiloTraduccion;

        private static void PrepararEstilos()
        {
            if (_estiloClave == null)
            {
                _estiloClave = new GUIStyle(EditorStyles.miniLabel);
                _estiloClave.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            }
            if (_estiloTraduccion == null)
            {
                _estiloTraduccion = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            }
        }

        public override void OnGUI(Rect posicion, SerializedProperty propiedad, GUIContent etiqueta)
        {
            PrepararEstilos();

            var attr = (ClaveLocalizacionAttribute)attribute;
            string clave = propiedad.propertyType == SerializedPropertyType.String
                ? propiedad.stringValue : string.Empty;
            string origen = LeerOrigen(propiedad, attr.CampoOrigen);

            bool enEspanol = PrevisualizacionIdioma.Idioma == LocalizationManager.IdiomaOrigen;

            var fila = new Rect(posicion.x, posicion.y, posicion.width, AltoLinea);

            if (enEspanol)
            {
                // Solo la clave, discreta. Sin clave, un aviso para que se note.
                EditorGUI.LabelField(fila,
                    string.IsNullOrEmpty(clave)
                        ? "sin clave todavia (Tools > Localizacion > Asignar claves)"
                        : "clave: " + clave,
                    _estiloClave);
                return;
            }

            string traduccion = PrevisualizacionIdioma.Traducir(clave, origen);

            EditorGUI.LabelField(fila,
                $"{PrevisualizacionIdioma.Idioma.ToUpperInvariant()}   ·   " +
                (string.IsNullOrEmpty(clave) ? "sin clave" : clave),
                _estiloClave);

            var caja = new Rect(posicion.x, posicion.y + AltoLinea + Margen,
                                posicion.width, posicion.height - AltoLinea - Margen);

            if (string.IsNullOrEmpty(traduccion))
            {
                var antes = GUI.color;
                GUI.color = new Color(1f, 0.8f, 0.6f);
                EditorGUI.SelectableLabel(caja, "sin traducir", _estiloTraduccion);
                GUI.color = antes;
            }
            else
            {
                // De solo lectura a proposito: el texto traducido se edita en el
                // CSV, que es la unica fuente de verdad de las traducciones.
                EditorGUI.SelectableLabel(caja, traduccion, _estiloTraduccion);
            }
        }

        public override float GetPropertyHeight(SerializedProperty propiedad, GUIContent etiqueta)
        {
            if (PrevisualizacionIdioma.Idioma == LocalizationManager.IdiomaOrigen)
                return AltoLinea;

            PrepararEstilos();
            var attr = (ClaveLocalizacionAttribute)attribute;
            string clave = propiedad.propertyType == SerializedPropertyType.String
                ? propiedad.stringValue : string.Empty;
            string traduccion = PrevisualizacionIdioma.Traducir(clave, LeerOrigen(propiedad, attr.CampoOrigen));
            if (string.IsNullOrEmpty(traduccion)) traduccion = "sin traducir";

            float ancho = EditorGUIUtility.currentViewWidth - 40f;
            float alto = _estiloTraduccion.CalcHeight(new GUIContent(traduccion), ancho);
            return AltoLinea + Margen + Mathf.Clamp(alto, AltoLinea, 200f);
        }

        /// <summary>
        /// Lee el campo hermano que guarda el espanol. Puede ser una cadena o,
        /// en el caso de las notas de lore, un TextAsset.
        /// </summary>
        private static string LeerOrigen(SerializedProperty propiedad, string campoOrigen)
        {
            if (string.IsNullOrEmpty(campoOrigen)) return string.Empty;

            string ruta = propiedad.propertyPath;
            int corte = ruta.LastIndexOf('.');
            string rutaHermano = corte >= 0 ? ruta.Substring(0, corte + 1) + campoOrigen : campoOrigen;

            SerializedProperty hermano = propiedad.serializedObject.FindProperty(rutaHermano);
            if (hermano == null) return string.Empty;

            if (hermano.propertyType == SerializedPropertyType.String)
                return hermano.stringValue;

            if (hermano.propertyType == SerializedPropertyType.ObjectReference)
            {
                var asset = hermano.objectReferenceValue as TextAsset;
                return asset != null ? asset.text : string.Empty;
            }
            return string.Empty;
        }
    }
}
