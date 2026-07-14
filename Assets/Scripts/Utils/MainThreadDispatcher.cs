using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace LiftingTwin.Utils
{
    /// <summary>
    /// 将后台线程的 Action 调度到 Unity 主线程执行
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        static MainThreadDispatcher _instance;
        static ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            var go = new GameObject("MainThreadDispatcher");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MainThreadDispatcher>();
        }

        public static void Run(Action action)
        {
            _queue.Enqueue(action);
        }

        void Update()
        {
            while (_queue.TryDequeue(out Action action))
            {
                action();
            }
        }
    }
}
