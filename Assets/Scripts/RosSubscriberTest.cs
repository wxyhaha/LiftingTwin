using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

/// <summary>
/// 测试从 ROS2 接收消息
/// </summary>
public class RosSubscriberTest : MonoBehaviour
{
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<StringMsg>("/unity_test", OnMessageReceived);
        Debug.Log("已订阅 /unity_test，等待 ROS2 消息...");
    }

    void OnMessageReceived(StringMsg msg)
    {
        Debug.Log($"[ROS2 收到消息] data: {msg.data}");
    }
}
