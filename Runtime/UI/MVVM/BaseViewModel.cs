using System;
using System.Collections.Generic;

namespace Scripts.Framework.UI {
    // ViewModel 基类（MVVM框架的核心）
    // 属性变更通知由 BaseModel 提供，这里作为 ViewModel 使用
    public abstract class BaseViewModel {
        protected BaseView View;
        protected BaseModel Model;
        private Dictionary<string, Action<BaseModel>> propertyBindingMap = new();

        // 公开只读属性（供UIStackManager和对象池使用）
        public BaseView ViewComponent => View;
        public event Action OnBindEvent;
        public event Action OnInitEvent;
        public event Action OnOpenEvent;
        public event Action OnCloseEvent;
        public event Action OnShowEvent;
        public event Action OnHideEvent;
        public event Action OnUnbindEvent;

        public virtual void Bind(BaseView view, BaseModel model) {
            View = view;
            Model = model;
            if (Model != null) {
                Model.PropertyChanged += OnModelPropertyChanged;
            }

            // 连接View生命周期事件
            if (View != null) {
                View.OnInitializeEvent += RaiseOnInit;
                View.OnOpenEvent += RaiseOnOpen;
                View.OnCloseEvent += RaiseOnClose;
                View.OnShowEvent += RaiseOnShow;
                View.OnHideEvent += RaiseOnHide;
            }

            OnBind();
            OnBindEvent?.Invoke();
        }

        /// <summary>
        /// 注册属性绑定（自动更新 UI）
        /// </summary>
        protected void RegisterBinding(string propertyName, Action<BaseModel> updateAction) {
            propertyBindingMap[propertyName] = updateAction;
        }

        // 当Model属性变更时调用
        protected virtual void OnModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (propertyBindingMap.TryGetValue(e.PropertyName, out var action)) {
                action?.Invoke(Model);
            }

            OnPropertyChanged(e.PropertyName);
        }

        protected virtual void OnPropertyChanged(string propertyName) { }
        protected abstract void OnBind();

        public virtual void Unbind() {
            if (Model != null) {
                Model.PropertyChanged -= OnModelPropertyChanged;
            }

            if (View != null) {
                View.OnInitializeEvent -= RaiseOnInit;
                View.OnOpenEvent -= RaiseOnOpen;
                View.OnCloseEvent -= RaiseOnClose;
                View.OnShowEvent -= RaiseOnShow;
                View.OnHideEvent -= RaiseOnHide;
            }

            OnUnbind();
            OnUnbindEvent?.Invoke();
        }

        public void Open() { View?.Open(); }
        public void Close() { View?.Close(); }
        public void Show() { View?.Show(); }
        public void Hide() { View?.Hide(); }
        public void Init(UILayer layer) { View?.Init(layer); }
        public void Clear() {
            Unbind();
            View?.Clear();
        }

        // ===== 生命周期方法 - 供子类重写 =====

        /// <summary>解绑时调用</summary>
        protected virtual void OnUnbind() { }

        /// <summary>初始化时调用</summary>
        protected virtual void OnInit() { }

        /// <summary>打开时调用</summary>
        protected virtual void OnOpen() { }

        /// <summary>关闭时调用</summary>
        protected virtual void OnClose() { }

        /// <summary>显示时调用</summary>
        protected virtual void OnShow() { }

        /// <summary>隐藏时调用</summary>
        protected virtual void OnHide() { }

        // ===== 内部方法 =====
        private void RaiseOnInit() { OnInit(); OnInitEvent?.Invoke(); }
        private void RaiseOnOpen() { OnOpen(); OnOpenEvent?.Invoke(); }
        private void RaiseOnClose() { OnClose(); OnCloseEvent?.Invoke(); }
        private void RaiseOnShow() { OnShow(); OnShowEvent?.Invoke(); }
        private void RaiseOnHide() { OnHide(); OnHideEvent?.Invoke(); }

        // ===== 工具方法 =====
        /// <summary>检查View是否已初始化</summary>
        public bool IsViewInitialized() => View != null && View.IsOpen();

        /// <summary>检查View是否可见</summary>
        public bool IsViewVisible() => View != null && View.IsShown();
    }
}