using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 观察者模式
/// </summary>
public class EventManager : Singleton<EventManager> {
    // 直接存储 Delegate，利用 C# 原生多播委托特性
    private Dictionary<int, Delegate> eventMap = new Dictionary<int, Delegate>();

    private void Register(int id, Delegate handler) {
        if (handler == null) {
            Debug.LogError($"EventManager: 事件 ID {id} 的委托为空");
            return;
        }

        if (eventMap.TryGetValue(id, out var value)) {
            eventMap[id] = Delegate.Combine(value, handler);
        } else {
            eventMap[id] = handler;
        }
    }

    private void Unregister(int id, Delegate handler) {
     if (handler == null) {
            Debug.LogError($"EventManager: 事件 ID {id} 的委托为空");
            return;
        }
        
        if (eventMap.TryGetValue(id, out var value)) {
            eventMap[id] = Delegate.Remove(value, handler);
        }
    }

    #region Register (注册)
    public void Register(int id, Action handler) => Register(id, (Delegate)handler);
    public void Register<T1>(int id, Action<T1> handler) => Register(id, (Delegate)handler);
    public void Register<T1, T2>(int id, Action<T1, T2> handler) => Register(id, (Delegate)handler);
    public void Register<T1, T2, T3>(int id, Action<T1, T2, T3> handler) => Register(id, (Delegate)handler);
    public void Register<T1, T2, T3, T4>(int id, Action<T1, T2, T3, T4> handler) => Register(id, (Delegate)handler);

    #endregion

    #region Unregister (注销)

    public void Unregister(int id, Action handler) => Unregister(id, (Delegate)handler);
    public void Unregister<T1>(int id, Action<T1> handler) => Unregister(id, (Delegate)handler);
    public void Unregister<T1, T2>(int id, Action<T1, T2> handler) => Unregister(id, (Delegate)handler);
    public void Unregister<T1, T2, T3>(int id, Action<T1, T2, T3> handler) => Unregister(id, (Delegate)handler);
    public void Unregister<T1, T2, T3, T4>(int id, Action<T1, T2, T3, T4> listener) => Unregister(id, (Delegate)listener);

    #endregion

    public void Dispatch(int id) {
        if (eventMap.TryGetValue(id, out var d)) {
            if (d is Action callback) {
                callback();
            } else {
                LogError(id, d, typeof(Action));
            }
        }
    }

    public void Dispatch<T1>(int id, T1 param) {
        if (eventMap.TryGetValue(id, out var d)) {
            if (d is Action<T1> callback) {
                callback(param);
            } else {
                LogError(id, d, typeof(Action<T1>));
            }
        }
    }

    public void Dispatch<T1, T2>(int id, T1 param1, T2 param2) {
        if (eventMap.TryGetValue(id, out var d)) {
            if (d is Action<T1, T2> callback) {
                callback(param1, param2);
            } else {
                LogError(id, d, typeof(Action<T1, T2>));
            }
        }
    }

    public void Dispatch<T1, T2, T3>(int id, T1 param1, T2 param2, T3 param3) {
        if (eventMap.TryGetValue(id, out var d)) {
            if (d is Action<T1, T2, T3> callback) {
                callback(param1, param2, param3);
            } else {
                LogError(id, d, typeof(Action<T1, T2, T3>));
            }
        }
    }

    public void Dispatch<T1, T2, T3, T4>(int id, T1 param1, T2 param2, T3 param3, T4 param4) {
        if (eventMap.TryGetValue(id, out var d)) {
            if (d is Action<T1, T2, T3, T4> callback) {
                callback(param1, param2, param3, param4);
            } else {
                LogError(id, d, typeof(Action<T1, T2, T3, T4>));
            }
        }
    }

    private void LogError(int id, Delegate d, Type expectedType) {
        Debug.LogError($"EventManager: 事件 ID {id} 类型不匹配。\n" +
                       $"期望: {expectedType}\n" +
                       $"实际: {d.GetType()}");
    }
}