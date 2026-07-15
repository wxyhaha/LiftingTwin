using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
#if LIFTINGTWIN_HAS_ROS
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
#endif
using LiftingTwin.Utils;

namespace LiftingTwin.Network
{
    /// <summary>
    /// Qt ↔ Unity IPC 桥接服务器
    /// Qt 通过 TCP 连接 localhost:9000，发送 JSON 命令，Unity 转发给 ROS2
    /// </summary>
    public class QtBridge : MonoBehaviour
    {
        public int listenPort = 9000;

        TcpListener _listener;
        Thread _serverThread;
        CancellationTokenSource _cts;
        TcpClient _client;
        NetworkStream _stream;
        ConcurrentQueue<string> _sendQueue = new ConcurrentQueue<string>();
        byte[] _recvBuf = new byte[4096];

        void Start()
        {
            _cts = new CancellationTokenSource();
            _serverThread = new Thread(ListenLoop);
            _serverThread.IsBackground = true;
            _serverThread.Start();
            Log.Info("[QtBridge] 启动，监听 localhost:" + listenPort);
        }

        void Update()
        {
            while (_sendQueue.TryDequeue(out string json))
            {
                Send(json);
            }
        }

        void OnApplicationQuit()
        {
            // Unity Standalone 退出时释放端口，防止下次启动冲突
            OnDestroy();
        }

        void OnDestroy()
        {
            _cts?.Cancel();
            try { _listener?.Stop(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
        }

        /// <summary>
        /// 向 Qt 发送事件
        /// </summary>
        public void SendEvent(string topic, string data)
        {
            var ev = $"{{\"event\":\"message\",\"topic\":\"{EscapeJson(topic)}\",\"data\":\"{EscapeJson(data)}\"}}";
            _sendQueue.Enqueue(ev);
        }

        /// <summary>
        /// 向 Qt 发送日志
        /// </summary>
        public void SendLog(string level, string text)
        {
            var ev = $"{{\"event\":\"log\",\"level\":\"{level}\",\"text\":\"{EscapeJson(text)}\"}}";
            _sendQueue.Enqueue(ev);
        }

        void ListenLoop()
        {
            _listener = new TcpListener(IPAddress.Loopback, listenPort);
            _listener.Start();

            var token = _cts.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    _client = client;
                    _stream = client.GetStream();

                    Log.Info("[QtBridge] Qt 已连接");
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
            var stream = _stream;
            var sb = new StringBuilder();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    int read = stream.Read(_recvBuf, 0, _recvBuf.Length);
                    if (read == 0) break;

                    sb.Append(Encoding.UTF8.GetString(_recvBuf, 0, read));
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

            Log.Info("[QtBridge] Qt 断开");
            _client = null;
            _stream = null;
        }

        void Send(string json)
        {
            var s = _stream;
            if (s == null) return;
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(json + "\n");
                s.Write(data, 0, data.Length);
            }
            catch
            {
                while (_sendQueue.TryDequeue(out _)) { }
            }
        }

        void HandleCommand(string json)
        {
            try
            {
                var cmd = JsonUtility.FromJson<QtCommand>(json);
                if (cmd == null) return;

#if LIFTINGTWIN_HAS_ROS
                var ros = ROSConnection.GetOrCreateInstance();

                switch (cmd.cmd)
                {
                    case "publish_string":
                        ros.RegisterPublisher(cmd.topic, "std_msgs/String");
                        ros.Publish(cmd.topic, new StringMsg(cmd.data));
                        break;

                    case "subscribe":
                        ros.Subscribe<StringMsg>(cmd.topic, msg =>
                        {
                            SendEvent(cmd.topic, msg.data);
                        });
                        break;

                    case "unsubscribe":
                        ros.Unsubscribe(cmd.topic);
                        break;

                    default:
                        Log.Warn("[QtBridge] 未知命令: " + cmd.cmd);
                        break;
                }
#else
                Log.Warn("[QtBridge] ROS 未安装，忽略命令: " + cmd.cmd);
#endif
            }
            catch (System.Exception e)
            {
                Log.Error("[QtBridge] 命令处理异常: " + e.Message);
            }
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
}
