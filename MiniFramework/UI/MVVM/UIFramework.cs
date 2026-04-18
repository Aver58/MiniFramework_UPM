using System;
using System.Collections.Generic;
using Scripts.Framework.Resource;
using UnityEngine;

namespace Scripts.Framework.UI {
    /// <summary>
    /// MVVM UI 框架 - 外部调用接口
    /// 使用方法: UIFramework.Open<PlayerInfoView>();
    ///          UIFramework.Close<PlayerInfoView>();
    /// </summary>
    public class UIFramework : MonoSingleton<UIFramework> {
        private Dictionary<Type, BaseViewModel> openViewModels = new();
        private HashSet<Type> loadingTypes = new(); // 正在异步加载中的UI，防止重复打开
        private Canvas uiCanvas; // UI根节点
        public Canvas UICanvas {
            get {
                if (uiCanvas == null) {
                    var obj = GameObject.Find("UICanvas");
                    if (obj != null) {
                        uiCanvas = obj.GetComponent<Canvas>();
                    } else {
                        // 如果场景中找不到UICanvas，自动创建一个
                        Debug.LogWarning("[UIFramework] 场景中未找到UICanvas，自动创建");
                        var canvasObj = new GameObject("UICanvas");
                        uiCanvas = canvasObj.AddComponent<Canvas>();
                        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                        DontDestroyOnLoad(canvasObj);
                    }
                }

                return uiCanvas;
            }
        }

        /// <summary>
        /// 打开 UI 界面 (原方法 - 仅打开)
        /// </summary>
        public static void OpenAsync<T>(Action<T> onOpen = null) where T : BaseViewModel, new() {
            Instance.OpenAsyncInternal<T>(onOpen, UILayer.Normal, pushToStack: false, useObjectPool: false);
        }

        /// <summary>
        /// 打开 UI 界面 (完整版 - 支持栈管理和对象池)
        /// </summary>
        public static void OpenAsync<T>(Action<T> onOpen, UILayer layer = UILayer.Normal,
                                       bool pushToStack = false, bool useObjectPool = false)
            where T : BaseViewModel, new() {
            Instance.OpenAsyncInternal<T>(onOpen, layer, pushToStack, useObjectPool);
        }

        /// <summary>
        /// 关闭 UI 界面
        /// </summary>
        public static void Close<T>() where T : BaseViewModel {
            Instance.CloseInternal<T>();
        }

        /// <summary>
        /// 获取已打开的 UI 实例
        /// </summary>
        public static T GetViewModel<T>() where T : BaseViewModel {
            var type = typeof(T);
            if (Instance.openViewModels.TryGetValue(type, out var viewModel)) {
                return viewModel as T;
            }
            return null;
        }

        // ===== 内部实现 =====
        private const string VIEW_MODEL = "ViewModel";
        private const string MODEL = "Model";
        private const string NAMESPACE_PREFIX = "Business.MVVM.Model.";

        private void OpenAsyncInternal<TViewModel>(Action<TViewModel> onOpen, UILayer layer,
                                                   bool pushToStack, bool useObjectPool)
            where TViewModel : BaseViewModel, new() {
            var type = typeof(TViewModel);

            // 如果已打开，直接返回
            if (openViewModels.TryGetValue(type, out var vm)) {
                vm.Open();
                onOpen?.Invoke(vm as TViewModel);
                return;
            }

            // 如果正在加载中，防止重复打开
            if (loadingTypes.Contains(type)) {
                Debug.LogWarning($"[{type.Name}] 正在加载中，请勿重复打开");
                return;
            }

            // 标记为正在加载
            loadingTypes.Add(type);

            // 从资源加载 Prefab
            var prefabName = typeof(TViewModel).Name;
            var key = prefabName.Replace(VIEW_MODEL, "");
            // 实例化
            var config = StaticConfig.UIViewDefine.Get(key);
            if (config == null) {
                Debug.LogError($"{key} not found in UIViewDefineConfig.csv");
                loadingTypes.Remove(type);
                return;
            }

            var assetPath = $"{ResourceConfig.Instance.UIAssetPathPrefix}/{config.AssetName}.prefab";
            ResourceManager.Instance.LoadAssetAsync<GameObject>(assetPath, o => {
                loadingTypes.Remove(type);

                if (o == null) {
                    Debug.LogError($"Failed to load UI prefab: {assetPath}");
                    return;
                }

                var parent = UICanvas.transform;
                GameObject go;

                // 如果启用对象池，从池中获取
                if (useObjectPool) {
                    go = UIObjectPool.Instance.GetFromPool(key);
                    if (go == null) {
                        // 池中没有，创建新的
                        go = Instantiate(o, parent);
                    } else {
                        go.transform.SetParent(parent);
                        go.SetActive(true);
                    }
                } else {
                    go = Instantiate(o, parent);
                }

                var view = go.GetComponent<BaseView>();
                if (view == null) {
                    Debug.LogError($"{go} prefab not found BaseView component!");
                    if (!useObjectPool) {
                        Destroy(go);
                    } else {
                        UIObjectPool.Instance.ReturnToPool(key, go);
                    }
                    return;
                }

                // 创建 ViewModel
                var viewModel = new TViewModel();
                // 用名称反射出具体 Model 类型并实例化
                var modelTypeName = prefabName.Replace(VIEW_MODEL, MODEL);
                var modelType = Type.GetType(modelTypeName);
                if (modelType == null) {
                    // 如果没找到，尝试默认的 Model 命名空间
                    var fullModelTypeName = $"{NAMESPACE_PREFIX}{modelTypeName}";
                    modelType = Type.GetType(fullModelTypeName);
                }

                BaseModel modelInstance = null;
                if (modelType != null) {
                    modelInstance = Activator.CreateInstance(modelType) as BaseModel;
                    viewModel.Bind(view, modelInstance);
                } else {
                    Debug.LogError($"没找到具体数据类。类名 {modelTypeName}，命名空间下也找不到：Business.MVVM.Model.{modelTypeName}");
                    // Model创建失败，清理GameObject
                    if (!useObjectPool) {
                        Destroy(go);
                    } else {
                        UIObjectPool.Instance.ReturnToPool(key, go);
                    }
                    return;
                }

                if (modelInstance == null) {
                    Debug.LogError($"创建Model实例失败: {modelTypeName}");
                    if (!useObjectPool) {
                        Destroy(go);
                    } else {
                        UIObjectPool.Instance.ReturnToPool(key, go);
                    }
                    return;
                }

                var uiLayer = (UILayer)config.UILayer;
                if (layer != UILayer.Normal) {
                    uiLayer = layer; // 使用传入的layer参数覆盖配置
                }
                viewModel.Init(uiLayer);
                viewModel.Open();
                openViewModels[type] = viewModel;

                // 如果启用栈管理，自动入栈
                if (pushToStack) {
                    UIStackManager.Instance.PushUIInternal(type, viewModel, uiLayer, useObjectPool);
                }

                onOpen?.Invoke(viewModel);
            });
        }

        private void CloseInternal<T>() where T : BaseViewModel {
            var type = typeof(T);
            if (openViewModels.TryGetValue(type, out var viewModel)) {
                viewModel.Close();
                openViewModels.Remove(type);
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            openViewModels.Clear();
            loadingTypes.Clear();
        }
    }
}

