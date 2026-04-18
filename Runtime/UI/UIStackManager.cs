using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Framework.UI {
    /// <summary>
    /// UI栈管理器 - 管理UI打开/关闭历史，支持返回导航
    /// 使用方法:
    ///   方式1 - 直接调用 OpenAsync 的完整版本
    ///   UIFramework.OpenAsync<PlayerInfoViewModel>(vm => {}, UILayer.Normal, pushToStack: true, useObjectPool: true);
    ///
    ///   方式2 - 手动管理栈
    ///   UIStackManager.Instance.PushUI<PlayerInfoViewModel>();
    ///   UIStackManager.Instance.PopUI();
    /// </summary>
    public class UIStackManager : MonoSingleton<UIStackManager> {
        private Stack<UIStackEntry> uiStack = new();
        private Dictionary<Type, int> openCountMap = new(); // 追踪同类型UI的打开次数

        // 事件
        public event Action<Type> OnUIOpened;
        public event Action<Type> OnUIClosed;
        public event Action OnStackEmpty;

        private class UIStackEntry {
            public Type ViewModelType;
            public object ViewModel;
            public UILayer Layer;
            public Action<object> OnCloseCallback;
            public int InstanceId;
            public bool UseObjectPool;
            public string PoolKey; // 对象池中的key

            public UIStackEntry(Type vmType, object vm, UILayer layer, Action<object> onClose, int id, bool usePool = false, string poolKey = "") {
                ViewModelType = vmType;
                ViewModel = vm;
                Layer = layer;
                OnCloseCallback = onClose;
                InstanceId = id;
                UseObjectPool = usePool;
                PoolKey = poolKey;
            }
        }

        /// <summary>
        /// 打开UI并压入栈 (简化版 - 自动处理对象池和栈)
        /// </summary>
        public void PushUI<T>(Action<T> onOpen = null, UILayer layer = UILayer.Normal, bool useObjectPool = false) where T : BaseViewModel, new() {
            UIFramework.OpenAsync<T>(onOpen, layer, pushToStack: true, useObjectPool: useObjectPool);
        }

        /// <summary>
        /// 内部方法 - 由UIFramework调用 (从OpenAsync中自动调用)
        /// </summary>
        public void PushUIInternal(Type viewModelType, BaseViewModel viewModel, UILayer layer, bool useObjectPool = false) {
            int id = GetInstanceId(viewModelType);
            string poolKey = viewModelType.Name.Replace("ViewModel", "");

            uiStack.Push(new UIStackEntry(viewModelType, viewModel, layer, null, id, useObjectPool, poolKey));

            // 更新open count
            if (!openCountMap.ContainsKey(viewModelType)) {
                openCountMap[viewModelType] = 0;
            }
            openCountMap[viewModelType]++;

            OnUIOpened?.Invoke(viewModelType);

            DebugStack();
        }

        /// <summary>
        /// 关闭顶部UI并弹出栈
        /// </summary>
        public void PopUI() {
            if (uiStack.Count == 0) {
                Debug.LogWarning("UI栈为空，无法弹出");
                return;
            }

            var entry = uiStack.Pop();
            CloseUIEntry(entry);

            if (uiStack.Count == 0) {
                OnStackEmpty?.Invoke();
            }

            DebugStack();
        }

        /// <summary>
        /// 关闭指定类型的UI（搜索整个栈，关闭找到的第一个匹配）
        /// </summary>
        public void PopUI<T>() where T : BaseViewModel {
            var type = typeof(T);
            if (uiStack.Count == 0) return;

            // 临时保存弹出的元素，最后再恢复回去（除了我们要关闭的那个）
            var tempStack = new Stack<UIStackEntry>();
            bool found = false;

            while (uiStack.Count > 0) {
                var entry = uiStack.Pop();
                if (entry.ViewModelType == type) {
                    // 找到目标，关闭它
                    CloseUIEntry(entry);
                    found = true;
                    break;
                }
                tempStack.Push(entry);
            }

            // 恢复其他元素
            while (tempStack.Count > 0) {
                uiStack.Push(tempStack.Pop());
            }

            if (!found) {
                Debug.LogWarning($"UI栈中未找到类型 {type.Name}，无法关闭");
            }

            DebugStack();
        }

        /// <summary>
        /// 返回N个UI（支持多层返回）
        /// </summary>
        public void PopMultiple(int count) {
            for (int i = 0; i < count && uiStack.Count > 0; i++) {
                PopUI();
            }
        }

        /// <summary>
        /// 清空所有UI
        /// </summary>
        public void ClearAllUI() {
            while (uiStack.Count > 0) {
                PopUI();
            }
            openCountMap.Clear();
        }

        /// <summary>
        /// 获取栈顶UI的ViewModel
        /// </summary>
        public T PeekViewModel<T>() where T : BaseViewModel {
            if (uiStack.Count == 0) return null;

            var entry = uiStack.Peek();
            return entry.ViewModel as T;
        }

        /// <summary>
        /// 检查UI栈中是否包含某个类型
        /// </summary>
        public bool Contains<T>() where T : BaseViewModel {
            var type = typeof(T);
            foreach (var entry in uiStack) {
                if (entry.ViewModelType == type) return true;
            }
            return false;
        }

        /// <summary>
        /// 获取栈中UI数量
        /// </summary>
        public int GetStackCount() => uiStack.Count;

        /// <summary>
        /// 获取某个类型的打开count数
        /// </summary>
        public int GetOpenCount<T>() where T : BaseViewModel {
            var type = typeof(T);
            openCountMap.TryGetValue(type, out int count);
            return count;
        }

        // ===== 内部方法 =====
        private void CloseUIEntry(UIStackEntry entry) {
            var baseViewModel = entry.ViewModel as BaseViewModel;
            baseViewModel?.Close();
            DecreaseCount(entry.ViewModelType);

            // 如果启用了对象池，返回到对象池
            if (entry.UseObjectPool && !string.IsNullOrEmpty(entry.PoolKey)) {
                var viewModel = entry.ViewModel as BaseViewModel;
                if (viewModel != null && viewModel.ViewComponent != null) {
                    UIObjectPool.Instance.ReturnToPool(entry.PoolKey, viewModel.ViewComponent.gameObject);
                }
            }

            entry.OnCloseCallback?.Invoke(entry.ViewModel);
            OnUIClosed?.Invoke(entry.ViewModelType);
        }

        private void OpenCount<T>() where T : BaseViewModel {
            var type = typeof(T);
            if (!openCountMap.ContainsKey(type)) {
                openCountMap[type] = 0;
            }
            openCountMap[type]++;
        }

        private void DecreaseCount(Type type) {
            if (openCountMap.TryGetValue(type, out int count)) {
                openCountMap[type] = Mathf.Max(0, count - 1);
            }
        }

        private int GetInstanceId(Type type) {
            if (openCountMap.TryGetValue(type, out int count)) {
                return count + 1;
            }
            return 1;
        }

        private void DebugStack() {
            #if UNITY_EDITOR
            string stackInfo = "= UI Stack =";
            int index = 0;
            foreach (var entry in uiStack) {
                stackInfo += $"\n  [{index}] {entry.ViewModelType.Name} (Layer: {entry.Layer}, Pool: {entry.UseObjectPool})";
                index++;
            }
            stackInfo += $"\nTotal: {uiStack.Count}";
            Debug.Log(stackInfo);
            #endif
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            ClearAllUI();
        }
    }

    // ===== 辅助扩展方法 =====
    public static class UIFrameworkExtensions {
        public static void Close(this Type vmType) {
            var method = typeof(UIFramework).GetMethod("Close");
            var genericMethod = method.MakeGenericMethod(vmType);
            genericMethod.Invoke(null, null);
        }
    }
}

