using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// 将后台线程的 Action 调度到 Unity 主线程执行
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    static MainThreadDispatcher s_Instance;
    static ConcurrentQueue<Action> s_Queue = new ConcurrentQueue<Action>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        var go = new GameObject("MainThreadDispatcher");
        DontDestroyOnLoad(go);
        s_Instance = go.AddComponent<MainThreadDispatcher>();
    }

    public static void Run(Action action)
    {
        s_Queue.Enqueue(action);
    }

    void Update()
    {
        while (s_Queue.TryDequeue(out Action action))
        {
            action();
        }
    }
}
