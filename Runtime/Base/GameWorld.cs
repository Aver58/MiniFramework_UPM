public class GameWorld {
    private string warId;
    private bool isDispose;
    private GameWorldFeatures baseFeatures;

    public GameWorld() {
        warId = string.Empty;
    }

    public void Init() {
        OnInit();

        var instance = UpdateRegister.Instance;// mono逻辑初始化
        baseFeatures = new GameWorldFeatures(this);
    }

    private void OnInit() {
        isDispose = false;

    }

    public void Clear() {
        if (isDispose) {
            return;
        }

        Dispose();
    }

    private void Dispose() {
        if (isDispose) {
            return;
        }

        isDispose = true;
    }

    public void AddBaseFeature<T>() where T : AbsBaseGameWorldFeature, new() {
        if (isDispose) {
            return;
        }
        baseFeatures.AddFeature<T>();
    }
}