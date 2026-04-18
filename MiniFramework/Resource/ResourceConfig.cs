using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Scripts.Framework.Resource {
    [CreateAssetMenu(fileName = "ResourceConfig", menuName = "Config/ResourceConfig")]
    public class ResourceConfig : ScriptableObject {
        public enum LoadModeEnum {
            Addressables,
            AssetBundle,
            Editor // 编辑器模式，直接加载资源
        }

        [Header("加载配置")]
        public LoadModeEnum LoadMode = LoadModeEnum.Addressables;

        [Header("资源路径配置")]
        [Tooltip("远程资源服务器地址")]
        public string RemoteBaseURL = "http://localhost:8080/";

        [Tooltip("本地AssetBundle路径")]
        public string LocalAssetBundlePath = "Assets/StreamingAssets/AssetBundles";

        [Tooltip("UI资源路径前缀")]
        public string UIAssetPathPrefix = "UI/";

        [Header("缓存配置")]
        [Tooltip("最大内存缓存大小(MB)")]
        public int MaxCacheSizeMB = 500;

        [Tooltip("资源过期时间(秒)")]
        public float CacheExpireTime = 300;

        [Header("调试配置")]
        public bool DebugMode = false;

        private static ResourceConfig instance;
        public static ResourceConfig Instance {
            get {
                if (instance == null) {
                    instance = Resources.Load<ResourceConfig>("ResourceConfig");

                    if (instance == null) {
                        Debug.LogError("【ResourceConfig】ResourceConfig.asset not found! Please create one in Resources folder.");
                    }
                }

                return instance;
            }
        }

        public bool ValidatePathConfig() {
            if (LoadMode == LoadModeEnum.Addressables) {
                return true;
            }

            bool isValid = true;

            if (LoadMode == LoadModeEnum.AssetBundle) {
                if (string.IsNullOrEmpty(LocalAssetBundlePath)) {
                    Debug.LogError("【ResourceConfig】LocalAssetBundlePath cannot be empty in AssetBundle mode");
                    isValid = false;
                }
            }

            return isValid;
        }

        public string GetValidatedRemoteURL() {
            string url = RemoteBaseURL;
            if (!string.IsNullOrEmpty(url) && !url.EndsWith("/")) {
                url += "/";
            }
            return url;
        }

        public string GetAssetBundlePath() {
            return LocalAssetBundlePath.TrimEnd('/', '\\');
        }

        public string GetUIAssetPath(string uiName) {
            string prefix = UIAssetPathPrefix.Trim('/', '\\');
            return string.IsNullOrEmpty(prefix) ? uiName : $"{prefix}/{uiName}";
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (string.IsNullOrEmpty(LocalAssetBundlePath)) {
                LocalAssetBundlePath = "Assets/StreamingAssets/AssetBundles";
            }

            if (string.IsNullOrEmpty(RemoteBaseURL)) {
                RemoteBaseURL = "http://localhost:8080/";
            }

            if (string.IsNullOrEmpty(UIAssetPathPrefix)) {
                UIAssetPathPrefix = "UI/";
            }

            MaxCacheSizeMB = Mathf.Max(10, MaxCacheSizeMB);
            CacheExpireTime = Mathf.Max(60, CacheExpireTime);
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ResourceConfig))]
    public class ResourceConfigEditor : Editor {
        public override void OnInspectorGUI() {
            var config = (ResourceConfig)target;

            base.OnInspectorGUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("验证配置")) {
                bool isValid = config.ValidatePathConfig();
                if (isValid) {
                    EditorUtility.DisplayDialog("配置验证", "所有配置项验证通过！", "确定");
                }
            }

            if (GUILayout.Button("打开资源路径")) {
                if (config.ValidatePathConfig() && !string.IsNullOrEmpty(config.LocalAssetBundlePath)) {
                    EditorUtility.RevealInFinder(config.LocalAssetBundlePath);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("资源加载模式说明");
            EditorGUILayout.HelpBox("Addressables模式使用Unity的Addressables资源系统加载资源，AssetBundle模式直接加载本地AssetBundle文件，Editor模式直接加载AssetDatabase资源(仅用于编辑器调试)", MessageType.Info);
        }
    }
#endif
}