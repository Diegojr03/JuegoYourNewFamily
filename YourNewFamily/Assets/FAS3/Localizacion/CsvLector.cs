using System.Collections.Generic;
using System.Text;

namespace YNF.Localizacion
{
    /// <summary>
    /// Lector de CSV conforme a RFC 4180.
    ///
    /// No vale con hacer Split(','): los dialogos del juego llevan comas,
    /// comillas y saltos de linea dentro del propio texto. Este lector
    /// entiende campos entrecomillados, comillas escapadas ("") y saltos de
    /// linea dentro de un campo, que es justo lo que exporta cualquier hoja
    /// de calculo.
    /// </summary>
    public static class CsvLector
    {
        /// <summary>
        /// Convierte el contenido completo de un CSV en una lista de filas.
        /// </summary>
        public static List<string[]> Leer(string contenido)
        {
            var filas = new List<string[]>();
            if (string.IsNullOrEmpty(contenido)) return filas;

            // Quita el BOM si la hoja de calculo lo dejo al guardar.
            if (contenido[0] == '﻿') contenido = contenido.Substring(1);

            var campos = new List<string>();
            var campo = new StringBuilder();
            bool entrecomillado = false;
            int i = 0;

            while (i < contenido.Length)
            {
                char c = contenido[i];

                if (entrecomillado)
                {
                    if (c == '"')
                    {
                        // Dos comillas seguidas dentro de un campo entrecomillado
                        // representan una comilla literal.
                        if (i + 1 < contenido.Length && contenido[i + 1] == '"')
                        {
                            campo.Append('"');
                            i += 2;
                            continue;
                        }
                        entrecomillado = false;
                        i++;
                        continue;
                    }
                    campo.Append(c);
                    i++;
                    continue;
                }

                if (c == '"' && campo.Length == 0)
                {
                    entrecomillado = true;
                    i++;
                    continue;
                }

                if (c == ',')
                {
                    campos.Add(campo.ToString());
                    campo.Length = 0;
                    i++;
                    continue;
                }

                if (c == '\r' || c == '\n')
                {
                    // Fin de fila. Se salta el \n de un \r\n.
                    if (c == '\r' && i + 1 < contenido.Length && contenido[i + 1] == '\n') i++;
                    campos.Add(campo.ToString());
                    campo.Length = 0;
                    if (!FilaVacia(campos)) filas.Add(campos.ToArray());
                    campos.Clear();
                    i++;
                    continue;
                }

                campo.Append(c);
                i++;
            }

            // Ultima fila, si el fichero no termina en salto de linea.
            if (campo.Length > 0 || campos.Count > 0)
            {
                campos.Add(campo.ToString());
                if (!FilaVacia(campos)) filas.Add(campos.ToArray());
            }

            return filas;
        }

        private static bool FilaVacia(List<string> campos)
        {
            for (int i = 0; i < campos.Count; i++)
                if (!string.IsNullOrEmpty(campos[i])) return false;
            return true;
        }

        /// <summary>
        /// Escapa un valor para escribirlo en un CSV.
        /// </summary>
        public static string Escapar(string valor)
        {
            if (valor == null) valor = string.Empty;
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }
    }
}
