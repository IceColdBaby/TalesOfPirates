using System;
using Top.Logging;
using UnityEngine;

namespace Top.Client.App
{
    /// <summary>
    /// Routes the logging facade to the Unity console. Installs itself when
    /// the editor loads and when a player starts, so any code that logs has
    /// a sink without further wiring.
    /// </summary>
    public class UnityLogWriter : ILogWriter
    {
        private const string Tag = "[Top] ";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            Log.Writer = new UnityLogWriter();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InstallInEditor()
        {
            Install();
        }
#endif

        public void Write(string message)
        {
            Debug.Log(Tag + message);
        }

        public void WriteWarning(string message)
        {
            Debug.LogWarning(Tag + message);
        }

        public void WriteError(string message, Exception exception)
        {
            Debug.LogError(exception == null
                ? Tag + message
                : $"{Tag}{message}: {exception}");
        }

        public void WriteDebug(string message)
        {
            Debug.Log(Tag + message);
        }
    }
}
