using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Framework.UI {
    /// <summary>
    /// UI对象池 - 用于复用UI对象，减少GC压力
    /// 支持预加载和自动扩展
    /// </summary>
    public class UIObjectPool : MonoSingleton<UIObjectPool> {
        private Dictionary<string, Queue<GameObject>> poolDict = new();
        private Dictionary<string, HashSet<GameObject>> activeDict = new();
        private Transform poolRoot;

        // 配置
        private const string POOL_ROOT_NAME = "UIObjectPool";
        private const int DEFAULT_POOL_SIZE = 10;

        private void Start() {
            InitializePoolRoot();
        }

        private void InitializePoolRoot() {
            if (poolRoot == null) {
                var existing = GameObject.Find(POOL_ROOT_NAME);
                if (existing != null) {
                    poolRoot = existing.transform;
                } else {
                    var poolGo = new GameObject(POOL_ROOT_NAME);
                    poolRoot = poolGo.transform;
                    DontDestroyOnLoad(poolGo);
                }
            }
        }

        /// <summary>
        /// 预加载对象池（需要传入预制体）
        /// </summary>
        public void PreloadPool(string key, GameObject prefab, int count) {
            InitializePoolRoot();
            if (!poolDict.ContainsKey(key)) {
                poolDict[key] = new Queue<GameObject>();
                activeDict[key] = new HashSet<GameObject>();
            }

            for (int i = 0; i < count; i++) {
                var obj = Instantiate(prefab, poolRoot);
                obj.SetActive(false);
                ReturnToPool(key, obj.gameObject);
            }

            Debug.Log($"预加载对象池 [{key}] 完成，数量: {count}");
        }

        /// <summary>
        /// 从池中获取对象，如果池中没有则返回null
        /// </summary>
        public GameObject GetFromPool(string key) {
            InitializePoolRoot();

            // 池不存在，创建空队列
            if (!poolDict.ContainsKey(key)) {
                poolDict[key] = new Queue<GameObject>();
                activeDict[key] = new HashSet<GameObject>();
                return null;
            }

            GameObject poolObj;
            if (poolDict[key].Count > 0) {
                poolObj = poolDict[key].Dequeue();
            } else {
                // 池中没有对象，返回null，由调用方从预制体实例化
                return null;
            }

            poolObj.SetActive(true);
            activeDict[key].Add(poolObj);
            poolObj.transform.SetParent(null);

            return poolObj;
        }

        /// <summary>
        /// 将对象返回到池
        /// </summary>
        public void ReturnToPool(string key, GameObject obj) {
            if (obj == null) return;

            if (!activeDict.ContainsKey(key)) {
                Destroy(obj);
                return;
            }

            activeDict[key].Remove(obj);
            obj.SetActive(false);
            obj.transform.SetParent(poolRoot);
            obj.transform.localPosition = Vector3.zero;

            if (!poolDict.ContainsKey(key)) {
                poolDict[key] = new Queue<GameObject>();
            }

            poolDict[key].Enqueue(obj);
        }

        /// <summary>
        /// 清空指定池
        /// </summary>
        public void ClearPool(string key) {
            if (poolDict.TryGetValue(key, out var queue)) {
                while (queue.Count > 0) {
                    Destroy(queue.Dequeue());
                }
                poolDict.Remove(key);
            }

            if (activeDict.ContainsKey(key)) {
                activeDict.Remove(key);
            }
        }

        /// <summary>
        /// 清空所有池
        /// </summary>
        public void ClearAllPools() {
            foreach (var kvp in poolDict) {
                while (kvp.Value.Count > 0) {
                    Destroy(kvp.Value.Dequeue());
                }
            }
            poolDict.Clear();
            activeDict.Clear();
        }

        /// <summary>
        /// 获取池的统计信息
        /// </summary>
        public void PrintPoolStats() {
            #if UNITY_EDITOR
            Debug.Log("=== UI对象池统计 ===");
            foreach (var kvp in poolDict) {
                int poolCount = kvp.Value.Count;
                int activeCount = activeDict[kvp.Key].Count;
                Debug.Log($"[{kvp.Key}] 空闲: {poolCount}, 使用中: {activeCount}, 总计: {poolCount + activeCount}");
            }
            #endif
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            ClearAllPools();
        }
    }

    /// <summary>
    /// 池化UI项目基类（用于列表项等可复用的UI元素）
    /// </summary>
    public abstract class PooledUIItem : MonoBehaviour {
        protected string poolKey;

        public virtual void OnGetFromPool() { }
        public virtual void OnReturnToPool() { }

        public void ReturnToPool() {
            OnReturnToPool();
            if (!string.IsNullOrEmpty(poolKey)) {
                UIObjectPool.Instance.ReturnToPool(poolKey, gameObject);
            }
        }
    }
}
