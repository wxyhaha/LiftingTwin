using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

/// <summary>
/// 测试从 ROS2 接收消息，并在屏幕右上角显示
/// </summary>
public class RosSubscriberTest : MonoBehaviour
{
    string m_LastMessage = "等待消息...";

    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<StringMsg>("/unity_test", OnMessageReceived);
        Debug.Log("已订阅 /unity_test，等待 ROS2 消息...");
    }

    void OnMessageReceived(StringMsg msg)
    {
        m_LastMessage = msg.data;
        Debug.Log($"[ROS2 收到消息] data: {msg.data}");
    }

    void OnGUI()
    {
        // 在屏幕右上角显示最新消息
        GUI.Label(new Rect(Screen.width - 400, 60, 380, 60),
            $"<size=20><b>ROS2 最新消息:</b>\n{m_LastMessage}</size>");
    }
}
