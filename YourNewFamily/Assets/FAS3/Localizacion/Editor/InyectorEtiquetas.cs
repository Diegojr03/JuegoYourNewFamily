using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YNF.Localizacion.EditorTools
{
    /// <summary>
    /// Anade el componente LocalizedTMP a las etiquetas de texto fijas de una
    /// escena: titulos, botones, creditos, cabeceras de opciones.
    ///
    /// La parte delicada es no tocar las etiquetas que ya pinta un script.
    /// Si a la caja de dialogo se le pone LocalizedTMP, el componente le
    /// escribiria encima el texto de origen cada vez que se activa y se
    /// pelearia con el efecto de maquina de escribir. Por eso, antes de
    /// inyectar nada, la herramienta busca todos los campos de tipo TMP_Text
    /// de todos los scripts de la escena y excluye las etiquetas referenciadas.
    /// </summary>
    public static class InyectorEtiquetas
    {
        [MenuItem("Tools/Localizacion/Preparar etiquetas de la escena abierta", priority = 20)]
        public static void PrepararEscenaAbierta()
        {
            Scene escena = SceneManager.GetActiveScene();
            var informe = new StringBuilder();
            int anadidos = Preparar(escena, informe);

            if (anadidos > 0) EditorSceneManager.MarkSceneDirty(escena);

            Debug.Log($"[Localizacion] {escena.name}: {anadidos} etiquetas preparadas.\n{informe}");
            EditorUtility.DisplayDialog("Etiquetas preparadas",
                $"Escena: {escena.name}\nEtiquetas preparadas: {anadidos}\n\n" +
                "Recuerda guardar la escena y volver a pasar el extractor.", "Vale");
        }

        [MenuItem("Tools/Localizacion/Preparar etiquetas de TODAS las escenas", priority = 21)]
        public static void PrepararTodas()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string escenaAbierta = SceneManager.GetActiveScene().path;
            // Se saltan las escenas de ejemplo de TextMesh Pro y demas material
            // que no es contenido del juego. Ver ExtractorLocalizacion.CarpetasExcluidas.
            string[] rutas = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !ExtractorLocalizacion.EstaExcluida(p))
                .OrderBy(p => p).ToArray();

            int total = 0;
            var informe = new StringBuilder();

            try
            {
                for (int i = 0; i < rutas.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Preparando etiquetas",
                        rutas[i], (float)i / rutas.Length);

                    Scene escena = EditorSceneManager.OpenScene(rutas[i], OpenSceneMode.Single);
                    int n = Preparar(escena, informe);
                    total += n;
                    if (n > 0) EditorSceneManager.SaveScene(escena);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(escenaAbierta))
                    EditorSceneManager.OpenScene(escenaAbierta, OpenSceneMode.Single);
            }

            Debug.Log($"[Localizacion] {total} etiquetas preparadas en {rutas.Length} escenas.\n{informe}");
            EditorUtility.DisplayDialog("Etiquetas preparadas",
                $"{total} etiquetas preparadas en {rutas.Length} escenas.\n\n" +
                "Ahora pasa el extractor para que entren en el CSV.", "Vale");
        }

        private static int Preparar(Scene escena, StringBuilder informe)
        {
            GameObject[] raices = escena.GetRootGameObjects();

            // 1. Etiquetas que algun script controla por codigo: intocables.
            HashSet<TMP_Text> gestionadasPorCodigo = EtiquetasReferenciadas(raices);

            // 2. El resto son texto fijo de interfaz.
            int anadidos = 0;
            foreach (GameObject raiz in raices)
            {
                foreach (TMP_Text etiqueta in raiz.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (gestionadasPorCodigo.Contains(etiqueta)) continue;
                    if (etiqueta.GetComponent<LocalizedTMP>() != null) continue;

                    string texto = etiqueta.text;
                    if (string.IsNullOrWhiteSpace(texto)) continue;
                    if (texto.Length < 2) continue;
                    if (!texto.Any(char.IsLetter)) continue;
                    if (texto == "New Text" || texto == "Sample Text") continue;

                    var loc = Undo.AddComponent<LocalizedTMP>(etiqueta.gameObject);
                    loc.CapturarOrigenDesdeEtiqueta();
                    EditorUtility.SetDirty(etiqueta.gameObject);

                    anadidos++;
                    informe.AppendLine($"  + {escena.name} / {etiqueta.name}: \"{Recortar(texto)}\"");
                }
            }
            return anadidos;
        }

        /// <summary>
        /// Devuelve las etiquetas TMP que estan asignadas a algun campo de algun
        /// script de la escena. Son las que pinta el codigo y no deben llevar
        /// LocalizedTMP.
        /// </summary>
        private static HashSet<TMP_Text> EtiquetasReferenciadas(GameObject[] raices)
        {
            var referenciadas = new HashSet<TMP_Text>();

            foreach (GameObject raiz in raices)
            {
                foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue;
                    if (mb is LocalizedTMP) continue;

                    var so = new SerializedObject(mb);
                    SerializedProperty p = so.GetIterator();
                    bool entrar = true;

                    while (p.NextVisible(entrar))
                    {
                        entrar = true;
                        if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (p.objectReferenceValue is TMP_Text texto) referenciadas.Add(texto);
                    }
                }
            }
            return referenciadas;
        }

        private static string Recortar(string s) => s.Length <= 45 ? s : s.Substring(0, 45) + "...";
    }
}
