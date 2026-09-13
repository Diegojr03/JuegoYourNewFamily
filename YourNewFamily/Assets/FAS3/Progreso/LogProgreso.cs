using UnityEngine;

namespace YNF.Progreso
{
    /// <summary>
    /// Trazas del sistema de progreso y guardado.
    ///
    /// Estan apagadas por defecto. Se encienden desde el inspector del
    /// SaveManager, o poniendo Activo a true desde codigo, y entonces cada
    /// activacion, destruccion, registro, guardado y restauracion deja una
    /// linea en consola con el id del objeto implicado. Es lo que permite
    /// seguir una partida y ver exactamente en que paso se pierde un estado.
    /// </summary>
    public static class LogProgreso
    {
        public static bool Activo = false;

        private const string Prefijo = "[Progreso] ";

        public static void Info(string mensaje)
        {
            if (Activo) Debug.Log(Prefijo + mensaje);
        }

        public static void Info(string mensaje, Object contexto)
        {
            if (Activo) Debug.Log(Prefijo + mensaje, contexto);
        }

        public static void Aviso(string mensaje, Object contexto = null)
        {
            // Los avisos salen siempre: señalan estados que no deberian darse.
            if (contexto != null) Debug.LogWarning(Prefijo + mensaje, contexto);
            else Debug.LogWarning(Prefijo + mensaje);
        }

        public static string Nombre(GameObject go) => go != null ? go.name : "(nulo)";
    }
}
