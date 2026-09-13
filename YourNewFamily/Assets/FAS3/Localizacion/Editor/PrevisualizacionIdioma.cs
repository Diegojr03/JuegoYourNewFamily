using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YNF.Localizacion.EditorTools
{
    /// <summary>
    /// Idioma de previsualizacion del editor.
    ///
    /// Sirve para dos cosas: para que el inspector muestre la traduccion
    /// debajo de cada texto espanol, y para ver la escena directamente en otro
    /// idioma sin entrar en modo de juego.
    ///
    /// La previsualizacion en escena cambia el texto de las etiquetas TMP, asi
    /// que hay una red de seguridad: antes de cualquier guardado de escena se
    /// restaura el espanol y se vuelve a aplicar despues. Es imposible guardar
    /// el ingles como texto fuente por accidente.
    /// </summary>
    [InitializeOnLoad]
    public static class PrevisualizacionIdioma
    {
        private const string PrefIdioma = "ynf_loc_idioma_preview";
        private const string PrefEscena = "ynf_loc_preview_escena";

        private static TablaLocalizacion _tabla;
        private static double _cargadaEn;

        static PrevisualizacionIdioma()
        {
            EditorSceneManager.sceneSaving += AntesDeGuardar;
            EditorSceneManager.sceneSaved += DespuesDeGuardar;
            EditorApplication.playModeStateChanged += AlCambiarModoDeJuego;
        }

        public static string Idioma
        {
            get => EditorPrefs.GetString(PrefIdioma, LocalizationManager.IdiomaOrigen);
            set
            {
                EditorPrefs.SetString(PrefIdioma, value);
                if (PreviewEnEscena) AplicarEnEscena();
                else RestaurarEscena();
                EditorApplication.RepaintHierarchyWindow();
                SceneView.RepaintAll();
            }
        }

        public static bool PreviewEnEscena
        {
            get => EditorPrefs.GetBool(PrefEscena, false);
            private set => EditorPrefs.SetBool(PrefEscena, value);
        }

        /// <summary>
        /// La tabla, cacheada unos segundos. Se recarga sola al guardar el CSV,
        /// asi que traducir en la hoja de calculo y volver a Unity se ve al momento.
        /// </summary>
        public static TablaLocalizacion Tabla
        {
            get
            {
                if (_tabla == null || EditorApplication.timeSinceStartup - _cargadaEn > 3.0)
                {
                    _tabla = TablaLocalizacion.Cargar(RutaCsv());
                    _cargadaEn = EditorApplication.timeSinceStartup;
                }
                return _tabla;
            }
        }

        public static void InvalidarCache() => _tabla = null;

        private static string RutaCsv() =>
            System.IO.Path.Combine(Application.streamingAssetsPath, LocalizationManager.RutaRelativa);

        /// <summary>
        /// Traduccion para mostrar en el editor. Devuelve null si no hay.
        /// </summary>
        public static string Traducir(string clave, string origen)
        {
            if (Idioma == LocalizationManager.IdiomaOrigen) return null;
            return Tabla?.Buscar(Idioma, clave, origen);
        }

        // ------------------------------------------------------------ menus

        [MenuItem("Tools/Localizacion/Idioma de previsualizacion/Espanol (original)", priority = 40)]
        private static void PonerEspanol() => Idioma = "es";

        [MenuItem("Tools/Localizacion/Idioma de previsualizacion/Espanol (original)", true)]
        private static bool PonerEspanolCheck()
        {
            Menu.SetChecked("Tools/Localizacion/Idioma de previsualizacion/Espanol (original)", Idioma == "es");
            return true;
        }

        [MenuItem("Tools/Localizacion/Idioma de previsualizacion/English", priority = 41)]
        private static void PonerIngles() => Idioma = "en";

        [MenuItem("Tools/Localizacion/Idioma de previsualizacion/English", true)]
        private static bool PonerInglesCheck()
        {
            Menu.SetChecked("Tools/Localizacion/Idioma de previsualizacion/English", Idioma == "en");
            return true;
        }

        [MenuItem("Tools/Localizacion/Ver la escena en el idioma de previsualizacion", priority = 45)]
        private static void AlternarPreviewEscena()
        {
            PreviewEnEscena = !PreviewEnEscena;
            if (PreviewEnEscena) AplicarEnEscena();
            else RestaurarEscena();
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/Localizacion/Ver la escena en el idioma de previsualizacion", true)]
        private static bool AlternarPreviewEscenaCheck()
        {
            Menu.SetChecked("Tools/Localizacion/Ver la escena en el idioma de previsualizacion", PreviewEnEscena);
            return true;
        }

        // -------------------------------------------------- aplicar/restaurar

        private static void AplicarEnEscena()
        {
            int n = 0;
            foreach (LocalizedTMP loc in EtiquetasDeLaEscena())
            {
                string traduccion = Traducir(loc.LocKey, loc.TextoOrigen);
                loc.PintarEnEditor(string.IsNullOrEmpty(traduccion) ? loc.TextoOrigen : traduccion);
                n++;
            }
            if (n > 0) Debug.Log($"[Localizacion] Previsualizando {n} etiquetas en '{Idioma}'. " +
                                 "El espanol se restaura solo antes de cualquier guardado.");
        }

        private static void RestaurarEscena()
        {
            foreach (LocalizedTMP loc in EtiquetasDeLaEscena())
                loc.PintarEnEditor(loc.TextoOrigen);
        }

        private static IEnumerable<LocalizedTMP> EtiquetasDeLaEscena()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene escena = SceneManager.GetSceneAt(i);
                if (!escena.isLoaded) continue;
                foreach (GameObject raiz in escena.GetRootGameObjects())
                    foreach (LocalizedTMP loc in raiz.GetComponentsInChildren<LocalizedTMP>(true))
                        yield return loc;
            }
        }

        // La red de seguridad: nunca se guarda una escena con el texto traducido.
        private static void AntesDeGuardar(Scene escena, string ruta)
        {
            if (PreviewEnEscena) RestaurarEscena();
        }

        private static void DespuesDeGuardar(Scene escena)
        {
            if (PreviewEnEscena) AplicarEnEscena();
        }

        private static void AlCambiarModoDeJuego(PlayModeStateChange estado)
        {
            // En modo de juego manda el sistema de verdad, no la previsualizacion.
            if (estado == PlayModeStateChange.ExitingEditMode && PreviewEnEscena) RestaurarEscena();
        }
    }
}
