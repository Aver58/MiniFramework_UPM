using UnityEngine;
using UnityEngine.UI;

namespace Scripts.Framework.UI {
    public abstract class BaseView : MonoBehaviour {
        // 所属层级
        public UILayer Layer = UILayer.Normal;

        // 生命周期状态
        public UIViewState ViewState { get; private set; } = UIViewState.Closed;

        // 生命周期事件
        public event System.Action OnInitializeEvent;
        public event System.Action OnOpenEvent;
        public event System.Action OnCloseEvent;
        public event System.Action OnShowEvent;
        public event System.Action OnHideEvent;
        public event System.Action OnDestroyEvent;

        private bool isInitialized = false;
        private bool isOnShow = false;

        public void Init(UILayer layer) {
            if (isInitialized) return;

            UILayerController.SetLayerOrder(this.transform, layer);
            var graphicRaycaster = transform.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null) {
                graphicRaycaster = transform.gameObject.AddComponent<GraphicRaycaster>();
            }

            ViewState = UIViewState.Initialized;
            isInitialized = true;

            OnInitialize();
            OnInitializeEvent?.Invoke();
        }

        public void Open() {
            if (!isInitialized) {
                Debug.LogError($"[{name}] Open() called before Init()");
                return;
            }

            if (ViewState == UIViewState.Open) return;

            OnBeforeOpen();

            ViewState = UIViewState.Open;
            gameObject.SetActive(true);

            OnOpen();
            OnOpenEvent?.Invoke();

            Show();
        }

        public void Close() {
            if (ViewState == UIViewState.Closed) return;

            OnBeforeClose();

            Hide();

            ViewState = UIViewState.Closed;
            gameObject.SetActive(false);

            OnClose();
            OnCloseEvent?.Invoke();
        }

        public void Show() {
            if (isOnShow) return;
            isOnShow = true;

            OnShow();
            OnShowEvent?.Invoke();
        }

        public void Hide() {
            if (!isOnShow) return;
            isOnShow = false;

            OnHide();
            OnHideEvent?.Invoke();
        }

        public void Clear() {
            isInitialized = false;
            isOnShow = false;
            ViewState = UIViewState.Closed;

            OnClear();
            OnDestroyEvent?.Invoke();
        }

        // ===== 生命周期方法 - 供子类重写 =====
        /// <summary>初始化时调用（仅一次）</summary>
        protected virtual void OnInitialize() { }

        /// <summary>打开前调用</summary>
        protected virtual void OnBeforeOpen() { }

        /// <summary>打开时调用</summary>
        protected virtual void OnOpen() { }

        /// <summary>显示时调用</summary>
        protected virtual void OnShow() { }

        /// <summary>隐藏前调用</summary>
        protected virtual void OnHide() { }

        /// <summary>关闭前调用</summary>
        protected virtual void OnBeforeClose() { }

        /// <summary>关闭时调用</summary>
        protected virtual void OnClose() { }

        /// <summary>销毁时调用</summary>
        protected virtual void OnClear() { }

        // ===== 工具方法 =====
        /// <summary>获取UI是否处于打开状态</summary>
        public bool IsOpen() => ViewState == UIViewState.Open;

        /// <summary>获取UI是否处于显示状态</summary>
        public bool IsShown() => isOnShow;
    }

    /// <summary>UI视图状态枚举</summary>
    public enum UIViewState {
        Closed = 0,       // 已关闭
        Initialized = 1,  // 已初始化
        Open = 2          // 已打开
    }
}