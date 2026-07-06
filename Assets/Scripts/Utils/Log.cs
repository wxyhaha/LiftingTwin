// -----------------------------------------------------------------------
// PowerTwin - Simple Structured Logger
//
// Design principles:
//   - Never use Debug.Log directly in business code.
//   - All logs go through Log.{Level}(tag, message).
//   - Tags are free-form strings; recommend "ModuleName" or "ModuleName.SubSystem".
//   - Log level can be configured globally at runtime.
//   - Extensible: add file output, network output, etc. by appending to
//     the Out event without modifying this file.
//
// Usage:
//   Log.Info ("Network", "Connected to server at {0}:{1}", host, port);
//   Log.Warn ("PointCloud", "Frame {0} has zero points, skipping", frameId);
//   Log.Error("Mesh", "Failed to parse mesh data: {0}", ex.Message);
// -----------------------------------------------------------------------

using System;
using UnityEngine;

namespace PowerTwin.Utils
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        None = 4
    }

    /// <summary>
    /// Centralized logging facade for the entire application.
    /// Business code must use this instead of UnityEngine.Debug.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Global minimum log level. Messages below this level are suppressed.
        /// Defaults to Debug in Editor, Info in builds.
        /// </summary>
        public static LogLevel MinLevel { get; set; } =
#if UNITY_EDITOR
            LogLevel.Debug;
#else
            LogLevel.Info;
#endif

        /// <summary>
        /// Subscribe to receive all log messages (tag, level, formatted message).
        /// Useful for file logging, network logging, or custom overlays.
        /// </summary>
        public static event Action<string, LogLevel, string> Out;

        #region Public API

        /// <summary>
        /// Debug-level log. For development-only diagnostics.
        /// Suppressed in builds by default.
        /// </summary>
        public static void Debug(string tag, string format, params object[] args)
        {
            Emit(tag, LogLevel.Debug, format, args);
        }

        /// <summary>
        /// Info-level log. For normal runtime events worth recording.
        /// </summary>
        public static void Info(string tag, string format, params object[] args)
        {
            Emit(tag, LogLevel.Info, format, args);
        }

        /// <summary>
        /// Warning-level log. For recoverable anomalies.
        /// </summary>
        public static void Warn(string tag, string format, params object[] args)
        {
            Emit(tag, LogLevel.Warn, format, args);
        }

        /// <summary>
        /// Error-level log. For failures that affect functionality.
        /// </summary>
        public static void Error(string tag, string format, params object[] args)
        {
            Emit(tag, LogLevel.Error, format, args);
        }

        /// <summary>
        /// Error-level log with an Exception object.
        /// </summary>
        public static void Exception(string tag, System.Exception ex, string format = null, params object[] args)
        {
            var msg = args.Length > 0 ? string.Format(format ?? "", args) : format ?? "";
            var full = string.IsNullOrEmpty(msg) ? ex.ToString() : $"{msg}\n{ex}";
            Emit(tag, LogLevel.Error, full);
        }

        #endregion

        #region Internal

        private static void Emit(string tag, LogLevel level, string format, params object[] args)
        {
            if (level < MinLevel) return;

            var message = args.Length > 0 ? string.Format(format, args) : format;

            // Unity console (with rich tag prefix)
            var prefix = $"[{tag}]";
            switch (level)
            {
                case LogLevel.Debug:
                    UnityEngine.Debug.Log($"{prefix} {message}");
                    break;
                case LogLevel.Info:
                    UnityEngine.Debug.Log($"{prefix} {message}");
                    break;
                case LogLevel.Warn:
                    UnityEngine.Debug.LogWarning($"{prefix} {message}");
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError($"{prefix} {message}");
                    break;
            }

            // Notify subscribers (file logger, network logger, etc.)
            Out?.Invoke(tag, level, message);
        }

        #endregion
    }
}
