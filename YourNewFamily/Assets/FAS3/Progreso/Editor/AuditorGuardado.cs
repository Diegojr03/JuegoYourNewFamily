using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YNF.Progreso;

namespace YNF.Progreso.EditorTools
{
    /// <summary>
    /// Revisa la salud del sistema de guardado en las escenas del juego.
    ///
    /// Busca las dos cosas que rompen una partida en silencio:
    ///
    ///  - objectId repetidos. Pasa al duplicar un objeto en el editor, porque
    ///    SaveableObject.Reset() solo genera id al anadir el componente, no al
    ///    copiarlo. Dos objetos con el mismo id comparten estado: al guardar,
    ///    el ultimo que se procese decide por los dos, y si uno estaba activo y
    ///    el otro no, uno de los dos se pierde al cargar.
    ///
    ///  - eventos con consecuencias pero sin id. Sin id no se registra el
    ///    progreso, asi que sus consecuencias no se pueden reconstruir y solo
    ///    sobreviven si la foto del mundo las pillo en el momento justo.
    /// </summary>
    public static class AuditorGuardado
    {
        [MenuItem("Tools/Guardado/Auditar escenas", priority = 10)]
        public static void Auditar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string escenaAbierta = SceneManager.GetActiveScene().path;
            var informe = new StringBuilder();
            int totalDuplicados = 0, totalSinId = 0, totalVacios = 0;

            string[] rutas = RutasDeEscena();
            try
            {
                for (int i = 0; i < rutas.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Auditando guardado", rutas[i], (float)i / rutas.Length);
                    Scene escena = EditorSceneManager.OpenScene(rutas[i], OpenSceneMode.Single);

                    var porId = new Dictionary<string, List<SaveableObject>>();
                    int vacios = 0;
                    foreach (SaveableObject so in TodosLosSaveables(escena))
                    {
                        if (string.IsNullOrEmpty(so.objectId)) { vacios++; continue; }
                        if (!porId.TryGetValue(so.objectId, out var lista))
                            porId[so.objectId] = lista = new List<SaveableObject>();
                        lista.Add(so);
                    }

                    var duplicados = porId.Where(kv => kv.Value.Count > 1).ToList();
                    int sinId = ContarEventosSinId(escena);

                    totalDuplicados += duplicados.Sum(kv => kv.Value.Count);
                    totalSinId += sinId;
                    totalVacios += vacios;

                    informe.AppendLine($"\n### {escena.name}");
                    informe.AppendLine($"   objetos guardables : {porId.Values.Sum(l => l.Count) + vacios}");
                    informe.AppendLine($"   ids repetidos      : {duplicados.Count} ids, " +
                                       $"{duplicados.Sum(kv => kv.Value.Count)} objetos");
                    informe.AppendLine($"   ids vacios         : {vacios}");
                    informe.AppendLine($"   eventos sin id     : {sinId}");

                    foreach (var kv in duplicados.Take(10))
                    {
                        informe.AppendLine($"     {kv.Key}");
                        foreach (SaveableObject so in kv.Value)
                            informe.AppendLine($"        - {Ruta(so.transform)}  activo={so.gameObject.activeSelf}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(escenaAbierta))
                    EditorSceneManager.OpenScene(escenaAbierta, OpenSceneMode.Single);
            }

            string resumen = $"Objetos con id repetido: {totalDuplicados}\n" +
                             $"Objetos con id vacio: {totalVacios}\n" +
                             $"Eventos con consecuencias y sin id: {totalSinId}";
            Debug.Log("[Guardado] Auditoria\n" + resumen + informe);
            EditorUtility.DisplayDialog("Auditoria del guardado",
                resumen + "\n\nEl detalle esta en la consola.", "Vale");
        }

        [MenuItem("Tools/Guardado/Arreglar ids repetidos", priority = 11)]
        public static void ArreglarDuplicados()
        {
            if (!EditorUtility.DisplayDialog("Arreglar ids repetidos",
                "A cada grupo de objetos que comparten objectId se le deja el id al PRIMERO y se " +
                "genera uno nuevo para el resto.\n\n" +
                "Se respeta el primero a proposito: asi las partidas ya guardadas siguen " +
                "reconociendo al menos a uno de ellos.\n\n¿Seguir?", "Arreglar", "Cancelar")) return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string escenaAbierta = SceneManager.GetActiveScene().path;
            int regenerados = 0;
            var informe = new StringBuilder();

            try
            {
                foreach (string ruta in RutasDeEscena())
                {
                    Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                    var vistos = new HashSet<string>();
                    bool tocada = false;

                    foreach (SaveableObject so in TodosLosSaveables(escena))
                    {
                        string id = so.objectId;
                        if (string.IsNullOrEmpty(id) || vistos.Contains(id))
                        {
                            string nuevo = System.Guid.NewGuid().ToString();
                            informe.AppendLine($"  {escena.name}/{Ruta(so.transform)}: {id} -> {nuevo}");
                            Undo.RecordObject(so, "Regenerar objectId");
                            so.objectId = nuevo;
                            EditorUtility.SetDirty(so);
                            vistos.Add(nuevo);
                            regenerados++;
                            tocada = true;
                        }
                        else vistos.Add(id);
                    }

                    if (tocada) EditorSceneManager.SaveScene(escena);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(escenaAbierta))
                    EditorSceneManager.OpenScene(escenaAbierta, OpenSceneMode.Single);
            }

            Debug.Log($"[Guardado] {regenerados} ids regenerados.\n{informe}");
            EditorUtility.DisplayDialog("Ids arreglados",
                $"{regenerados} objectId regenerados.\n\nEl detalle esta en la consola.", "Vale");
        }

        private static string[] RutasDeEscena() =>
            AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Replace('\\', '/').StartsWith("Assets/TextMesh Pro/"))
                .OrderBy(p => p).ToArray();

        private static IEnumerable<SaveableObject> TodosLosSaveables(Scene escena)
        {
            foreach (GameObject raiz in escena.GetRootGameObjects())
                foreach (SaveableObject so in raiz.GetComponentsInChildren<SaveableObject>(true))
                    yield return so;
        }

        private static int ContarEventosSinId(Scene escena)
        {
            int n = 0;
            foreach (GameObject raiz in escena.GetRootGameObjects())
                foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue;
                    // Antes solo se miraban dos nombres de campo concretos, asi
                    // que los puzles que bautizaron su lista de otra manera
                    // (valvulas, estatuas, laberinto) nunca salian en la
                    // auditoria aunque no tuviesen id. Ahora se pregunta al
                    // reproductor, que conoce todas las convenciones.
                    if (!ReproductorDeProgreso.TieneConsecuencias(mb)) continue;
                    if (string.IsNullOrEmpty(ReproductorDeProgreso.LeerId(mb))) n++;
                }
            return n;
        }

        private static string Ruta(Transform t)
        {
            var sb = new StringBuilder(t.name);
            while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
            return sb.ToString();
        }
    }
}
