// -----------------------------------------------------------------------
// PowerTwin - Application Bootstrap
//
// Purpose:
//   Minimal entry point MonoBehaviour. Attach to a GameObject in the
//   startup scene (_Scenes/MainScene.unity). It initializes:
//     1. Logging level from config.
//     2. Frame rate and VSync from config.
//     3. Future: subsystem initialization dispatch.
//
// Lifetime:
//   DontDestroyOnLoad — persists across scene loads.
//   Place it on a "Bootstrap" GameObject in the main scene.
// -----------------------------------------------------------------------

using PowerTwin.Utils;
using UnityEngine;

namespace PowerTwin.Core
{
    /// <summary>
    /// Application entry point. Initializes core services on Awake.
    /// Attach to a single GameObject in the startup scene.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Reference to the AppConfig ScriptableObject (from Assets/_Config/).")]
        private AppConfig config;

        private void Awake()
        {
            if (config == null)
            {
                Log.Warn("Bootstrap", "No AppConfig assigned. Using defaults.");
                return;
            }

            ApplyConfig(config);
        }

        private void ApplyConfig(AppConfig cfg)
        {
            // Logging
            Log.MinLevel = cfg.minLogLevel;
            Log.Info("Bootstrap", "PowerTwin starting. LogLevel={0}", Log.MinLevel);

            // Frame
            Application.targetFrameRate = cfg.targetFrameRate;
            QualitySettings.vSyncCount = cfg.vSyncCount;
            Log.Info("Bootstrap", "TargetFPS={0}, VSync={1}", cfg.targetFrameRate, cfg.vSyncCount);

            // Future: dispatch to NetworkManager, PointCloudManager, etc.
        }
    }
}
