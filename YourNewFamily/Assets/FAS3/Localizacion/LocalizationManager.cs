using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YNF.Localizacion
{
    /// <summary>
    /// Nucleo del sistema de localizacion de Your New Family.
    ///
    /// Carga el CSV maestro de StreamingAssets/Localization/Localization.csv y
    /// sirve las traducciones a traves de Loc.T().
    ///
    /// La busqueda va primero por clave estable y, si no la encuentra, por el
    /// propio texto espanol. Ese segundo camino es la red de seguridad: cubre
    /// lo que todavia no lleva clave y evita que una clave mal asignada deje
    /// una linea sin traducir.
    ///
    /// No hace falta arrastrarlo a ninguna escena: se crea solo antes de que
    /// cargue la primera escena.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public const string IdiomaOrigen = "es";
        public const string RutaRelativa = "Localization/Localization.csv";
        private const string ClavePrefs = "ynf_idioma";

        // Idiomas que el juego ofrece. El orden es el del desplegable de opciones.
        // Para anadir uno: se anade aqui y se anade su columna al CSV. Nada mas.
        public static readonly IdiomaInfo[] IdiomasDisponibles =
        {
            new IdiomaInfo("es", "Espanol", "Espanol", SystemLanguage.Spanish),
            new IdiomaInfo("en", "Ingles",  "English", SystemLanguage.English),
        };

        public struct IdiomaInfo
        {
            public readonly string Codigo;        // columna del CSV: "es", "en", ...
            public readonly string NombreEnEspanol;
            public readonly string NombreNativo;  // lo que ve el jugador en el desplegable
            public readonly SystemLanguage IdiomaUnity;

            public IdiomaInfo(string codigo, string nombreEnEspanol, string nombreNativo, SystemLanguage idiomaUnity)
            {
                Codigo = codigo;
                NombreEnEspanol = nombreEnEspanol;
                NombreNativo = nombreNativo;
                IdiomaUnity = idiomaUnity;
            }
        }

        /// <summary>
        /// Se dispara cuando cambia el idioma. Todo lo que ya este pintado en
        /// pantalla debe volver a pintarse al recibirlo (lo hace LocalizedTMP
        /// por su cuenta; los paneles abiertos se refrescan solos).
        /// </summary>
        public static event Action OnIdiomaCambiado;

        private static LocalizationManager _instancia;
        public static LocalizationManager Instancia
        {
            get
            {
                if (_instancia == null) Crear();
                return _instancia;
            }
        }

        private string _idiomaActual = IdiomaOrigen;
        public static string IdiomaActual => _instancia != null ? _instancia._idiomaActual : IdiomaOrigen;

        private TablaLocalizacion _tabla;

        /// <summary>Ruta absoluta del CSV. La comparten juego y herramientas del editor.</summary>
        public static string RutaCsvAbsoluta =>
            Path.Combine(Application.streamingAssetsPath, RutaRelativa);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Cadenas que llegaron a pantalla sin traduccion. Solo en editor y en
        // builds de desarrollo: sirve para cazar texto que se escapo del CSV.
        private static readonly HashSet<string> _sinTraducir = new HashSet<string>(StringComparer.Ordinal);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Arranque() => Crear();

        private static void Crear()
        {
            if (_instancia != null) return;
            var go = new GameObject("[LocalizationManager]");
            _instancia = go.AddComponent<LocalizationManager>();
            DontDestroyOnLoad(go);
            _instancia.Inicializar();
        }

        private void Inicializar()
        {
            _tabla = TablaLocalizacion.Cargar(RutaCsvAbsoluta);
            if (!string.IsNullOrEmpty(_tabla.Error))
                Debug.LogWarning($"[Localizacion] {_tabla.Error} El juego se queda en espanol.");
            else
                Debug.Log($"[Localizacion] Tabla cargada: {_tabla.Filas} filas, idiomas: {string.Join(", ", _tabla.Idiomas)}.");

            string guardado = PlayerPrefs.GetString(ClavePrefs, string.Empty);
            if (string.IsNullOrEmpty(guardado)) guardado = DetectarIdiomaDelSistema();
            AplicarIdioma(guardado, avisar: false);
        }

        /// <summary>
        /// Primera ejecucion: si el sistema esta en un idioma que el juego
        /// soporta, se arranca en ese idioma. Si no, en espanol.
        /// </summary>
        private static string DetectarIdiomaDelSistema()
        {
            foreach (var idioma in IdiomasDisponibles)
                if (idioma.IdiomaUnity == Application.systemLanguage) return idioma.Codigo;
            return IdiomaOrigen;
        }

        /// <summary>
        /// Cambia el idioma activo y avisa a la interfaz para que se repinte.
        /// </summary>
        public static void CambiarIdioma(string codigo) => Instancia.AplicarIdioma(codigo, avisar: true);

        private void AplicarIdioma(string codigo, bool avisar)
        {
            if (string.IsNullOrEmpty(codigo)) codigo = IdiomaOrigen;

            bool soportado = false;
            foreach (var idioma in IdiomasDisponibles)
                if (string.Equals(idioma.Codigo, codigo, StringComparison.OrdinalIgnoreCase)) { soportado = true; break; }

            if (!soportado)
            {
                Debug.LogWarning($"[Localizacion] Idioma '{codigo}' no soportado. Se usa {IdiomaOrigen}.");
                codigo = IdiomaOrigen;
            }

            _idiomaActual = codigo;
            PlayerPrefs.SetString(ClavePrefs, codigo);
            PlayerPrefs.Save();

            if (avisar)
            {
                try { OnIdiomaCambiado?.Invoke(); }
                catch (Exception e) { Debug.LogError($"[Localizacion] Error repintando tras el cambio de idioma: {e}"); }
            }
        }

        /// <summary>
        /// Busqueda propiamente dicha. Se llama mucho, asi que la ruta rapida
        /// (idioma espanol) sale sin tocar ningun diccionario.
        /// </summary>
        internal static string Traducir(string clave, string origen)
        {
            var m = _instancia;
            if (m == null) { Crear(); m = _instancia; }
            if (m == null || m._idiomaActual == IdiomaOrigen || m._tabla == null) return origen;

            string traduccion = m._tabla.Buscar(m._idiomaActual, clave, origen);
            if (traduccion != null) return traduccion;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string normalizado = Loc.Normalizar(origen);
            if (!string.IsNullOrWhiteSpace(normalizado) && _sinTraducir.Add(normalizado))
            {
                string id = string.IsNullOrEmpty(clave) ? "sin clave" : clave;
                Debug.LogWarning($"[Localizacion] Sin traducir ({m._idiomaActual}) [{id}]: \"{Recortar(normalizado)}\"");
            }
#endif
            return origen;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static string Recortar(string s) => s.Length <= 60 ? s : s.Substring(0, 60) + "...";

        /// <summary>
        /// Vuelca a disco las cadenas que aparecieron en pantalla sin traduccion.
        /// Util despues de una pasada de QA: se juega el capitulo en ingles y
        /// este fichero dice exactamente que falto.
        /// </summary>
        public static string VolcarSinTraducir()
        {
            string ruta = Path.Combine(Application.persistentDataPath, $"sin_traducir_{IdiomaActual}.csv");
            using (var w = new StreamWriter(ruta, false, new System.Text.UTF8Encoding(true)))
            {
                w.WriteLine("key,es");
                foreach (string s in _sinTraducir)
                    w.WriteLine($"{CsvLector.Escapar(Loc.Clave(s))},{CsvLector.Escapar(s)}");
            }
            Debug.Log($"[Localizacion] {_sinTraducir.Count} cadenas sin traducir volcadas en {ruta}");
            return ruta;
        }
#endif
    }
}
