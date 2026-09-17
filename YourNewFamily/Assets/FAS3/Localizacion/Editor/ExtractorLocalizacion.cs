using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YNF.Localizacion.EditorTools
{
    /// <summary>
    /// Recorre el proyecto y vuelca al CSV todo el texto que ve el jugador.
    ///
    /// Va guiado por la CLAVE, no por el texto: si una linea ya tiene locKey y
    /// alguien reescribe el espanol en Unity, el extractor actualiza la columna
    /// 'es' de esa misma fila y la traduccion sigue en su sitio. Esa es la
    /// diferencia con la version anterior, donde cambiar una coma creaba una
    /// fila nueva y dejaba la traduccion huerfana.
    ///
    /// Lo que no lleva clave (nombres de hablante, mensajes de puzle, literales
    /// del codigo) se sigue identificando por su texto, que para cadenas cortas
    /// que nadie reescribe funciona perfectamente.
    /// </summary>
    public static class ExtractorLocalizacion
    {
        public const string RutaCsv = "Assets/StreamingAssets/Localization/Localization.csv";
        private const string CarpetaNotas = "Assets/Notas";

        /// <summary>
        /// Carpetas que no son contenido del juego. Las escenas de ejemplo de
        /// TextMesh Pro viven dentro de Assets y traen texto de demostracion en
        /// ingles, que no pinta nada en el CSV de traduccion.
        /// </summary>
        public static readonly string[] CarpetasExcluidas =
        {
            "Assets/TextMesh Pro/",
            "Assets/Plugins/",
            "Assets/Samples/",
        };

        public static bool EstaExcluida(string ruta)
        {
            string n = ruta.Replace('\\', '/');
            foreach (string carpeta in CarpetasExcluidas)
                if (n.StartsWith(carpeta, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Textos que no estan en ninguna escena porque estan escritos a pelo
        /// dentro de un script, pero que el jugador si ve. Si se anade uno
        /// nuevo en codigo, hay que anadirlo aqui para que entre en el CSV.
        /// </summary>
        private static readonly (string Fichero, string Uso, string Texto)[] LiteralesEnCodigo =
        {
            ("GameManager.cs",        "Objetivo de mision",   "VE A HABLAR CON LIN DE NUEVO"),
            ("InventorySystem.cs",    "Etiqueta de interfaz", "Vacío"),
            ("BacklogManager.cs",     "Etiqueta de interfaz", "Conversación con: "),
            ("ArrowPuzzleTrigger.cs", "Mensaje de puzle",     "COMPLETADO"),
            ("FNFGameManager.cs",     "Marcador de puzle",    "PUNTOS: {0}/{1}"),
        };

        private class Entrada
        {
            public string Clave;
            public string Es;
            public bool TieneClavePropia;
            public readonly SortedSet<string> Usos = new SortedSet<string>(StringComparer.Ordinal);
            public readonly SortedSet<string> Contextos = new SortedSet<string>(StringComparer.Ordinal);
        }

        [MenuItem("Tools/Localizacion/Extraer textos a CSV", priority = 10)]
        public static void Extraer()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[Localizacion] Extraccion cancelada.");
                return;
            }

            string escenaAbierta = SceneManager.GetActiveScene().path;
            var entradas = new Dictionary<string, Entrada>(StringComparer.Ordinal);

            string[] rutas = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !EstaExcluida(p))
                .OrderBy(p => p).ToArray();

            try
            {
                for (int i = 0; i < rutas.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Extrayendo textos",
                        Path.GetFileName(rutas[i]), (float)i / rutas.Length);
                    Scene escena = EditorSceneManager.OpenScene(rutas[i], OpenSceneMode.Single);
                    foreach (GameObject raiz in escena.GetRootGameObjects())
                        foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                            RecorrerComponente(mb, escena.name, entradas);
                }

                EditorUtility.DisplayProgressBar("Extrayendo textos", "Prefabs", 0.9f);
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                {
                    string ruta = AssetDatabase.GUIDToAssetPath(guid);
                    if (EstaExcluida(ruta)) continue;
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                    if (prefab == null) continue;
                    foreach (MonoBehaviour mb in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                        RecorrerComponente(mb, "Prefabs/" + Path.GetFileNameWithoutExtension(ruta), entradas);
                }

                EditorUtility.DisplayProgressBar("Extrayendo textos", "Notas de lore", 0.95f);
                RecorrerNotasSueltas(entradas);

                foreach (var lit in LiteralesEnCodigo)
                    Anadir(entradas, Loc.Clave(lit.Texto), lit.Texto, lit.Uso,
                           "Codigo:" + lit.Fichero, false);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(escenaAbierta))
                    EditorSceneManager.OpenScene(escenaAbierta, OpenSceneMode.Single);
            }

            EscribirCsv(entradas);
            PrevisualizacionIdioma.InvalidarCache();
        }

        private static void RecorrerComponente(MonoBehaviour mb, string origenNombre,
                                               Dictionary<string, Entrada> entradas)
        {
            if (mb == null) return;

            var so = new SerializedObject(mb);
            SerializedProperty p = so.GetIterator();
            bool entrar = true;

            // 1. Campos con clave propia: mandan ellos.
            var conClave = new HashSet<string>(StringComparer.Ordinal);
            while (p.NextVisible(entrar))
            {
                entrar = true;
                if (p.propertyType != SerializedPropertyType.String) continue;
                if (CamposLocalizables.NombreDeCampo(p.propertyPath) != "locKey") continue;

                SerializedProperty origen = CamposLocalizables.BuscarOrigen(p);
                if (origen == null) continue;
                string texto = CamposLocalizables.LeerTexto(origen);
                if (!CamposLocalizables.EsTraducible(texto)) continue;

                conClave.Add(origen.propertyPath);
                string clave = string.IsNullOrEmpty(p.stringValue) ? Loc.Clave(texto) : p.stringValue;
                Anadir(entradas, clave, texto, CamposLocalizables.UsoDe(origen),
                       $"{origenNombre}:{RutaObjeto(mb.transform)}", !string.IsNullOrEmpty(p.stringValue));
            }

            // 2. El resto de texto visible, identificado por su contenido.
            p = so.GetIterator();
            entrar = true;
            while (p.NextVisible(entrar))
            {
                entrar = true;
                if (p.propertyType != SerializedPropertyType.String) continue;
                if (conClave.Contains(p.propertyPath)) continue;

                string campo = CamposLocalizables.NombreDeCampo(p.propertyPath);
                if (!CamposLocalizables.Visibles.TryGetValue(campo, out string uso)) continue;
                if (!CamposLocalizables.EsTraducible(p.stringValue)) continue;

                Anadir(entradas, Loc.Clave(p.stringValue), p.stringValue, uso,
                       $"{origenNombre}:{RutaObjeto(mb.transform)}", false);
            }
        }

        /// <summary>
        /// Notas de lore que no cuelgan de ningun LoreNoteSystem. Las que si
        /// cuelgan ya han entrado con su clave por el camino normal.
        /// </summary>
        private static void RecorrerNotasSueltas(Dictionary<string, Entrada> entradas)
        {
            if (!Directory.Exists(CarpetaNotas)) return;
            foreach (string ruta in Directory.GetFiles(CarpetaNotas, "*.txt").OrderBy(x => x))
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ruta.Replace('\\', '/'));
                if (asset == null || !CamposLocalizables.EsTraducible(asset.text)) continue;
                string clave = Loc.Clave(asset.text);
                if (entradas.ContainsKey(clave)) continue;
                bool yaEsta = entradas.Values.Any(e => e.Es == Loc.Normalizar(asset.text));
                if (yaEsta) continue;
                Anadir(entradas, clave, asset.text, CamposLocalizables.UsoNota,
                       "Notas:" + Path.GetFileName(ruta), false);
            }
        }

        private static void Anadir(Dictionary<string, Entrada> entradas, string clave, string texto,
                                   string uso, string contexto, bool clavePropia)
        {
            string normalizado = Loc.Normalizar(texto);
            if (!entradas.TryGetValue(clave, out Entrada e))
            {
                e = new Entrada { Clave = clave, Es = normalizado, TieneClavePropia = clavePropia };
                entradas[clave] = e;
            }
            else if (clavePropia && e.Es != normalizado)
            {
                // Dos sitios distintos comparten clave pero ya no comparten
                // texto. Pasa si alguien duplico un objeto y edito solo uno.
                Debug.LogWarning($"[Localizacion] La clave {clave} aparece con dos textos distintos. " +
                                 $"Revisa {contexto}. Se queda el primero:\n  A: {e.Es}\n  B: {normalizado}");
            }
            e.TieneClavePropia |= clavePropia;
            e.Usos.Add(uso);
            e.Contextos.Add(contexto);
        }

        private static string RutaObjeto(Transform t)
        {
            var sb = new StringBuilder(t.name);
            while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
            return sb.ToString();
        }

        private static void EscribirCsv(Dictionary<string, Entrada> entradas)
        {
            var idiomas = new List<string>();
            var previasPorClave = new Dictionary<string, string[]>(StringComparer.Ordinal);
            var previasPorTexto = new Dictionary<string, string[]>(StringComparer.Ordinal);
            var esPrevio = new Dictionary<string, string>(StringComparer.Ordinal);

            if (File.Exists(RutaCsv))
            {
                var filas = CsvLector.Leer(File.ReadAllText(RutaCsv, Encoding.UTF8));
                if (filas.Count > 0)
                {
                    string[] cab = filas[0];
                    var cols = new List<int>();
                    for (int c = 0; c < cab.Length; c++)
                    {
                        string n = cab[c].Trim();
                        if (n == "key" || n == "contexto" || n == "uso" || n == "es") continue;
                        idiomas.Add(n); cols.Add(c);
                    }
                    int colKey = Array.FindIndex(cab, x => x.Trim() == "key");
                    int colEs = Array.FindIndex(cab, x => x.Trim() == "es");

                    for (int f = 1; f < filas.Count; f++)
                    {
                        string[] fila = filas[f];
                        var vals = new string[cols.Count];
                        for (int k = 0; k < cols.Count; k++)
                            vals[k] = cols[k] < fila.Length ? fila[cols[k]] : string.Empty;

                        string clave = colKey >= 0 && colKey < fila.Length ? fila[colKey].Trim() : null;
                        string es = colEs >= 0 && colEs < fila.Length ? Loc.Normalizar(fila[colEs]) : null;

                        if (!string.IsNullOrEmpty(clave))
                        {
                            previasPorClave[clave] = vals;
                            if (!string.IsNullOrEmpty(es)) esPrevio[clave] = es;
                        }
                        if (!string.IsNullOrEmpty(es) && !previasPorTexto.ContainsKey(es))
                            previasPorTexto[es] = vals;
                    }
                }
            }
            if (idiomas.Count == 0) idiomas.Add("en");

            var huerfanas = previasPorClave.Keys.Where(k => !entradas.ContainsKey(k)).ToList();

            Directory.CreateDirectory(Path.GetDirectoryName(RutaCsv));
            var sb = new StringBuilder();
            sb.Append(string.Join(",", new[] { "key", "contexto", "uso", "es" }
                .Concat(idiomas).Select(CsvLector.Escapar))).Append("\r\n");

            int conservadas = 0, propagadas = 0, reescritas = 0;

            foreach (Entrada e in entradas.Values.OrderBy(x => x.Contextos.First(), StringComparer.Ordinal))
            {
                string contexto = e.Contextos.First();
                if (e.Contextos.Count > 1) contexto += $" (+{e.Contextos.Count - 1})";

                sb.Append(CsvLector.Escapar(e.Clave)).Append(',');
                sb.Append(CsvLector.Escapar(contexto)).Append(',');
                sb.Append(CsvLector.Escapar(string.Join(" / ", e.Usos))).Append(',');
                sb.Append(CsvLector.Escapar(e.Es));

                // 1. Por clave: lo normal, y lo que permite reescribir el espanol.
                previasPorClave.TryGetValue(e.Clave, out string[] vals);
                if (vals != null)
                {
                    if (esPrevio.TryGetValue(e.Clave, out string antes) && antes != e.Es) reescritas++;
                }
                else if (previasPorTexto.TryGetValue(e.Es, out string[] gemelas))
                {
                    // 2. Clave nueva con texto identico a otra fila: se hereda
                    //    su traduccion, que es lo que evita tener que traducir
                    //    otra vez las repeticiones.
                    vals = gemelas;
                    propagadas++;
                }

                for (int i = 0; i < idiomas.Count; i++)
                {
                    string v = vals != null && i < vals.Length ? vals[i] : string.Empty;
                    if (!string.IsNullOrWhiteSpace(v)) conservadas++;
                    sb.Append(',').Append(CsvLector.Escapar(v));
                }
                sb.Append("\r\n");
            }

            foreach (string clave in huerfanas)
            {
                sb.Append(CsvLector.Escapar(clave)).Append(',');
                sb.Append(CsvLector.Escapar("HUERFANA - ya no aparece en el proyecto")).Append(',');
                sb.Append(CsvLector.Escapar("Revisar")).Append(',');
                sb.Append(CsvLector.Escapar(esPrevio.TryGetValue(clave, out string es) ? es : string.Empty));
                string[] vals = previasPorClave[clave];
                for (int i = 0; i < idiomas.Count; i++)
                    sb.Append(',').Append(CsvLector.Escapar(i < vals.Length ? vals[i] : string.Empty));
                sb.Append("\r\n");
            }

            File.WriteAllText(RutaCsv, sb.ToString(), new UTF8Encoding(true));
            AssetDatabase.Refresh();

            int conClave = entradas.Values.Count(e => e.TieneClavePropia);
            string resumen =
                $"{entradas.Count} cadenas ({conClave} con clave propia)\n" +
                $"{conservadas} traducciones conservadas\n" +
                $"{propagadas} heredadas de una cadena identica\n" +
                $"{reescritas} con el espanol reescrito, traduccion intacta\n" +
                $"{huerfanas.Count} huerfanas";

            Debug.Log("[Localizacion] " + resumen.Replace("\n", ". ") + $"\nEscrito en {RutaCsv}");
            EditorUtility.DisplayDialog("Extraccion terminada", resumen + "\n\n" + RutaCsv, "Vale");
        }

        [MenuItem("Tools/Localizacion/Abrir CSV maestro", priority = 11)]
        public static void AbrirCsv()
        {
            if (File.Exists(RutaCsv)) EditorUtility.OpenWithDefaultApp(RutaCsv);
            else EditorUtility.DisplayDialog("Localizacion",
                "Todavia no existe el CSV. Pasa primero el extractor.", "Vale");
        }

        [MenuItem("Tools/Localizacion/Informe de cobertura", priority = 12)]
        public static void Informe()
        {
            if (!File.Exists(RutaCsv))
            {
                EditorUtility.DisplayDialog("Localizacion", "Todavia no existe el CSV.", "Vale");
                return;
            }

            var filas = CsvLector.Leer(File.ReadAllText(RutaCsv, Encoding.UTF8));
            if (filas.Count < 2) return;

            string[] cab = filas[0];
            var sb = new StringBuilder("Cobertura de traduccion\n\n");
            int sinClave = 0;
            int colKey = Array.FindIndex(cab, x => x.Trim() == "key");

            for (int c = 0; c < cab.Length; c++)
            {
                string n = cab[c].Trim();
                if (n == "key" || n == "contexto" || n == "uso" || n == "es") continue;
                int hechas = 0, total = 0;
                for (int f = 1; f < filas.Count; f++)
                {
                    if (filas[f].Length <= c) continue;
                    total++;
                    if (!string.IsNullOrWhiteSpace(filas[f][c])) hechas++;
                }
                sb.AppendLine($"{n}: {hechas}/{total}  ({(total > 0 ? 100f * hechas / total : 0f):0.0}%)");
            }

            for (int f = 1; f < filas.Count; f++)
                if (colKey < 0 || colKey >= filas[f].Length || string.IsNullOrWhiteSpace(filas[f][colKey]))
                    sinClave++;
            if (sinClave > 0) sb.AppendLine($"\nFilas sin clave: {sinClave}");

            Debug.Log("[Localizacion] " + sb);
            EditorUtility.DisplayDialog("Cobertura de traduccion", sb.ToString(), "Vale");
        }
    }
}
