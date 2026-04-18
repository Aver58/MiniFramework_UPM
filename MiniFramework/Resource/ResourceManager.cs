using Scripts.Framework.Resource;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : Singleton<ResourceManager> {
    private IResourceLoader m_loader;
    private ResourceCache m_cache = new ResourceCache();
    private Dictionary<string, int> m_referenceCounts = new Dictionary<string, int>();
    private bool m_isInitialized = false;

    public bool IsInitialized => m_isInitialized;

    public ResourceManager() {
        Initialize();
    }

    private void Initialize() {
        try {
            var config = ResourceConfig.Instance;
            if (config == null) {
                Debug.LogError("【ResourceManager】ResourceConfig not found! Please create a ResourceConfig.asset in Resources folder.");
                return;
            }

            switch (config.LoadMode) {
                case ResourceConfig.LoadModeEnum.Addressables:
                    m_loader = new AddressableLoader();
                    Debug.Log("【ResourceManager】Using Addressables loader");
                    break;
                case ResourceConfig.LoadModeEnum.AssetBundle:
                    m_loader = new AssetBundleLoader();
                    Debug.Log("【ResourceManager】Using AssetBundle loader");
                    break;
                default:
                    Debug.LogError($"【ResourceManager】Unknown load mode: {config.LoadMode}");
                    return;
            }

            m_loader.Initialize();
            m_isInitialized = true;
            Debug.Log("【ResourceManager】Initialized successfully");
        } catch (Exception e) {
            Debug.LogError($"【ResourceManager】Initialization failed: {e.Message}\n{e.StackTrace}");
        }
    }

    public void LoadAssetAsync<T>(string key, Action<T> onComplete, Action<float> onProgress = null) where T : UnityEngine.Object {
        if (!m_isInitialized) {
            Debug.LogError("【ResourceManager】Not initialized!");
            onComplete?.Invoke(null);
            return;
        }

        if (string.IsNullOrEmpty(key)) {
            Debug.LogError("【ResourceManager】Key cannot be null or empty!");
            onComplete?.Invoke(null);
            return;
        }

        var cachedAsset = m_cache.Get<T>(key);
        if (cachedAsset != null) {
            Debug.Log($"【ResourceManager】Asset loaded from cache: {key}");
            AddReference(key);
            onComplete?.Invoke(cachedAsset);
            return;
        }

        Debug.Log($"【ResourceManager】Loading asset: {key}");
        m_loader.LoadAssetAsync<T>(key, asset => {
            if (asset != null) {
                m_cache.Add(key, asset);
                AddReference(key);
                Debug.Log($"【ResourceManager】Asset loaded successfully: {key}");
            } else {
                Debug.LogError($"【ResourceManager】Failed to load asset: {key}");
            }
            onComplete?.Invoke(asset);
        }, onProgress);
    }

    public void UnloadAsset(string key) {
        if (string.IsNullOrEmpty(key)) {
            Debug.LogError("【ResourceManager】Key cannot be null or empty!");
            return;
        }

        int refCount = RemoveReference(key);
        if (refCount <= 0) {
            Debug.Log($"【ResourceManager】Unloading asset: {key}");
            m_loader.UnloadAsset(key);
            m_cache.Remove(key);
            m_referenceCounts.Remove(key);
        } else {
            Debug.Log($"【ResourceManager】Asset still referenced (count: {refCount}), not unloading: {key}");
        }
    }

    public void UnloadAll() {
        Debug.Log("【ResourceManager】Unloading all assets");
        m_loader.UnloadAll();
        m_cache.Clear();
        m_referenceCounts.Clear();
    }

    public bool IsAssetLoaded(string key) {
        return m_cache.Get<UnityEngine.Object>(key) != null;
    }

    public int GetReferenceCount(string key) {
        if (m_referenceCounts.TryGetValue(key, out int count)) {
            return count;
        }
        return 0;
    }

    private void AddReference(string key) {
        if (m_referenceCounts.TryGetValue(key, out int count)) {
            m_referenceCounts[key] = count + 1;
        } else {
            m_referenceCounts[key] = 1;
        }
    }

    private int RemoveReference(string key) {
        if (m_referenceCounts.TryGetValue(key, out int count)) {
            count = Mathf.Max(0, count - 1);
            m_referenceCounts[key] = count;
            return count;
        }
        return 0;
    }
}