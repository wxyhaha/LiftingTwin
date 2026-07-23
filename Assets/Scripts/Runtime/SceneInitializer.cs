// -----------------------------------------------------------------------
// LiftingTwin - 场景初始化器（精简版）
//
// 用途：
//   仅初始化点云渲染所需的基础环境，不创建任何辅助物体。
// -----------------------------------------------------------------------

using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.Runtime
{
    /// <summary>
    /// 场景初始化器。仅初始化基础环境。
    /// </summary>
    public class SceneInitializer : MonoBehaviour
    {
        private void Start()
        {
            Log.Info("Runtime", "SceneInitializer: 场景已就绪（仅点云渲染模式）");
        }
    }
}
