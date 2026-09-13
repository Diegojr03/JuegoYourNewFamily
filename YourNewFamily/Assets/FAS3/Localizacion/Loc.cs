using System.Security.Cryptography;
using System.Text;

namespace YNF.Localizacion
{
    /// <summary>
    /// Fachada estatica del sistema de localizacion.
    ///
    /// Hay dos formas de pedir una traduccion, y conviene entender por que:
    ///
    ///   Loc.T(clave, origen)   <- la buena. La clave es un identificador
    ///                             estable guardado en la escena junto al
    ///                             texto. Se puede reescribir el espanol sin
    ///                             que la traduccion se desenganche.
    ///
    ///   Loc.T(origen)          <- respaldo. Busca por el propio texto
    ///                             espanol. Sigue ahi para lo que no lleva
    ///                             clave (nombres de hablante, mensajes de
    ///                             puzle, literales sueltos del codigo) y
    ///                             como red de seguridad si una clave se
    ///                             queda sin asignar.
    ///
    /// En ambos casos, si no hay traduccion se devuelve el texto original.
    /// El juego nunca se queda en blanco ni muestra una clave cruda.
    /// </summary>
    public static class Loc
    {
        /// <summary>
        /// Traduce buscando primero por clave y, si no la encuentra, por el
        /// texto de origen. Es la forma recomendada.
        /// </summary>
        public static string T(string clave, string origen)
        {
            if (string.IsNullOrEmpty(origen)) return origen;
            return LocalizationManager.Traducir(clave, origen);
        }

        /// <summary>
        /// Traduce buscando solo por el texto de origen.
        /// </summary>
        public static string T(string origen)
        {
            if (string.IsNullOrEmpty(origen)) return origen;
            return LocalizationManager.Traducir(null, origen);
        }

        /// <summary>
        /// Normalizacion canonica del texto de origen antes de buscarlo.
        /// Unifica saltos de linea y recorta los extremos, para que un espacio
        /// de mas escrito en el inspector no rompa la correspondencia.
        ///
        /// IMPORTANTE: esta funcion tiene que hacer exactamente lo mismo que
        /// normalizar() en las herramientas de Python. Si cambia una, cambia la otra.
        /// </summary>
        public static string Normalizar(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;
            return texto.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
        }

        /// <summary>
        /// Id corto y estable derivado de un texto. Se usa para generar la
        /// clave la primera vez que se estampa una cadena; a partir de ahi la
        /// clave vive en la escena y ya no se recalcula, que es justo lo que
        /// permite reescribir el espanol sin perder la traduccion.
        /// </summary>
        public static string Clave(string origen)
        {
            string normalizado = Normalizar(origen);
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(normalizado));
                var sb = new StringBuilder("t_", 14);
                for (int i = 0; i < 6; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// Variante para cuando la misma cadena aparece en varios sitios y cada
        /// aparicion necesita su propia clave: anade un sufijo derivado del
        /// contexto (escena y ruta del objeto).
        /// </summary>
        public static string Clave(string origen, string contexto)
        {
            if (string.IsNullOrEmpty(contexto)) return Clave(origen);
            string baseClave = Clave(origen);
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(contexto));
                var sb = new StringBuilder(baseClave, 20);
                sb.Append('_');
                for (int i = 0; i < 2; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
