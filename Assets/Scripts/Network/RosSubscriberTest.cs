#if LIFTINGTWIN_HAS_ROS
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using LiftingTwin.Utils;

namespace LiftingTwin.Network
{
    /// <summary>
    /// 测试从 ROS2 接收消息，并在屏幕右上角显示
    /// </summary>
    public class RosSubscriberTest : MonoBehaviour
    {
        string _lastMessage = "等待消息...";

        void Start()
        {
            ROSConnection.GetOrCreateInstance().Subscribe<StringMsg>("/unity_test", OnMessageReceived);
            Log.Info("[RosSubscriberTest] 已订阅 /unity_test");
        }

        void OnMessageReceived(StringMsg msg)
        {
            _lastMessage = msg.data;
            Log.Info("[ROS2 收到消息] data: " + msg.data);
        }

        void OnGUI()
        {
            GUI.Label(new Rect(Screen.width - 400, 60, 380, 60),
                $"<size=20><b>ROS2 最新消息:</b>\n{_lastMessage}</size>");
        }
    }
}
#endif
