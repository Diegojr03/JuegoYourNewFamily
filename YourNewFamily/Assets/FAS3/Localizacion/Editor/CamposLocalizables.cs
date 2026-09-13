using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YNF.Localizacion.EditorTools
{
    /// <summary>
    /// Que campos del proyecto llevan texto que ve el jugador, y cuales de
    /// ellos tienen clave propia. Lo comparten el extractor, el asignador de
    /// claves y el inyector de etiquetas, para que no se les vaya la lista.
    /// </summary>
    public static class CamposLocalizables
    {
        /// <summary>
        /// Campos con texto visible, y el uso que se escribe en el CSV.
        ///
        /// OJO con lo que NO esta aqui:
        ///  - nombreZona de MapaManager: es el identificador con el que
        ///    DesbloqueadorZona casa las zonas. Traducirlo rompe el mapa.
        ///  - itemId, dialogueId, noteId, triggerId, objectId, transitionName:
        ///    identificadores internos y claves de guardado.
        ///  - sceneName, nombreEscenaMenu: nombres de escena para SceneManager.
        /// </summary>
        public static readonly Dictionary<string, string> Visibles = new Dictionary<string, string>
        {
            { "dialogueText",      "Dialogo" },
            { "speakerName",       "Nombre de hablante" },
            { "choiceText",        "Opcion de dialogo" },
            { "textoNuevo",        "Objetivo de mision" },
            { "zoneName",          "Nombre de zona" },
            { "inventoryItemName", "Nombre de objeto" },
            { "itemName",          "Nombre de objeto" },
            { "itemNameToGive",    "Nombre de objeto" },
            { "description",       "Descripcion de objeto" },
            { "mensajeCompletado", "Mensaje de puzle" },
            { "mensajeIncorrecto", "Mensaje de puzle" },
            { "protagonistName",   "Nombre de hablante" },
            { "conversationOwner", "Nombre de hablante" },
            { "actionName",        "Nombre de control" },
            { "lineasDialogo",     "Dialogo de puzle" },
            { "textoOrigen",       "Etiqueta de interfaz" },
        };

        /// <summary>
        /// Campos que llevan un locKey hermano, es decir, los que tienen clave
        /// propia y por tanto se pueden reescribir en espanol sin perder la
        /// traduccion. El resto se resuelve por texto, que sigue funcionando.
        /// </summary>
        public static readonly string[] ConClavePropia =
        {
            "dialogueText", "choiceText", "textoNuevo", "zoneName", "textoOrigen", "noteTextAsset",
        };

        /// <summary>Uso que se escribe en el CSV para las notas de lore.</summary>
        public const string UsoNota = "Nota de lore";

        private static readonly HashSet<string> Marcadores = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "New Text", "Nuevo Item", "Nombre del Item", "Nombre de la Zona",
            "Descripcion del item", "Descripción del item", "Sample Text", "Text", "Texto",
        };

        public static bool EsTraducible(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return false;
            string v = Loc.Normalizar(valor);
            if (v.Length < 2) return false;
            if (Marcadores.Contains(v)) return false;
            foreach (char c in v) if (char.IsLetter(c)) return true;
            return false;
        }

        /// <summary>
        /// Dado un locKey, devuelve la propiedad hermana que guarda el espanol.
        /// Puede ser una cadena o un TextAsset, en el caso de las notas.
        /// </summary>
        public static SerializedProperty BuscarOrigen(SerializedProperty locKey)
        {
            string ruta = locKey.propertyPath;
            int corte = ruta.LastIndexOf('.');
            string prefijo = corte >= 0 ? ruta.Substring(0, corte + 1) : string.Empty;

            foreach (string campo in ConClavePropia)
            {
                SerializedProperty p = locKey.serializedObject.FindProperty(prefijo + campo);
                if (p != null) return p;
            }
            return null;
        }

        public static string LeerTexto(SerializedProperty p)
        {
            if (p == null) return string.Empty;
            if (p.propertyType == SerializedPropertyType.String) return p.stringValue;
            if (p.propertyType == SerializedPropertyType.ObjectReference)
            {
                var asset = p.objectReferenceValue as TextAsset;
                return asset != null ? asset.text : string.Empty;
            }
            return string.Empty;
        }

        public static string UsoDe(SerializedProperty origen)
        {
            if (origen == null) return "Dialogo";
            if (origen.propertyType == SerializedPropertyType.ObjectReference) return UsoNota;
            string nombre = NombreDeCampo(origen.propertyPath);
            return Visibles.TryGetValue(nombre, out string uso) ? uso : "Dialogo";
        }

        public static string NombreDeCampo(string propertyPath)
        {
            int corte = propertyPath.LastIndexOf('.');
            return corte >= 0 ? propertyPath.Substring(corte + 1) : propertyPath;
        }
    }
}
