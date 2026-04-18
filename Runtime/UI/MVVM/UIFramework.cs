using System;
using System.Collections.Generic;
using Scripts.Framework.Resource;
using UnityEngine;

namespace Scripts.Framework.UI {
    /// <summary>
    /// MVVM UI 框架 - 外部调用入口
    /// 使用方法:
    /// 1. 初始化: UIFramework.Instance.Initialize(configProvider); 在游戏启动时调用
    /// 2. 打开UI: UIFramework.OpenAsync<MainMenuViewModel>();
    /// 3. 关闭UI: UIFramework.Close<MainMenuViewModel>();
    /// </summary>
    public class UIFramework : MonoSingleton<UIFramework> {
        private IUIConfigProvider configProvider;
        private Dictionary<Type, BaseViewModel> openViewModels = new();
        private HashSet<Type> loadingTypes = new();
        private Canvas uiCanvas;

        /// <summary>
        /// UI画布根节点
        /// </summary>
        public Canvas UICanvas {
            get {
                if (uiCanvas == null) {
                    var obj = GameObject.Find("UICanvas");
                    if (obj != null) {
                        uiCanvas = obj.GetComponent<Canvas>();
                    } else {
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
            set { uiCanvas = value; }
        }

        /// <summary>
        /// 初始化框架，注入配置提供者
        /// </summary>
        /// <param name="provider">UI配置提供者（由业务层实现）</param>
        public void Initialize(IUIConfigProvider provider) {
            configProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// 检查是否已初始化
        /// </summary>
        public bool IsInitialized() => configProvider != null;

        /// <summary>
        /// 打开 UI 界面（简化版）
        /// </summary>
        public static void OpenAsync<T>(Action<T> onOpen = null) where T : BaseViewModel, new() {
            Instance.OpenAsyncInternal<T>(onOpen, UILayer.Normal, pushToStack: false, useObjectPool: false);
        }

        /// <summary>
        /// 打开 UI 界面（完整版 - 支持栈管理和对象池）
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
        private const string VIEW_MODEL_SUFFIX = "ViewModel";
        private const string MODEL_SUFFIX = "Model";
        private const string DEFAULT_MODEL_NAMESPACE = "Business.MVVM.Model.";

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

            // 检查是否初始化
            if (configProvider == null) {
                Debug.LogError("[UIFramework] 未初始化，请先调用 Initialize 注入配置提供者");
                return;
            }

            // 标记为正在加载
            loadingTypes.Add(type);

            // 从配置提供者获取配置
            string viewModelName = type.Name;
            if (!configProvider.TryGetUIConfig(viewModelName, out var assetPath, out var defaultLayer)) {
                Debug.LogError($"[UIFramework] 未找到UI配置: {viewModelName}");
                loadingTypes.Remove(type);
                return;
            }

            // 使用传入的layer覆盖配置
            if (layer != UILayer.Normal) {
                defaultLayer = layer;
            }

            ResourceManager.Instance.LoadAssetAsync<GameObject>(assetPath, o => {
                loadingTypes.Remove(type);

                if (o == null) {
                    Debug.LogError($"[UIFramework] 加载UI预制体失败: {assetPath}");
                    return;
                }

                var parent = UICanvas.transform;
                GameObject go;

                // 如果启用对象池，从池中获取
                if (useObjectPool) {
                    string key = viewModelName.Replace(VIEW_MODEL_SUFFIX, "");
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
                    Debug.LogError($"{go.name} prefab 未找到 BaseView 组件!");
                    if (!useObjectPool) {
                        Destroy(go);
                    } else {
                        string key = viewModelName.Replace(VIEW_MODEL_SUFFIX, "");
                        UIObjectPool.Instance.ReturnToPool(key, go);
                    }
                    return;
                }

                // 创建 ViewModel
                var viewModel = new TViewModel();

                // 反射创建 Model 实例
                var modelInstance = CreateModelInstance(viewModelName);
                if (modelInstance == null) {
                    Debug.LogError($"创建Model实例失败: {viewModelName.Replace(VIEW_MODEL_SUFFIX, MODEL_SUFFIX)}");
                    if (!useObjectPool) {
                        Destroy(go);
                    } else {
                        string key = viewModelName.Replace(VIEW_MODEL_SUFFIX, "");
                        UIObjectPool.Instance.ReturnToPool(key, go);
                    }
                    return;
                }

                // 绑定
                viewModel.Bind(view, modelInstance);

                // 初始化并打开
                view.Init(defaultLayer);
                viewModel.Init(defaultLayer);
                viewModel.Open();
                openViewModels[type] = viewModel;

                // 如果启用栈管理，自动入栈
                if (pushToStack) {
                    UIStackManager.Instance.PushUIInternal(type, viewModel, defaultLayer, useObjectPool);
                }

                onOpen?.Invoke(viewModel);
            });
        }

        /// <summary>
        /// 反射创建 Model 实例
        /// </summary>
        private BaseModel CreateModelInstance(string viewModelName) {
            string modelTypeName = viewModelName.Replace(VIEW_MODEL_SUFFIX, MODEL_SUFFIX);

            // 先尝试全局查找
            var modelType = Type.GetType(modelTypeName);
            if (modelType == null) {
                // 没找到，尝试默认命名空间
                var fullName = $"{DEFAULT_MODEL_NAMESPACE}{modelTypeName}";
                modelType = Type.GetType(fullName);
            }

            if (modelType == null) {
                Debug.LogError($"[UIFramework] 没找到 Model 类: {modelTypeName}，尝试过默认命名空间: {DEFAULT_MODEL_NAMESPACE}{modelTypeName}");
                return null;
            }

            return Activator.CreateInstance(modelType) as BaseModel;
        }

        private void CloseInternal<T>() where T : BaseViewModel {
            var type = typeof(T);
            if (openViewModels.TryGetValue(type, out var viewModel)) {
                viewModel.Close();
                openViewModels.Remove(type);

                // 如果使用对象池，回收
                // （栈管理会在 Pop 时处理回收，这里只处理非栈管理的直接关闭）
                if (viewModel.ViewComponent != null && !UIStackManager.Instance.Contains<T>()) {
                    string key = typeof(T).Name.Replace(VIEW_MODEL_SUFFIX, "");
                    UIObjectPool.Instance.ReturnToPool(key, viewModel.ViewComponent.gameObject);
                }
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            openViewModels.Clear();
            loadingTypes.Clear();
            configProvider = null;
        }
    }
}