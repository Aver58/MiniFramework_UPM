using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Scripts.Framework.UI {
    // Model基类（实现属性变更通知）
    public abstract class BaseModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // // 示例Model（玩家数据）
    // public class TestModel : BaseModel {
    //     private int hp;
    //     public int HP {
    //         get => hp;
    //         set {
    //             if (hp != value) {
    //                 hp = value;
    //                 RaisePropertyChanged();
    //             }
    //         }
    //     }
    // }
}