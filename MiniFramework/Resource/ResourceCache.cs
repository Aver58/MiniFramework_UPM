using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Framework.Resource {
    public class ResourceCache {
        private Dictionary<string, CacheItem> m_cache = new Dictionary<string, CacheItem>();
        private LinkedList<string> m_lruList = new LinkedList<string>();
        private int m_maxCacheSize = 100;
        private int m_cacheHits = 0;
        private int m_cacheMisses = 0;

        public int MaxCacheSize {
            get { return m_maxCacheSize; }
            set {
                m_maxCacheSize = Mathf.Max(10, value);
                TrimCacheToSize();
            }
        }

        public int CacheCount {
            get { return m_cache.Count; }
        }

        public int CacheHits {
            get { return m_cacheHits; }
        }

        public int CacheMisses {
            get { return m_cacheMisses; }
        }

        public float HitRate {
            get {
                int total = m_cacheHits + m_cacheMisses;
                return total > 0 ? (float)m_cacheHits / total : 0;
            }
        }

        public T Get<T>(string key) where T : Object {
            if (string.IsNullOrEmpty(key)) {
                Debug.LogError("【ResourceCache】Key cannot be null or empty!");
                return null;
            }

            if (m_cache.TryGetValue(key, out CacheItem item)) {
                m_cacheHits++;
                UpdateLRU(key);
                return item.Asset as T;
            }

            m_cacheMisses++;
            return null;
        }

        public void Add(string key, Object asset) {
            if (string.IsNullOrEmpty(key) || asset == null) {
                Debug.LogError("【ResourceCache】Key or asset cannot be null!");
                return;
            }

            if (m_cache.ContainsKey(key)) {
                var existingItem = m_cache[key];
                existingItem.Asset = asset;
                existingItem.AccessTime = Time.realtimeSinceStartup;
                UpdateLRU(key);
                return;
            }

            if (m_cache.Count >= m_maxCacheSize) {
                TrimCacheToSize();
            }

            var newItem = new CacheItem {
                Asset = asset,
                AccessTime = Time.realtimeSinceStartup
            };

            m_cache[key] = newItem;
            m_lruList.AddLast(key);
            Debug.Log($"【ResourceCache】Asset added to cache: {key}, Current size: {m_cache.Count}/{m_maxCacheSize}");
        }

        public void Remove(string key) {
            if (string.IsNullOrEmpty(key)) {
                Debug.LogError("【ResourceCache】Key cannot be null or empty!");
                return;
            }

            if (m_cache.Remove(key)) {
                RemoveFromLRU(key);
                Debug.Log($"【ResourceCache】Asset removed from cache: {key}, Current size: {m_cache.Count}/{m_maxCacheSize}");
            }
        }

        public void Clear() {
            m_cache.Clear();
            m_lruList.Clear();
            m_cacheHits = 0;
            m_cacheMisses = 0;
            Debug.Log("【ResourceCache】Cache cleared");
        }

        private void TrimCacheToSize() {
            while (m_cache.Count > m_maxCacheSize) {
                string oldestKey = m_lruList.First?.Value;
                if (oldestKey != null) {
                    Debug.Log($"【ResourceCache】Cache size limit reached, removing oldest asset: {oldestKey}");
                    Remove(oldestKey);
                }
            }
        }

        private void UpdateLRU(string key) {
            RemoveFromLRU(key);
            m_lruList.AddLast(key);
        }

        private void RemoveFromLRU(string key) {
            var node = m_lruList.First;
            while (node != null) {
                if (node.Value == key) {
                    m_lruList.Remove(node);
                    break;
                }
                node = node.Next;
            }
        }

        public void LogCacheStats() {
            Debug.Log($"【ResourceCache】Stats - Hits: {m_cacheHits}, Misses: {m_cacheMisses}, Hit Rate: {HitRate:P2}, Size: {m_cache.Count}/{m_maxCacheSize}");
        }

        private class CacheItem {
            public Object Asset { get; set; }
            public float AccessTime { get; set; }
        }
    }
}