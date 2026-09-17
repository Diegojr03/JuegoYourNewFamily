using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace YNF.Localizacion.EditorTools
{
    /// <summary>
    /// Busca textos que llegan al jugador sin pasar por Loc.T.
    ///
    /// POR QUE EXISTE
    /// El sistema de localizacion no tiene un punto unico por el que pase todo
    /// el texto: cualquier script puede escribir en un TMP cuando le apetezca.
    /// Eso significa que un texto sin traducir no da ningun error, no rompe
    /// nada y no se nota hasta que alguien juega en ingles y lo ve. Ya han
    /// aparecido tres tandas asi: la maquina de escribir de los dialogos, el
    /// backlog y los mensajes de los puzles.
    ///
    /// Esta herramienta convierte ese goteo en una lista que se revisa en diez
    /// segundos antes de una entrega.
    ///
    /// COMO LEER EL INFORME
    /// Se separa en tres bloques por cuanta confianza hay en cada hallazgo:
    ///
    ///  1. Literales. Texto escrito a mano en el codigo. Casi siempre es un
    ///     fallo de verdad. Ademas se comprueba contra el CSV, asi que el
    ///     informe dice si la cadena ya esta traducida y solo falta la llamada,
    ///     o si encima hay que darla de alta.
    ///
    ///  2. Campos localizables. Se pinta un campo que el extractor ya conoce
    ///     (mensajeCompletado, actionName, itemName...). Si el extractor lo
    ///     saca al CSV es que es texto de jugador, asi que pintarlo en crudo
    ///     es un fallo. Aqui cayeron los seis mensajes de puzle.
    ///
    ///  3. Para revisar. Se pinta una variable y desde fuera no se puede saber
    ///     si viene ya traducida de mas arriba. Muchas estaran bien: por
    ///     ejemplo dialogueText.text = fullText, donde fullText ya salio de un
    ///     Loc.T unas lineas antes. Se miran una vez y se silencian.
    ///
    /// COMO SILENCIAR UNA LINEA REVISADA
    /// Se le anade el comentario // loc-ok al final. La herramienta la salta y
    /// el informe se queda limpio, que es la unica forma de que alguien lo lea
    /// la proxima vez.
    /// </summary>
    public static class AuditorTextos
    {
        private const string Silenciador = "loc-ok";

        // Carpetas que no se miran: el propio sistema, lo que viene de fuera y
        // las herramientas de editor, cuyo texto lo lee el equipo y no el
        // jugador.
        private static readonly string[] CarpetasExcluidas =
        {
            "Assets/FAS3/Localizacion/",
            "Assets/TextMesh Pro/",
            "Assets/Plugins/",
            "Assets/Samples/",
        };

        // Asignaciones a un TMP. El (?!=) es para no confundir una comparacion
        // (if (x.text == y)) con una asignacion.
        private static readonly Regex Asignacion =
            new Regex(@"\.\s*text\s*\+?=(?!=)\s*(?<valor>[^;]+);", RegexOptions.Compiled);

        private static readonly Regex LlamadaSetText =
            new Regex(@"\.\s*SetText\s*\(\s*(?<valor>.+?)\s*\)\s*;", RegexOptions.Compiled);

        // Una expresion que es solo un identificador, posiblemente con puntos
        // e indices: mensaje, line.dialogueText, secciones[i].itemName.
        private static readonly Regex SoloIdentificador =
            new Regex(@"^[A-Za-z_]\w*(\s*\[[^\]]*\]|\s*\.\s*[A-Za-z_]\w*)*$", RegexOptions.Compiled);

        private enum Clase { Literal, LiteralInterpolado, CampoLocalizable, Revisar }

        private class Hallazgo
        {
            public string Ruta;
            public int Linea;
            public Clase Clase;
            public string Codigo;      // la linea tal cual, recortada
            public string Texto;       // el literal, si lo es
            public string Campo;       // el nombre del campo, si lo es
            public string Nota;        // estado en la tabla
        }

        [MenuItem("Tools/Localizacion/Buscar textos sin traducir", priority = 13)]
        public static void Buscar()
        {
            TablaLocalizacion tabla = TablaLocalizacion.Cargar(LocalizationManager.RutaCsvAbsoluta);
            if (!string.IsNullOrEmpty(tabla.Error))
                Debug.LogWarning($"[Localizacion] No se pudo leer la tabla ({tabla.Error}). " +
                                 "El informe saldra sin comprobar si los literales ya estan traducidos.");

            var hallazgos = new List<Hallazgo>();
            int ficherosMirados = 0, lineasConTexto = 0, yaCorrectas = 0, silenciadas = 0;

            string[] ficheros = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

            try
            {
                for (int i = 0; i < ficheros.Length; i++)
                {
                    string ruta = ARutaDeProyecto(ficheros[i]);
                    if (Excluido(ruta)) continue;

                    if (i % 25 == 0)
                        EditorUtility.DisplayProgressBar("Buscando textos sin traducir",
                                                         ruta, (float)i / ficheros.Length);

                    ficherosMirados++;
                    string[] lineas;
                    try { lineas = File.ReadAllLines(ficheros[i]); }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Localizacion] No se pudo leer {ruta}: {e.Message}");
                        continue;
                    }

                    for (int n = 0; n < lineas.Length; n++)
                    {
                        string linea = lineas[n];
                        string limpia = linea.TrimStart();
                        if (limpia.StartsWith("//") || limpia.StartsWith("*") || limpia.StartsWith("/*"))
                            continue;

                        string valor = ValorAsignado(linea);
                        if (valor == null) continue;

                        lineasConTexto++;

                        if (linea.Contains(Silenciador)) { silenciadas++; continue; }
                        if (linea.Contains("Loc.T")) { yaCorrectas++; continue; }
                        if (EsVacio(valor)) { yaCorrectas++; continue; }

                        hallazgos.Add(Clasificar(ruta, n + 1, linea.Trim(), valor, tabla));
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Informar(hallazgos, ficherosMirados, lineasConTexto, yaCorrectas, silenciadas);
        }

        // ------------------------------------------------------------------

        private static string ARutaDeProyecto(string completa) =>
            "Assets" + completa.Substring(Application.dataPath.Length).Replace('\\', '/');

        private static bool Excluido(string ruta) =>
            CarpetasExcluidas.Any(c => ruta.StartsWith(c, StringComparison.Ordinal)) ||
            ruta.Contains("/Editor/");

        private static string ValorAsignado(string linea)
        {
            Match m = Asignacion.Match(linea);
            if (m.Success) return m.Groups["valor"].Value.Trim();
            m = LlamadaSetText.Match(linea);
            if (m.Success) return m.Groups["valor"].Value.Trim();
            return null;
        }

        private static bool EsVacio(string valor) =>
            valor == "\"\"" || valor == "string.Empty" || valor == "null";

        private static Hallazgo Clasificar(string ruta, int linea, string codigo,
                                           string valor, TablaLocalizacion tabla)
        {
            var h = new Hallazgo { Ruta = ruta, Linea = linea, Codigo = codigo };

            // Cadena interpolada: no se puede traducir tal cual, hay que
            // convertirla en cadena con formato y darla de alta.
            if (valor.StartsWith("$\""))
            {
                h.Clase = Clase.LiteralInterpolado;
                h.Texto = Recorta(valor);
                h.Nota = "hay que convertirla en cadena con formato ({0}, {1}...) y registrarla " +
                         "en ExtractorLocalizacion.LiteralesEnCodigo";
                return h;
            }

            // Literal a pelo. Se mira si ya esta en la tabla, que es lo que
            // separa "solo falta la llamada" de "ademas hay que darla de alta".
            if (valor.StartsWith("\"") && valor.EndsWith("\"") && valor.Length > 1)
            {
                h.Clase = Clase.Literal;
                h.Texto = valor.Substring(1, valor.Length - 2);
                h.Nota = EstadoEnLaTabla(h.Texto, tabla);
                return h;
            }

            // Campo que el extractor ya saca al CSV: si esta ahi es texto de
            // jugador, asi que pintarlo en crudo es un fallo seguro.
            string campo = UltimoIdentificador(valor);
            if (campo != null && CamposLocalizables.Visibles.ContainsKey(campo))
            {
                h.Clase = Clase.CampoLocalizable;
                h.Campo = campo;
                h.Nota = CamposLocalizables.Visibles[campo];
                return h;
            }

            h.Clase = Clase.Revisar;
            h.Campo = campo;
            return h;
        }

        private static string EstadoEnLaTabla(string texto, TablaLocalizacion tabla)
        {
            if (!string.IsNullOrEmpty(tabla.Error)) return "tabla no disponible";

            bool enLaTabla = tabla.Buscar(LocalizationManager.IdiomaOrigen, null, texto) != null;
            if (!enLaTabla)
                return "NO esta en la tabla: hay que registrarla en " +
                       "ExtractorLocalizacion.LiteralesEnCodigo y volver a extraer";

            var faltan = new List<string>();
            foreach (var idioma in LocalizationManager.IdiomasDisponibles)
            {
                if (idioma.Codigo == LocalizationManager.IdiomaOrigen) continue;
                if (tabla.Buscar(idioma.Codigo, null, texto) == null) faltan.Add(idioma.Codigo);
            }

            return faltan.Count == 0
                ? "ya traducida: solo falta envolverla en Loc.T"
                : "en la tabla pero sin traducir a: " + string.Join(", ", faltan);
        }

        private static string UltimoIdentificador(string valor)
        {
            if (!SoloIdentificador.IsMatch(valor)) return null;
            string sinIndices = Regex.Replace(valor, @"\[[^\]]*\]", "");
            string[] trozos = sinIndices.Split('.');
            string ultimo = trozos[trozos.Length - 1].Trim();
            return string.IsNullOrEmpty(ultimo) ? null : ultimo;
        }

        private static string Recorta(string s, int max = 70) =>
            s.Length <= max ? s : s.Substring(0, max) + "...";

        // ------------------------------------------------------------------

        private static void Informar(List<Hallazgo> hallazgos, int ficheros, int lineas,
                                     int correctas, int silenciadas)
        {
            var literales = hallazgos.Where(h => h.Clase == Clase.Literal ||
                                                 h.Clase == Clase.LiteralInterpolado).ToList();
            var campos = hallazgos.Where(h => h.Clase == Clase.CampoLocalizable).ToList();
            var revisar = hallazgos.Where(h => h.Clase == Clase.Revisar).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("[Localizacion] Textos que llegan al jugador sin pasar por Loc.T");
            sb.AppendLine($"  ficheros mirados      : {ficheros}");
            sb.AppendLine($"  lineas que pintan texto: {lineas}");
            sb.AppendLine($"  ya pasan por Loc.T    : {correctas}");
            sb.AppendLine($"  silenciadas con {Silenciador} : {silenciadas}");
            sb.AppendLine();
            sb.AppendLine($"  1. literales sin traducir : {literales.Count}");
            sb.AppendLine($"  2. campos localizables    : {campos.Count}");
            sb.AppendLine($"  3. para revisar a mano    : {revisar.Count}");

            if (literales.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== 1. LITERALES ESCRITOS EN EL CODIGO ===");
                sb.AppendLine("Casi siempre es un fallo de verdad.");
                foreach (Hallazgo h in literales.OrderBy(h => h.Ruta).ThenBy(h => h.Linea))
                {
                    sb.AppendLine($"\n  {h.Ruta}:{h.Linea}");
                    sb.AppendLine($"      {Recorta(h.Codigo, 100)}");
                    sb.AppendLine($"      texto : \"{Recorta(h.Texto)}\"");
                    sb.AppendLine($"      estado: {h.Nota}");
                }
            }

            if (campos.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== 2. CAMPOS QUE EL EXTRACTOR YA SACA AL CSV ===");
                sb.AppendLine("Estan traducidos en la tabla y se pintan en crudo. Falta el Loc.T.");
                foreach (Hallazgo h in campos.OrderBy(h => h.Ruta).ThenBy(h => h.Linea))
                {
                    sb.AppendLine($"\n  {h.Ruta}:{h.Linea}   campo '{h.Campo}' ({h.Nota})");
                    sb.AppendLine($"      {Recorta(h.Codigo, 100)}");
                }
            }

            if (revisar.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== 3. PARA REVISAR A MANO ===");
                sb.AppendLine("Se pinta una variable y desde aqui no se sabe si ya viene traducida.");
                sb.AppendLine($"Si la revisas y esta bien, anadele // {Silenciador} al final de la linea.");
                foreach (Hallazgo h in revisar.OrderBy(h => h.Ruta).ThenBy(h => h.Linea))
                {
                    sb.AppendLine($"\n  {h.Ruta}:{h.Linea}");
                    sb.AppendLine($"      {Recorta(h.Codigo, 100)}");
                }
            }

            Debug.Log(sb.ToString());

            // Una linea por fichero con hallazgos serios, con el script como
            // contexto: haciendo doble clic en la consola se abre el fichero.
            foreach (var grupo in hallazgos
                         .Where(h => h.Clase != Clase.Revisar)
                         .GroupBy(h => h.Ruta)
                         .OrderBy(g => g.Key))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(grupo.Key);
                string lineasDelFichero = string.Join(", ", grupo.Select(h => h.Linea.ToString()));
                Debug.LogWarning($"[Localizacion] {Path.GetFileName(grupo.Key)}: " +
                                 $"{grupo.Count()} texto(s) sin Loc.T en la(s) linea(s) {lineasDelFichero}.",
                                 script);
            }

            int serios = literales.Count + campos.Count;
            string resumen = serios == 0
                ? (revisar.Count == 0
                    ? "Ningun texto se escapa. Todo lo que pinta pasa por Loc.T."
                    : $"Ningun fallo claro.\n\nQuedan {revisar.Count} lineas para revisar a mano.")
                : $"{serios} texto(s) llegan al jugador sin traducir:\n" +
                  $"   {literales.Count} literal(es) en el codigo\n" +
                  $"   {campos.Count} campo(s) localizable(s) en crudo\n\n" +
                  $"Ademas hay {revisar.Count} linea(s) para revisar a mano.";

            EditorUtility.DisplayDialog("Textos sin traducir",
                resumen + "\n\nEl detalle esta en la consola.", "Vale");
        }
    }
}
