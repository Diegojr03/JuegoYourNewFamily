using UnityEngine;

namespace YNF.Localizacion
{
    /// <summary>
    /// Marca un campo de texto como clave de localizacion.
    ///
    /// En el juego no hace nada: es solo un anzuelo para el editor. El
    /// dibujante de propiedad asociado sustituye la caja de texto cruda por
    /// algo util: la clave en pequeno y, debajo, la traduccion en el idioma de
    /// previsualizacion elegido en Tools > Localizacion.
    ///
    /// Se pone sobre el campo de la CLAVE, y se le indica el nombre del campo
    /// hermano que guarda el texto espanol, para poder mostrar los dos juntos:
    ///
    ///     [TextArea(3, 5)] public string dialogueText;
    ///     [ClaveLocalizacion(nameof(dialogueText))] public string locKey;
    /// </summary>
    public class ClaveLocalizacionAttribute : PropertyAttribute
    {
        /// <summary>Nombre del campo hermano que guarda el texto en espanol.</summary>
        public readonly string CampoOrigen;

        public ClaveLocalizacionAttribute(string campoOrigen)
        {
            CampoOrigen = campoOrigen;
        }
    }
}
