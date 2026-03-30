// ============================================================
// 파일명  : UnityMainThread.cs
// 역할    : Unity API 호출을 위한 메인 스레드 디스패처
// 작성자  : 송준호
// 작성일  : 2026-03-30
// 수정이력: 
// ============================================================

using System;
using System.Collections.Concurrent;
using UnityEngine;

public class UnityMainThread : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> _queue = new();
    private static UnityMainThread _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_instance == null)
        {
            var go = new GameObject("[UnityMainThread]");
            _instance = go.AddComponent<UnityMainThread>();
            DontDestroyOnLoad(go);
        }
    }

    public static void Execute(Action action)
    {
        _queue.Enqueue(action);
    }

    private void Update()
    {
        while (_queue.TryDequeue(out var action))
        {
            action?.Invoke();
        }
    }
}