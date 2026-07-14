using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

/// <summary>
/// Qt ↔ Unity IPC 桥接服务器
/// Qt 通过 TCP 连接 localhost:9000，发送 JSON 命令，Unity 转发给 ROS2
///
/// 命令格式（Qt → Unity）：
///   {"cmd":"publish_string","topic":"/xxx","data":"hello"}
///   {"cmd":"subscribe","topic":"/xxx"}
///   {"cmd":"unsubscribe","topic":"/xxx"}
///
/// 事件格式（Unity → Qt）：
///   {"event":"message","topic":"/xxx","data":"消息内容"}
///   {"event":"log","level":"info","text":"..."}
/// </summary>
public class QtBridge : MonoBehaviour
{
    public int listenPort = 9000;

    TcpListener m_Listener;
    Thread m_ServerThread;
    CancellationTokenSource m_Cts;

    // 连接的 Qt 客户端
    volatile TcpClient m_Client;
    volatile NetworkStream m_Stream;

    // 发往 Qt 的队列（主线程写入，后台线程读取发送）
    ConcurrentQueue<string> m_SendQueue = new ConcurrentQueue<string>();

    // 后台接收缓冲
    byte[] m_RecvBuf = new byte[4096];

    void Start()
    {
        m_Cts = new CancellationTokenSource();
        m_ServerThread = new Thread(ListenLoop);
        m_ServerThread.IsBackground = true;
        m_ServerThread.Start();
        Debug.Log($"[QtBridge] 启动，监听 localhost:{listenPort}");
    }

    void Update()
    {
        // 主线程：发队列 → 网络
        while (m_SendQueue.TryDequeue(out string json))
        {
            Send(json);
        }
    }

    void OnDestroy()
    {
        m_Cts?.Cancel();
        m_Listener?.Stop();
        m_Stream?.Close();
        m_Client?.Close();
    }

    // ── 对外 API（供 Unity 其他脚本调用） ──────────────────────────

    /// <summary>向 Qt 发送事件</summary>
    public void SendEvent(string topic, string data)
    {
        var ev = $"{{\"event\":\"message\",\"topic\":\"{EscapeJson(topic)}\",\"data\":\"{EscapeJson(data)}\"}}";
        m_SendQueue.Enqueue(ev);
    }

    /// <summary>向 Qt 发送日志</summary>
    public void SendLog(string level, string text)
    {
        var ev = $"{{\"event\":\"log\",\"level\":\"{level}\",\"text\":\"{EscapeJson(text)}\"}}";
        m_SendQueue.Enqueue(ev);
    }

    // ── TCP 服务器 ─────────────────────────────────────────────

    void ListenLoop()
    {
        m_Listener = new TcpListener(IPAddress.Loopback, listenPort);
        m_Listener.Start();

        var token = m_Cts.Token;
        while (!token.IsCancellationRequested)
        {
            try
            {
                // 等待连接（可取消）
                var client = m_Listener.AcceptTcpClient();
                m_Client = client;
                m_Stream = client.GetStream();

                Debug.Log("[QtBridge] Qt 已连接");

                // 发送输出队列中的数据
                var sendThread = new Thread(() => SendLoop(token));
                sendThread.IsBackground = true;
                sendThread.Start();

                // 接收循环
                RecvLoop(token);
            }
            catch (OperationCanceledException) { break; }
            catch (SocketException) { break; }
            catch (ThreadAbortException) { break; }
            catch
            {
                Thread.Sleep(1000);
            }
        }
    }

    void RecvLoop(CancellationToken token)
    {
        var stream = m_Stream;
        var sb = new StringBuilder();

        try
        {
            while (!token.IsCancellationRequested)
            {
                int read = stream.Read(m_RecvBuf, 0, m_RecvBuf.Length);
                if (read == 0) break;

                sb.Append(Encoding.UTF8.GetString(m_RecvBuf, 0, read));
                string s = sb.ToString();

                int nl;
                while ((nl = s.IndexOf('\n')) >= 0)
                {
                    string line = s.Substring(0, nl).Trim();
                    s = s.Substring(nl + 1);
                    if (line.Length > 0)
                    {
                        MainThreadDispatcher.Run(() => HandleCommand(line));
                    }
                }
                sb.Clear();
                sb.Append(s);
            }
        }
        catch { }

        Debug.Log("[QtBridge] Qt 断开");
        m_Client = null;
        m_Stream = null;
    }

    void SendLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (m_SendQueue.TryDequeue(out string json))
            {
                Send(json);
            }
            else
            {
                Thread.Sleep(10);
            }
        }
    }

    void Send(string json)
    {
        var s = m_Stream;
        if (s == null) return;
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(json + "\n");
            s.Write(data, 0, data.Length);
        }
        catch { }
    }

    // ── 命令处理 ───────────────────────────────────────────────

    void HandleCommand(string json)
    {
        try
        {
            var cmd = JsonUtility.FromJson<QtCommand>(json);
            if (cmd == null) return;

            var ros = ROSConnection.GetOrCreateInstance();

            switch (cmd.cmd)
            {
                case "publish_string":
                    ros.RegisterPublisher(cmd.topic, "std_msgs/String");
                    ros.Publish(cmd.topic, new StringMsg(cmd.data));
                    Log(cmd.cmd, cmd.topic);
                    break;

                case "subscribe":
                    ros.Subscribe<StringMsg>(cmd.topic, msg =>
                    {
                        SendEvent(cmd.topic, msg.data);
                    });
                    Log(cmd.cmd, cmd.topic);
                    break;

                case "unsubscribe":
                    ros.Unsubscribe(cmd.topic);
                    Log(cmd.cmd, cmd.topic);
                    break;

                default:
                    Debug.LogWarning($"[QtBridge] 未知命令: {cmd.cmd}");
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[QtBridge] 命令处理异常: {e.Message}");
        }
    }

    void Log(string cmd, string topic)
    {
        Debug.Log($"[QtBridge] {cmd} {topic}");
    }

    static string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    [System.Serializable]
    class QtCommand
    {
        public string cmd;
        public string topic;
        public string data;
    }
}
