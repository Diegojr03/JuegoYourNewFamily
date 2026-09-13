using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YNF.Localizacion
{
    /// <summary>
    /// El CSV de traduccion, ya parseado y listo para consultar.
    ///
    /// Es una clase de datos pura, sin MonoBehaviour, a proposito: la usa el
    /// juego en ejecucion a traves de LocalizationManager, y tambien las
    /// herramientas del editor para previsualizar traducciones sin entrar en
    /// modo de juego. Asi solo hay un sitio donde se decide como se lee la tabla.
    /// </summary>
    public class TablaLocalizacion
    {
        /// <summary>Idiomas encontrados en la cabecera, en su orden del fichero.</summary>
        public readonly List<string> Idiomas = new List<string>();

        /// <summary>Por idioma: clave estable -> traduccion.</summary>
        private readonly Dictionary<string, Dictionary<string, string>> _porClave =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Por idioma: texto espanol normalizado -> traduccion.</summary>
        private readonly Dictionary<string, Dictionary<string, string>> _porOrigen =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Clave -> espanol de referencia. Lo usan las herramientas del editor.</summary>
        public readonly Dictionary<string, string> OrigenPorClave =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public int Filas { get; private set; }
        public string Error { get; private set; }

        public static TablaLocalizacion Cargar(string rutaAbsoluta)
        {
            var tabla = new TablaLocalizacion();
            try
            {
                if (!File.Exists(rutaAbsoluta))
                {
                    tabla.Error = $"No se encontro el CSV en {rutaAbsoluta}";
                    return tabla;
                }
                tabla.Parsear(File.ReadAllText(rutaAbsoluta, Encoding.UTF8));
            }
            catch (Exception e)
            {
                tabla.Error = e.Message;
            }
            return tabla;
        }

        private void Parsear(string contenido)
        {
            var filas = CsvLector.Leer(contenido);
            if (filas.Count < 2)
            {
                Error = "El CSV esta vacio o solo tiene cabecera.";
                return;
            }

            string[] cabecera = filas[0];
            int colEs = -1, colKey = -1;
            var columnas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int c = 0; c < cabecera.Length; c++)
            {
                string nombre = cabecera[c].Trim();
                if (nombre.Equals("es", StringComparison.OrdinalIgnoreCase)) { colEs = c; continue; }
                if (nombre.Equals("key", StringComparison.OrdinalIgnoreCase)) { colKey = c; continue; }
                if (nombre.Equals("contexto", StringComparison.OrdinalIgnoreCase)) continue;
                if (nombre.Equals("uso", StringComparison.OrdinalIgnoreCase)) continue;
                columnas[nombre] = c;
            }

            if (colEs < 0)
            {
                Error = "El CSV no tiene columna 'es'.";
                return;
            }

            foreach (var kv in columnas)
            {
                Idiomas.Add(kv.Key);
                _porClave[kv.Key] = new Dictionary<string, string>(filas.Count, StringComparer.Ordinal);
                _porOrigen[kv.Key] = new Dictionary<string, string>(filas.Count, StringComparer.Ordinal);
            }

            for (int f = 1; f < filas.Count; f++)
            {
                string[] fila = filas[f];
                if (colEs >= fila.Length) continue;

                string origen = Loc.Normalizar(fila[colEs]);
                string clave = colKey >= 0 && colKey < fila.Length ? fila[colKey].Trim() : null;

                if (!string.IsNullOrEmpty(clave) && !string.IsNullOrEmpty(origen))
                    OrigenPorClave[clave] = origen;

                foreach (var kv in columnas)
                {
                    int col = kv.Value;
                    if (col >= fila.Length) continue;
                    string traduccion = fila[col];
                    if (string.IsNullOrWhiteSpace(traduccion)) continue;
                    traduccion = traduccion.Replace("\r\n", "\n").Replace("\r", "\n");

                    if (!string.IsNullOrEmpty(clave)) _porClave[kv.Key][clave] = traduccion;
                    // La misma cadena espanola puede salir en varias filas con
                    // claves distintas. Para el respaldo por texto vale la
                    // primera que aparezca: son traducciones equivalentes.
                    if (!string.IsNullOrEmpty(origen) && !_porOrigen[kv.Key].ContainsKey(origen))
                        _porOrigen[kv.Key][origen] = traduccion;
                }
            }

            Filas = filas.Count - 1;
        }

        /// <summary>
        /// Busca la traduccion: primero por clave, luego por texto de origen.
        /// Devuelve null si no hay ninguna, para que quien llame decida el respaldo.
        /// </summary>
        public string Buscar(string idioma, string clave, string origen)
        {
            if (string.IsNullOrEmpty(idioma)) return null;

            if (!string.IsNullOrEmpty(clave) &&
                _porClave.TryGetValue(idioma, out var porClave) &&
                porClave.TryGetValue(clave, out string porLaClave))
                return porLaClave;

            if (!string.IsNullOrEmpty(origen) &&
                _porOrigen.TryGetValue(idioma, out var porOrigen) &&
                porOrigen.TryGetValue(Loc.Normalizar(origen), out string porElTexto))
                return porElTexto;

            return null;
        }

        public int Traducciones(string idioma) =>
            _porClave.TryGetValue(idioma, out var d) ? d.Count : 0;
    }
}
