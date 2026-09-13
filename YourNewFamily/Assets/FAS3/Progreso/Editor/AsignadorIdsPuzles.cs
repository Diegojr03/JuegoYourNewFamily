using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YNF.Progreso.EditorTools
{
    /// <summary>
    /// Rellena el campo progresoId de todos los puzles de las escenas.
    ///
    /// Esto lo hace Unity, no un script externo, y el motivo importa: si se
    /// escribe el .unity desde fuera mientras el editor tiene la escena
    /// cargada en memoria, el siguiente guardado de Unity se lleva por delante
    /// lo escrito. Haciendolo desde aqui, el editor es el unico que toca el
    /// fichero y no hay pelea posible.
    ///
    /// QUE SE CONSIDERA UN PUZLE
    /// Cualquier MonoBehaviour que declare un campo publico "string progresoId".
    /// Ese campo es el gate, no una lista de nombres de clase: la version
    /// anterior comparaba mb.GetType().Name contra un diccionario y dos puzles
    /// se colaban en silencio porque el fichero y la clase no se llaman igual
    /// (SemaforoPuzzle2D.cs declara SemaforoPuzzle2DCompleto, y
    /// PuzleBotonesMusicales.cs declara PuzzleBotonesMusicales). Sin id no hay
    /// registro de evento, y sin evento el puzle reaparece al recargar.
    /// Ahora un puzle nuevo, o uno renombrado, entra solo.
    ///
    /// El id se deriva del nombre del objeto, asi que es estable entre
    /// ejecuciones: pasarlo dos veces da el mismo resultado y no rompe
    /// partidas ya guardadas. Lo que ya tiene id no se toca.
    /// </summary>
    public static class AsignadorIdsPuzles
    {
        // Fragmento del nombre de la clase (normalizado) -> prefijo legible.
        // Se busca por "contiene", no por igualdad, para que sufijos y
        // prefijos (2D, Completo, Puzzle...) no rompan la correspondencia.
        // Es solo cosmetica: si nada casa, el prefijo sale del nombre de la
        // clase y el puzle se registra igual.
        private sealed class Familia
        {
            public readonly string Fragmento;
            public readonly string Prefijo;
            public Familia(string fragmento, string prefijo) { Fragmento = fragmento; Prefijo = prefijo; }
        }

        private static readonly Familia[] Familias =
        {
            new Familia("semaforo",         "semaforos"),
            new Familia("gridmanagerui",    "cajas"),
            new Familia("lock",             "candado"),
            new Familia("peluchessillas",   "peluches"),
            new Familia("4botones",         "placas"),
            new Familia("flechastipos",     "plantas"),
            new Familia("botonesmusicales", "gramola"),
            new Familia("tuberias",         "tuberias"),
            new Familia("fnfgamemanager",   "flechas"),
            new Familia("estatuas",         "estatuas"),
            new Familia("patrullero",       "patrullero"),
            new Familia("puzzlemanager",    "valvulas"),
        };

        [MenuItem("Tools/Guardado/Asignar ids a los puzles", priority = 5)]
        public static void Asignar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string escenaAbierta = SceneManager.GetActiveScene().path;
            var informe = new StringBuilder();
            int asignados = 0, yaTenian = 0;

            string[] rutas = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Replace('\\', '/').StartsWith("Assets/TextMesh Pro/"))
                .OrderBy(p => p).ToArray();

            try
            {
                for (int i = 0; i < rutas.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Asignando ids de puzle", rutas[i], (float)i / rutas.Length);
                    Scene escena = EditorSceneManager.OpenScene(rutas[i], OpenSceneMode.Single);

                    var usados = new HashSet<string>();
                    bool tocada = false;

                    // Dos vueltas: primero se apuntan los ids que ya existen en
                    // la escena, y solo despues se generan los que faltan. Asi
                    // un id nuevo nunca puede chocar con uno que ya estaba mas
                    // abajo en la jerarquia.
                    var pendientes = new List<MonoBehaviour>();

                    foreach (MonoBehaviour mb in Todos(escena))
                    {
                        if (mb == null) continue;

                        FieldInfo campo = CampoId(mb);
                        if (campo == null) continue;

                        string actual = campo.GetValue(mb) as string;
                        if (!string.IsNullOrEmpty(actual)) { usados.Add(actual); yaTenian++; }
                        else pendientes.Add(mb);
                    }

                    foreach (MonoBehaviour mb in pendientes)
                    {
                        string familia = FamiliaDe(mb.GetType().Name);
                        string id = Unico($"puzzle_{familia}_{Limpia(mb.gameObject.name)}", usados);
                        Undo.RecordObject(mb, "Asignar progresoId");
                        CampoId(mb).SetValue(mb, id);
                        EditorUtility.SetDirty(mb);
                        usados.Add(id);
                        asignados++;
                        tocada = true;
                        informe.AppendLine($"  {escena.name}/{mb.gameObject.name} " +
                                           $"({mb.GetType().Name})  ->  {id}");
                    }

                    foreach (string aviso in Sospechosos(escena))
                        informe.AppendLine($"  !! {escena.name}: {aviso}");

                    if (tocada) EditorSceneManager.SaveScene(escena);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(escenaAbierta))
                    EditorSceneManager.OpenScene(escenaAbierta, OpenSceneMode.Single);
            }

            Debug.Log($"[Progreso] Ids de puzle asignados: {asignados}. Ya tenian: {yaTenian}.\n{informe}");
            EditorUtility.DisplayDialog("Ids de puzle",
                $"Asignados: {asignados}\nYa tenian id: {yaTenian}\n\nEl detalle esta en la consola.", "Vale");
        }

        /// <summary>
        /// Scripts que huelen a puzle (el nombre encaja con alguna familia
        /// conocida) pero no declaran progresoId. Es el aviso que faltaba: un
        /// puzle sin campo no se puede registrar, y antes eso no se veia.
        /// </summary>
        private static IEnumerable<string> Sospechosos(Scene escena)
        {
            var dichos = new HashSet<string>();
            foreach (MonoBehaviour mb in Todos(escena))
            {
                if (mb == null) continue;
                var tipo = mb.GetType();
                if (CampoId(mb) != null) continue;

                string normal = Normal(tipo.Name);
                bool pareceP = Familias.Any(f => normal.Contains(f.Fragmento))
                               || normal.Contains("puzzle") || normal.Contains("puzle");
                if (!pareceP || !dichos.Add(tipo.Name)) continue;

                yield return $"{tipo.Name} parece un puzle pero no tiene campo progresoId " +
                             "(objeto: " + mb.gameObject.name + "). No se podra guardar su progreso.";
            }
        }

        /// <summary>
        /// El campo publico "string progresoId", o null si el script no lo
        /// declara. Es lo unico que decide si un componente es un puzle.
        /// </summary>
        private static FieldInfo CampoId(MonoBehaviour mb)
        {
            FieldInfo f = mb.GetType().GetField("progresoId",
                BindingFlags.Public | BindingFlags.Instance);
            return (f != null && f.FieldType == typeof(string)) ? f : null;
        }

        private static string FamiliaDe(string nombreClase)
        {
            string normal = Normal(nombreClase);
            foreach (Familia f in Familias)
                if (normal.Contains(f.Fragmento)) return f.Prefijo;

            // Sin correspondencia: se usa el nombre de la clase sin el ruido
            // habitual. Feo pero unico y estable, que es lo que importa.
            string limpio = normal
                .Replace("puzzle", "").Replace("puzle", "")
                .Replace("manager", "").Replace("completo", "")
                .Replace("2d", "");
            return string.IsNullOrEmpty(limpio) ? normal : limpio;
        }

        private static string Normal(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s)
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        private static IEnumerable<MonoBehaviour> Todos(Scene escena)
        {
            foreach (GameObject raiz in escena.GetRootGameObjects())
                foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                    yield return mb;
        }

        private static string Limpia(string nombre)
        {
            var sb = new StringBuilder();
            foreach (char c in nombre)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
                else if (sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
            }
            return sb.ToString().Trim('_');
        }

        private static string Unico(string baseId, HashSet<string> usados)
        {
            if (!usados.Contains(baseId)) return baseId;
            for (int i = 2; ; i++)
            {
                string intento = $"{baseId}_{i}";
                if (!usados.Contains(intento)) return intento;
            }
        }
    }
}
