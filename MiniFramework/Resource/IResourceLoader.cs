using UnityEngine;

namespace Scripts.Framework.Resource {
    public interface IResourceLoader {
        void Initialize();
        void LoadAssetAsync<T>(string key, System.Action<T> onComplete, System.Action<float> onProgress = null) where T : Object;
        void UnloadAsset(string key);
        void UnloadAll();
    }
}