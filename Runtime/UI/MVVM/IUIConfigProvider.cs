using System;

namespace Scripts.Framework.UI {
    /// <summary>
    /// UI 配置提供者接口
    /// 框架通过此接口获取UI预制体路径和层级配置
    /// 具体实现由业务层提供，框架不依赖具体配置来源
    /// </summary>
    public interface IUIConfigProvider {
        /// <summary>
        /// 根据ViewModel名称获取UI配置
        /// </summary>
        /// <param name="viewModelName">ViewModel类名，如 "MainMenuViewModel"</param>
        /// <param name="assetPath">输出：UI预制体的资源路径</param>
        /// <param name="defaultLayer">输出：UI默认层级</param>
        /// <returns>是否找到配置</returns>
        bool TryGetUIConfig(string viewModelName, out string assetPath, out UILayer defaultLayer);
    }
}