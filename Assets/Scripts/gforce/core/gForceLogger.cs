
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
using UnityEngine;
#else
using System;
#endif


namespace gf
{
    public class GForceLogger
    {
        public enum LogLevel
        {
            LOG_DEBUG,
            LOG_WARNING,
            LOG_ERROR
        };

        private static LogLevel _logLevel = LogLevel.LOG_DEBUG;

        public static void SetLogLevel(LogLevel level)
        {
            _logLevel = level;
        }

        private static void LogInternal(LogLevel level, object message)
        {
            if (_logLevel > level)
            {
                return;
            }

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
            switch (level)
            {
                case LogLevel.LOG_ERROR:
                    Debug.LogError(message);
                    break;

                case LogLevel.LOG_WARNING:
                    Debug.LogWarning(message);
                    break;

                case LogLevel.LOG_DEBUG:
                    Debug.Log(message);
                    break;
            }
#else
        switch (level)
        {
            case LogLevel.LOG_ERROR:
                Console.WriteLine("[E]" + message);
                break;

            case LogLevel.LOG_WARNING:
                Console.WriteLine("[W]" + message);
                break;

            case LogLevel.LOG_DEBUG:
                Console.WriteLine("[D]" + message);
                break;
        }
#endif
        }


        private static void LogFormatInternal(LogLevel level, string format, params object[] args)
        {
            if (_logLevel > level)
            {
                return;
            }

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
            switch (level)
            {
                case LogLevel.LOG_ERROR:
                    Debug.LogErrorFormat(format, args);
                    break;

                case LogLevel.LOG_WARNING:
                    Debug.LogWarningFormat(format, args);
                    break;

                case LogLevel.LOG_DEBUG:
                    Debug.LogFormat(format, args);
                    break;
            }
#else
        switch (level)
        {
            case LogLevel.LOG_ERROR:
                Console.WriteLine("[E]" + format, args);
                break;

            case LogLevel.LOG_WARNING:
                Console.WriteLine("[W]" + format, args);
                break;

            case LogLevel.LOG_DEBUG:
                Console.WriteLine("[D]" + format, args);
                break;
        }
#endif
        }

        public static void Log(object message)
        {
            LogInternal(LogLevel.LOG_DEBUG, message);
        }

        public static void LogFormat(string format, params object[] args)
        {
            LogFormatInternal(LogLevel.LOG_DEBUG, format, args);
        }

        public static void LogWarning(object message)
        {
            LogInternal(LogLevel.LOG_WARNING, message);
        }

        public static void LogWarningFormat(string format, params object[] args)
        {
            LogFormatInternal(LogLevel.LOG_WARNING, format, args);
        }

        public static void LogError(object message)
        {
            LogInternal(LogLevel.LOG_ERROR, message);
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            LogFormatInternal(LogLevel.LOG_ERROR, format, args);
        }
    }
}