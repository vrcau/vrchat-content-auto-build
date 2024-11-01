using System;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRChatAerospaceUniversity.VRChatAutoBuild
{
    [PublicAPI]
    public static class AutoBuildLogger
    {
        private static int _logGroupDepth;

        public static void Log(LogType logType, object message, Object context = null, string source = null)
        {
            switch (logType)
            {
                case LogType.Error:
                    LogAction("error", message, context, source);
                    break;
                case LogType.Assert:
                    LogAction("debug", message, context, source);
                    break;
                case LogType.Warning:
                    LogAction("warning", message, context, source);
                    break;
                case LogType.Log:
                    LogAction("notice", message, context, source);
                    break;
                case LogType.Exception:
                    LogAction("error", message, context, source);
                    break;
            }

            Debug.unityLogger.Log(logType, source ?? nameof(AutoBuildLogger), message, context);
        }

        public static void Log(object message, Object context = null, string source = null)
        {
            Log(LogType.Log, message, context, source);
        }

        public static void LogWarning(object message, Object context = null, string source = null)
        {
            Log(LogType.Warning, message, context, source);
        }

        public static void LogError(object message, Object context = null, string source = null)
        {
            Log(LogType.Error, message, context, source);
        }

        public static void LogException(Exception exception, Object context = null, string source = null)
        {
            Log(LogType.Exception, exception, context, source);
        }

        public static void BeginLogGroup(string name)
        {
            Console.WriteLine("::group::" + name);
            _logGroupDepth++;
        }

        public static void EndLogGroup()
        {
            Console.WriteLine("::endgroup::");
            if (_logGroupDepth > 0)
                _logGroupDepth--;
        }

        public static void EndAllLogGroups()
        {
            while (_logGroupDepth > 0)
            {
                EndLogGroup();
            }
        }

        private static void LogAction(string action, object message, Object context = null, string source = null)
        {
            Console.WriteLine(
                $"::{action}{(source != null ? $" title={source}" : "")}::{message}{(context ? $"\n{context}" : "")}");
        }
    }
}
