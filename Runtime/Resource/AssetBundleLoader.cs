using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Scripts.Framework.Resource {
    public class AssetBundleLoader : IResourceLoader {
        private ResourceConfig m_resourceConfig;
        private Dictionary<string, AssetBundle> m_loadedBundles = new Dictionary<string, AssetBundle>();
        private Dictionary<string, Object> m_loadedAssets = new Dictionary<string, Object>();

        public void Initialize() {
            m_resourceConfig = ResourceConfig.Instance;
            Debug.Log($"【AssetBundleLoader】Initialized. Load path: {m_resourceConfig.LocalAssetBundlePath}");

            if (!Directory.Exists(m_resourceConfig.LocalAssetBundlePath)) {
                Debug.LogWarning($"【AssetBundleLoader】AssetBundle directory not found: {m_resourceConfig.LocalAssetBundlePath}");
            }
        }

        public void LoadAssetAsync<T>(string key, System.Action<T> onComplete, System.Action<float> onProgress = null) where T : Object {
            if (m_loadedAssets.TryGetValue(key, out Object existingAsset)) {
                onComplete?.Invoke(existingAsset as T);
                return;
            }

            string bundleName = GetBundleName(key);
            string assetName = GetAssetName(key);

            if (m_loadedBundles.TryGetValue(bundleName, out AssetBundle bundle)) {
                LoadAssetFromBundle(bundle, assetName, onComplete, onProgress);
            } else {
                LoadBundleAsync(bundleName, assetName, onComplete, onProgress);
            }
        }

        private void LoadBundleAsync<T>(string bundleName, string assetName, System.Action<T> onComplete, System.Action<float> onProgress) where T : Object {
            string bundlePath = Path.Combine(m_resourceConfig.LocalAssetBundlePath, bundleName);
            string fullPath = bundlePath.EndsWith(".assetbundle") ? bundlePath : $"{bundlePath}.assetbundle";

            if (!File.Exists(fullPath)) {
                Debug.LogError($"【AssetBundleLoader】AssetBundle not found at: {fullPath}");
                onComplete?.Invoke(null);
                return;
            }

            Debug.Log($"【AssetBundleLoader】Loading bundle: {fullPath}");
            var request = AssetBundle.LoadFromFileAsync(fullPath);

            request.completed += operation => {
                var bundle = request.assetBundle;
                if (bundle == null) {
                    Debug.LogError($"【AssetBundleLoader】Failed to load AssetBundle: {fullPath}");
                    onComplete?.Invoke(null);
                    return;
                }

                m_loadedBundles[bundleName] = bundle;
                Debug.Log($"【AssetBundleLoader】Bundle loaded successfully: {bundleName}");

                LoadAssetFromBundle(bundle, assetName, onComplete, onProgress);
            };
        }

        private void LoadAssetFromBundle<T>(AssetBundle bundle, string assetName, System.Action<T> onComplete, System.Action<float> onProgress) where T : Object {
            var request = bundle.LoadAssetAsync<T>(assetName);

            request.completed += operation => {
                var asset = request.asset;
                if (asset == null) {
                    Debug.LogError($"【AssetBundleLoader】Asset not found in bundle: {assetName}");
                    onComplete?.Invoke(null);
                    return;
                }

                m_loadedAssets[$"{bundle.name}/{assetName}"] = asset;
                Debug.Log($"【AssetBundleLoader】Asset loaded successfully: {assetName}");
                onComplete?.Invoke(asset as T);
            };
        }

        public void UnloadAsset(string key) {
            if (m_loadedAssets.ContainsKey(key)) {
                m_loadedAssets.Remove(key);
                Debug.Log($"【AssetBundleLoader】Asset unloaded: {key}");
            }

            string bundleName = GetBundleName(key);
            if (m_loadedBundles.TryGetValue(bundleName, out AssetBundle bundle)) {
                bool hasOtherAssets = false;
                foreach (string loadedKey in m_loadedAssets.Keys) {
                    if (loadedKey.StartsWith(bundleName + "/")) {
                        hasOtherAssets = true;
                        break;
                    }
                }

                if (!hasOtherAssets) {
                    bundle.Unload(false);
                    m_loadedBundles.Remove(bundleName);
                    Debug.Log($"【AssetBundleLoader】Bundle unloaded: {bundleName}");
                }
            }
        }

        public void UnloadAll() {
            foreach (var bundle in m_loadedBundles.Values) {
                bundle.Unload(true);
            }
            m_loadedBundles.Clear();
            m_loadedAssets.Clear();
            Debug.Log("【AssetBundleLoader】All assets and bundles unloaded");
        }

        private string GetBundleName(string key) {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            int slashIndex = key.IndexOf('/');
            if (slashIndex == -1) return "main";
            return key.Substring(0, slashIndex);
        }

        private string GetAssetName(string key) {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            int slashIndex = key.IndexOf('/');
            if (slashIndex == -1) return key;
            return key.Substring(slashIndex + 1);
        }
    }
}