// -----------------------------------------------------------------------
// PowerTwin - Application Configuration (ScriptableObject)
//
// Purpose:
//   Central, inspector-editable configuration asset for the application.
//   Create one instance via: Assets > Create > PowerTwin > AppConfig
//   and place it under Assets/_Config/.
//
// Design:
//   - ScriptableObject: edit in Inspector, no parsing, one asset = one config.
//   - Fields are grouped by subsystem for readability.
//   - All values have sensible defaults so the app runs out-of-the-box.
// -----------------------------------------------------------------------

using UnityEngine;

namespace PowerTwin.Core
{
    /// <summary>
    /// Root configuration asset for the PowerTwin application.
    /// Create via the Create Asset menu and reference from AppBootstrap.
    /// </summary>
    [CreateAssetMenu(fileName = "AppConfig", menuName = "PowerTwin/App Config", order = 1)]
    public class AppConfig : ScriptableObject
    {
        [Header("Application")]
        [Tooltip("Target frame rate. 0 = platform default.")]
        public int targetFrameRate = 60;

        [Tooltip("Enable VSync. 0 = off, 1 = on, 2 = on (every second V-blank).")]
        public int vSyncCount = 0;

        [Header("Network")]
        [Tooltip("Server address for data stream connection.")]
        public string serverAddress = "127.0.0.1";

        [Tooltip("Server port for WebSocket / TCP connection.")]
        public int serverPort = 8765;

        [Tooltip("Auto-reconnect delay in seconds.")]
        public float reconnectDelay = 3.0f;

        [Header("Rendering")]
        [Tooltip("Maximum point cloud points to render in a single frame.")]
        public int maxPointCloudPoints = 10_000_000;

        [Tooltip("Point cloud point size in screen pixels.")]
        public float pointCloudPointSize = 2.0f;

        [Header("Logging")]
        [Tooltip("Minimum log level. Messages below this are suppressed.")]
        public Utils.LogLevel minLogLevel = Utils.LogLevel.Info;
    }
}
