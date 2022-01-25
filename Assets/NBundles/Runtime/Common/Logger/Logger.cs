using UnityEngine;
using System.Diagnostics;
using System;

public static class Logger
{
    
    [Conditional("LOGGER_ON")]
    public static void Log(object o)
    {
#if UNITY_EDITOR
        UnityEngine.Debug.Log(o);
#else
        UnityEngine.Debug.Log(DateTime.Now.ToString("T") + " -- " + o);
#endif
    }
    
    [Conditional("LOGGER_ON")]
    public static void Log(string s, params object[] p)
    {
        string msg = (p != null && p.Length > 0 ? string.Format(s, p) : s);
        
#if UNITY_EDITOR
        UnityEngine.Debug.Log(msg);
#else
        UnityEngine.Debug.Log(DateTime.Now.ToString("T") + " -- " + msg);
#endif
    }

    [Conditional("LOGGER_ON")]
    public static void LogWarning(string s, params object[] p)
    {
        string msg = (p != null && p.Length > 0 ? string.Format(s, p) : s);
        
#if UNITY_EDITOR
        UnityEngine.Debug.LogWarning(msg);
#else
        UnityEngine.Debug.LogWarning(DateTime.Now.ToString("T") + " -- " + msg);
#endif
    }
    
    [Conditional("LOGGER_ON")]
    public static void Assert(bool condition, string s, params object[] p)
    {
        if (condition)
        {
            return;
        }

        LogError("Assert failed! Message:\n" + s, p);
    }

    /// <summary>
    /// 打印Error + 日志上报，（移动端才会上报）
    /// </summary>
    public static void LogError(string s, params object[] p)
    {
        string msg = (p != null && p.Length > 0 ? string.Format(s, p) : s);
        
#if UNITY_EDITOR
        UnityEngine.Debug.LogError(msg);
#else
        UnityEngine.Debug.LogError(DateTime.Now.ToString("T") + " -- " + msg);
#endif
    }

    
    static  Stopwatch watch = new Stopwatch();
    [Conditional("UNITY_EDITOR")]
    public static void Watch()
    {
#if UNITY_EDITOR
        watch.Reset();
        watch.Start();
#endif
    }

    public static long useTime
    {
        get
        {
#if UNITY_EDITOR
            return watch.ElapsedMilliseconds;
#else
            return 0;
#endif
        }
    }

    public static string useMemory
    {
        get
        {
            return (UnityEngine.Profiling.Profiler.usedHeapSizeLong / (1024 * 1024)).ToString() + " mb";
        }
    }

}