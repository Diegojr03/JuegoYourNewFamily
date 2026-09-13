using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace YNF.Progreso
{
    /// <summary>
    /// Reconstruye el mundo a partir del progreso del jugador.
    ///
    /// EL PROBLEMA QUE RESUELVE
    /// El guardado antiguo almacenaba una foto del mundo: que objetos estaban
    /// activos y cuales destruidos. Pero el estado del mundo es una
    /// consecuencia, no una causa. Si una secuencia se aplicaba a medias (por
    /// ejemplo, un dialogo que destruye una conversacion y enciende otra, y
    /// solo se llega a grabar la mitad), esa mitad quedaba grabada para
    /// siempre y la partida se volvia incompletable.
    ///
    /// LO QUE HACE ESTE REPRODUCTOR
    /// Toma la lista ordenada de eventos completados (dialogos, recorridos,
    /// puzles, notas) y vuelve a aplicar las consecuencias que cada uno
    /// declara en la escena. El estado del mundo pasa a ser una funcion del
    /// progreso, asi que un estado a medias deja de poder existir.
    ///
    /// POR QUE POR REFLEXION Y NO POR INTERFAZ
    /// Los trece scripts que declaran consecuencias ya usan los mismos nombres
    /// de campo. Leerlos por reflexion evita tocar esos trece ficheros, que es
    /// justo donde vive la logica del juego y donde menos conviene meter mano,
    /// y hace que cualquier script futuro que siga la misma convencion entre
    /// solo. Se ejecuta dos veces por carga de escena, no por fotograma, asi
    /// que el coste de la reflexion es irrelevante.
    /// </summary>
    public static class ReproductorDeProgreso
    {
        // Campos que identifican el evento. Se usa el primero no vacio.
        private static readonly string[] CamposId =
        {
            "progresoId", "dialogueId", "triggerId", "noteId",
        };

        // Cada script del juego bautizo su lista de consecuencias a su manera.
        // En vez de unificarlos (tocar trece ficheros de logica), el
        // reproductor conoce todos los nombres. Cualquiera de estos campos
        // vale como GameObject suelto o como GameObject[].
        private static readonly string[] CamposActivar =
        {
            "objectsToActivateAfter", "objectsToActivateAfterFadeIn",
            "objectsToActivate", "objetosParaActivar",
            "gameObjectsAActivar", "objetosAActivar", "objetoQueAparece",
        };

        private static readonly string[] CamposDestruir =
        {
            "objectsToDestroyAfter", "objectsToDestroyAfterFadeIn",
        };

        // Consecuencias que apagan un objeto en lugar de destruirlo. Antes no
        // se reproducian: las valvulas apagan tres objetos al completarse y
        // las estatuas hacen desaparecer uno, y todo eso volvia a su sitio al
        // recargar la escena aunque el puzle constase como hecho.
        private static readonly string[] CamposDesactivar =
        {
            "gameObjectsADesactivar", "objetosADesactivar", "objetoQueDesaparece",
            "objectsToDeactivate", "objectsToDeactivateAfter", "objetosParaDesactivar",
            "puerta",
        };

        private const BindingFlags Flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Aplica las consecuencias de todos los eventos completados.
        ///
        /// Itera hasta punto fijo a proposito: activar un objeto puede sacar a
        /// la luz otro componente cuyo evento tambien estaba completado y que
        /// por tanto tambien hay que reproducir. Sin ese bucle, una cadena de
        /// dos o mas pasos se quedaria a medio reconstruir.
        /// </summary>
        public static int Reproducir(IList<string> eventosCompletados, string etiqueta = "")
        {
            if (eventosCompletados == null || eventosCompletados.Count == 0) return 0;

            var pendientes = new HashSet<string>(eventosCompletados, StringComparer.Ordinal);
            var yaAplicados = new HashSet<string>(StringComparer.Ordinal);

            int aplicadosTotal = 0;
            const int maxPasadas = 12;

            for (int pasada = 1; pasada <= maxPasadas; pasada++)
            {
                int aplicadosEnEstaPasada = 0;

                // Se vuelve a escanear en cada pasada porque la anterior pudo
                // destruir componentes y activar otros nuevos.
                MonoBehaviour[] todos = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);

                // Se respeta el orden en que el jugador completo los eventos.
                foreach (string evento in eventosCompletados)
                {
                    if (string.IsNullOrEmpty(evento) || yaAplicados.Contains(evento)) continue;

                    foreach (MonoBehaviour mb in todos)
                    {
                        if (mb == null) continue;
                        string id = LeerId(mb);
                        if (id != evento) continue;

                        if (Aplicar(mb))
                        {
                            aplicadosEnEstaPasada++;
                            aplicadosTotal++;
                        }
                        yaAplicados.Add(evento);
                        break;
                    }
                }

                if (aplicadosEnEstaPasada == 0)
                {
                    LogProgreso.Info($"Reproduccion {etiqueta}: {aplicadosTotal} eventos aplicados " +
                                     $"en {pasada} pasada(s).");
                    break;
                }

                if (pasada == maxPasadas)
                    LogProgreso.Aviso($"Reproduccion {etiqueta}: se alcanzo el limite de {maxPasadas} " +
                                      "pasadas. Puede haber una cadena de consecuencias circular.");
            }

            int sinDuenio = 0;
            foreach (string evento in pendientes)
                if (!yaAplicados.Contains(evento)) sinDuenio++;
            if (sinDuenio > 0)
                LogProgreso.Info($"Reproduccion {etiqueta}: {sinDuenio} eventos completados no tienen " +
                                 "componente en esta escena (normal si ocurrieron en otra).");

            return aplicadosTotal;
        }

        /// <summary>
        /// Aplica las consecuencias de un componente: primero activar, luego
        /// destruir. Ese orden importa: si se destruyese antes, un objeto que
        /// aparece en las dos listas se perderia.
        /// </summary>
        private static bool Aplicar(MonoBehaviour mb)
        {
            bool algo = false;
            string quien = mb.gameObject != null ? mb.gameObject.name : mb.GetType().Name;

            foreach (string campo in CamposActivar)
            {
                foreach (GameObject obj in LeerObjetos(mb, campo))
                {
                    if (obj == null || obj.activeSelf) continue;
                    obj.SetActive(true);
                    LogProgreso.Info($"  activa  {quien} -> {obj.name}", obj);
                    algo = true;
                }
            }

            foreach (string campo in CamposDesactivar)
            {
                foreach (GameObject obj in LeerObjetos(mb, campo))
                {
                    if (obj == null || !obj.activeSelf) continue;
                    obj.SetActive(false);
                    LogProgreso.Info($"  apaga   {quien} -> {obj.name}", mb);
                    algo = true;
                }
            }

            foreach (string campo in CamposDestruir)
            {
                foreach (GameObject obj in LeerObjetos(mb, campo))
                {
                    if (obj == null) continue;
                    LogProgreso.Info($"  destruye {quien} -> {obj.name}", mb);
                    UnityEngine.Object.Destroy(obj);
                    algo = true;
                }
            }

            // El propio componente puede tener que desaparecer tras su evento.
            if (LeerBoolAutodestruccion(mb))
            {
                LogProgreso.Info($"  autodestruye {quien}", mb);
                UnityEngine.Object.Destroy(mb.gameObject);
                algo = true;
            }

            return algo;
        }

        public static string LeerId(MonoBehaviour mb)
        {
            Type t = mb.GetType();
            foreach (string campo in CamposId)
            {
                FieldInfo f = t.GetField(campo, Flags);
                if (f == null || f.FieldType != typeof(string)) continue;
                var valor = f.GetValue(mb) as string;
                if (!string.IsNullOrEmpty(valor) && valor != "None") return valor;
            }
            return null;
        }

        /// <summary>
        /// Lee un campo que puede ser GameObject o GameObject[] y lo devuelve
        /// siempre como secuencia, para no duplicar el bucle en cada caso.
        /// </summary>
        private static IEnumerable<GameObject> LeerObjetos(MonoBehaviour mb, string campo)
        {
            FieldInfo f = mb.GetType().GetField(campo, Flags);
            if (f == null) yield break;

            if (f.FieldType == typeof(GameObject))
            {
                var uno = f.GetValue(mb) as GameObject;
                if (uno != null) yield return uno;
            }
            else if (f.FieldType == typeof(GameObject[]))
            {
                var varios = f.GetValue(mb) as GameObject[];
                if (varios == null) yield break;
                foreach (GameObject obj in varios) yield return obj;
            }
        }

        /// <summary>
        /// ¿Este componente declara alguna consecuencia reproducible? Se mira
        /// que el campo exista, no que tenga objetos dentro: un array vacio
        /// hoy puede llenarse manana en el inspector.
        /// </summary>
        public static bool TieneConsecuencias(MonoBehaviour mb)
        {
            Type t = mb.GetType();
            foreach (string campo in CamposActivar)
                if (EsCampoDeObjetos(t, campo)) return true;
            foreach (string campo in CamposDesactivar)
                if (EsCampoDeObjetos(t, campo)) return true;
            foreach (string campo in CamposDestruir)
                if (EsCampoDeObjetos(t, campo)) return true;
            return false;
        }

        private static bool EsCampoDeObjetos(Type t, string campo)
        {
            FieldInfo f = t.GetField(campo, Flags);
            return f != null &&
                   (f.FieldType == typeof(GameObject) || f.FieldType == typeof(GameObject[]));
        }

        /// <summary>
        /// ¿El componente se borra a si mismo tras su evento? Dos formas:
        /// un bool tipo destroyAfterCompletion / destruirDespuesDeDialogo, o
        /// un enum cuyo valor elegido se llame "Destruirse" (asi lo resuelve
        /// el patrullero del laberinto, con su comportamientoFinal).
        /// </summary>
        private static bool LeerBoolAutodestruccion(MonoBehaviour mb)
        {
            foreach (FieldInfo f in mb.GetType().GetFields(Flags))
            {
                if (f.FieldType == typeof(bool))
                {
                    string n = f.Name;
                    if (n.StartsWith("destroyAfter", StringComparison.Ordinal) ||
                        n.StartsWith("destruirDespues", StringComparison.Ordinal))
                    {
                        if ((bool)f.GetValue(mb)) return true;
                    }
                }
                else if (f.FieldType.IsEnum)
                {
                    object valor = f.GetValue(mb);
                    string nombre = valor != null ? valor.ToString() : "";
                    if (nombre.IndexOf("destru", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        nombre.IndexOf("destroy", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Informe de diagnostico: que eventos declara la escena y cuales de
        /// ellos constan como completados. Util para QA y para cazar ids
        /// repetidos o vacios.
        /// </summary>
        public static string Informe(IList<string> eventosCompletados)
        {
            var completados = new HashSet<string>(
                eventosCompletados ?? new List<string>(), StringComparer.Ordinal);
            var vistos = new Dictionary<string, int>(StringComparer.Ordinal);
            int sinId = 0;

            foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                if (!TieneConsecuencias(mb)) continue;

                string id = LeerId(mb);
                if (string.IsNullOrEmpty(id)) { sinId++; continue; }
                vistos.TryGetValue(id, out int n);
                vistos[id] = n + 1;
            }

            int hechos = 0, repetidos = 0;
            foreach (var kv in vistos)
            {
                if (completados.Contains(kv.Key)) hechos++;
                if (kv.Value > 1) repetidos++;
            }

            return $"Eventos con consecuencias en la escena: {vistos.Count}\n" +
                   $"  completados segun la partida : {hechos}\n" +
                   $"  con id repetido              : {repetidos}\n" +
                   $"  sin id (no reproducibles)    : {sinId}";
        }
    }
}
