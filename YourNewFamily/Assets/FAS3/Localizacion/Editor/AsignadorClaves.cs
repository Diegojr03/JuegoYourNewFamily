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
    /// Rellena los campos locKey de todo el proyecto.
    ///
    /// Regla de asignacion, y el motivo importa:
    ///
    ///  - La PRIMERA aparicion de un texto espanol recibe la clave derivada de
    ///    ese texto, que es exactamente la que ya tiene esa cadena en el CSV.
    ///    Asi las 1.000 traducciones que ya existen siguen enganchadas sin
    ///    tocar el CSV.
    ///
    ///  - Las apariciones siguientes del mismo texto reciben una clave con
    ///    sufijo de contexto, para que cada sitio sea independiente: cambiar el
    ///    espanol en una conversacion no arrastra a la otra. El extractor les
    ///    copia la traduccion de la cadena identica, asi que no hay trabajo
    ///    extra de traduccion.
    ///
    ///  - Lo que ya tiene clave no se toca, salvo que se pida reasignar.
    /// </summary>
    public static class AsignadorClaves
    {
        [MenuItem("Tools/Localizacion/Asignar claves a los textos", priority = 30)]
        public static void Asignar() => Ejecutar(reasignarTodo: false);

        [MenuItem("Tools/Localizacion/Avanzado/Reasignar TODAS las claves", priority = 200)]
        public static void Reasignar()
        {
            if (!EditorUtility.DisplayDialog("Reasignar todas las claves",
                "Esto vuelve a calcular la clave de todos los textos, incluidos los que ya la tienen.\n\n" +
                "Solo tiene sentido si las claves se han descolocado. Despues hay que pasar el extractor " +
                "y revisar la cobertura, porque las traducciones se recasan por texto espanol.\n\n" +
                "¿Seguir?", "Reasignar", "Cancelar")) return;
            Ejecutar(reasignarTodo: true);
        }

        private static void Ejecutar(bool reasignarTodo)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string escenaAbierta = SceneManager.GetActiveScene().path;
            var usadas = new HashSet<string>(System.StringComparer.Ordinal);
            int asignadas = 0, yaTenian = 0, objetos = 0;
            var informe = new StringBuilder();

            string[] rutas = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !ExtractorLocalizacion.EstaExcluida(p))
                .OrderBy(p => p).ToArray();

            try
            {
                for (int i = 0; i < rutas.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Asignando claves", rutas[i], (float)i / (rutas.Length + 1));
                    Scene escena = EditorSceneManager.OpenScene(rutas[i], OpenSceneMode.Single);
                    bool tocada = false;

                    foreach (GameObject raiz in escena.GetRootGameObjects())
                        foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                        {
                            if (mb == null) continue;
                            objetos++;
                            if (Procesar(mb, escena.name, usadas, reasignarTodo,
                                         ref asignadas, ref yaTenian, informe)) tocada = true;
                        }

                    if (tocada) EditorSceneManager.SaveScene(escena);
                }

                EditorUtility.DisplayProgressBar("Asignando claves", "Prefabs", 0.95f);
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                {
                    string ruta = AssetDatabase.GUIDToAssetPath(guid);
                    if (ExtractorLocalizacion.EstaExcluida(ruta)) continue;
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                    if (prefab == null) continue;

                    bool tocado = false;
                    foreach (MonoBehaviour mb in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (mb == null) continue;
                        if (Procesar(mb, "Prefabs/" + Path.GetFileNameWithoutExtension(ruta),
                                     usadas, reasignarTodo, ref asignadas, ref yaTenian, informe)) tocado = true;
                    }
                    if (tocado) PrefabUtility.SavePrefabAsset(prefab);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(escenaAbierta))
                    EditorSceneManager.OpenScene(escenaAbierta, OpenSceneMode.Single);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[Localizacion] Claves asignadas: {asignadas}. Ya tenian: {yaTenian}. " +
                      $"Componentes revisados: {objetos}.\n{informe}");
            EditorUtility.DisplayDialog("Claves asignadas",
                $"Nuevas: {asignadas}\nYa tenian clave: {yaTenian}\n\n" +
                "Siguiente paso: Tools > Localizacion > Extraer textos a CSV.", "Vale");
        }

        private static bool Procesar(MonoBehaviour mb, string origenNombre, HashSet<string> usadas,
                                     bool reasignarTodo, ref int asignadas, ref int yaTenian,
                                     StringBuilder informe)
        {
            var so = new SerializedObject(mb);
            SerializedProperty p = so.GetIterator();
            bool entrar = true, cambiado = false;

            while (p.NextVisible(entrar))
            {
                entrar = true;
                if (p.propertyType != SerializedPropertyType.String) continue;
                if (CamposLocalizables.NombreDeCampo(p.propertyPath) != "locKey") continue;

                SerializedProperty origen = CamposLocalizables.BuscarOrigen(p);
                string texto = CamposLocalizables.LeerTexto(origen);
                if (!CamposLocalizables.EsTraducible(texto)) continue;

                if (!reasignarTodo && !string.IsNullOrEmpty(p.stringValue))
                {
                    usadas.Add(p.stringValue);
                    yaTenian++;
                    continue;
                }

                // La primera aparicion se queda la clave "limpia", que es la que
                // ya esta en el CSV; las siguientes llevan sufijo de contexto.
                string clave = Loc.Clave(texto);
                if (usadas.Contains(clave))
                {
                    string contexto = $"{origenNombre}:{RutaObjeto(mb.transform)}:{p.propertyPath}";
                    clave = Loc.Clave(texto, contexto);
                    int intento = 2;
                    while (usadas.Contains(clave))
                        clave = Loc.Clave(texto, contexto + "#" + intento++);
                }

                p.stringValue = clave;
                usadas.Add(clave);
                asignadas++;
                cambiado = true;
                if (informe.Length < 8000)
                    informe.AppendLine($"  {clave}  {origenNombre}/{mb.name}  \"{Recortar(texto)}\"");
            }

            if (cambiado)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mb);
            }
            return cambiado;
        }

        private static string RutaObjeto(Transform t)
        {
            var sb = new StringBuilder(t.name);
            while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
            return sb.ToString();
        }

        private static string Recortar(string s) => s.Length <= 50 ? s : s.Substring(0, 50) + "...";
    }
}
