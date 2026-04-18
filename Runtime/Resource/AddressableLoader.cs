using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

namespace Scripts.Framework.Resource {
    public class AddressableLoader : IResourceLoader {
        private Dictionary<string, AsyncOperationHandle> m_loadingHandles = new Dictionary<string, AsyncOperationHandle>();
        private Dictionary<string, Object> m_loadedAssets = new Dictionary<string, Object>();

        public void Initialize() {
            var initOperation = Addressables.InitializeAsync();
            initOperation.Completed += handle => {
                if (handle.Status == AsyncOperationStatus.Succeeded) {
                    Debug.Log("【AddressableLoader】Initialize succeeded");
                } else {
                    Debug.LogError("【AddressableLoader】Initialize failed");
                }
            };
        }

        public void LoadAssetAsync<T>(string key, System.Action<T> onComplete, System.Action<float> onProgress = null) where T : Object {
            if (m_loadedAssets.TryGetValue(key, out Object existingAsset)) {
                onComplete?.Invoke(existingAsset as T);
                return;
            }

            if (m_loadingHandles.ContainsKey(key)) {
                Debug.LogWarning($"【AddressableLoader】Asset already loading: {key}");
                return;
            }

            var loadOperation = Addressables.LoadAssetAsync<T>(key);
            m_loadingHandles[key] = loadOperation;

            loadOperation.Completed += handle => {
                m_loadingHandles.Remove(key);

                if (handle.Status == AsyncOperationStatus.Succeeded) {
                    m_loadedAssets[key] = handle.Result;
                    Debug.Log($"【AddressableLoader】Asset loaded successfully: {key}");
                    onComplete?.Invoke(handle.Result);
                } else {
                    Debug.LogError($"【AddressableLoader】Failed to load asset: {key}, Error: {handle.OperationException}");
                    onComplete?.Invoke(null);
                }
            };

            if (onProgress != null) {
                UpdateProgress(loadOperation, onProgress);
            }
        }

        private async void UpdateProgress(AsyncOperationHandle operation, System.Action<float> onProgress) {
            while (!operation.IsDone) {
                onProgress?.Invoke(operation.PercentComplete);
                await System.Threading.Tasks.Task.Yield();
            }
            onProgress?.Invoke(1.0f);
        }

        public void UnloadAsset(string key) {
            if (m_loadingHandles.TryGetValue(key, out AsyncOperationHandle handle)) {
                Addressables.Release(handle);
                m_loadingHandles.Remove(key);
            }

            if (m_loadedAssets.TryGetValue(key, out Object asset)) {
                Addressables.Release(asset);
                m_loadedAssets.Remove(key);
                Debug.Log($"【AddressableLoader】Asset unloaded: {key}");
            }
        }

        public void UnloadAll() {
            foreach (var handle in m_loadingHandles.Values) {
                Addressables.Release(handle);
            }
            m_loadingHandles.Clear();

            foreach (var asset in m_loadedAssets.Values) {
                Addressables.Release(asset);
            }
            m_loadedAssets.Clear();

            Debug.Log("【AddressableLoader】All assets unloaded");
        }
    }
}