using Scripts.Framework.UI;
using Scripts.TetrisGame;
using UnityEngine;

public class GameRoot : MonoBehaviour {
    private void Start() {
        // 加载资源示例
        // ResourceManager.Instance.LoadAssetAsync<GameObject>("Assets/ToBundle/Prefabs/MyPrefab.prefab", obj => {
        //     if (obj != null) {
        //         var go = Instantiate(obj);
        //         go.transform.localPosition = Vector3.zero;
        //     }
        // });

        // 卸载资源示例
        // ResourceManager.Instance.UnloadAsset("Assets/ToBundle/Prefabs/MyPrefab.prefab");

        // 注册事件示例
        // EventManager.Instance.Register<string>(EventConstantId.OnTestEvent, OnTestEvent);
        // EventManager.Instance.Register<string>(EventConstantId.OnTestEvent, OnTestEvent2);

        // 触发事件示例
        // EventManager.Instance.Dispatch(EventConstantId.OnTestEvent, "Hello, World!");
        // EventManager.Instance.Unregister<string>(EventConstantId.OnTestEvent, OnTestEvent);
        // EventManager.Instance.Dispatch(EventConstantId.OnTestEvent, "Hello, World!2");
        GameWorld gameWorld = new GameWorld();
        gameWorld.Init();
        // ControllerManager.Instance.OpenAsync<PetController>();

        // 全量加载配置示例
        // var tables = new cfg.Tables(StaticConfig.LoadByteBuf);
        // UnityEngine.Debug.LogFormat("item[1].name:{0}", tables.Tbitem[1001].Name);

        // 单表加载配置示例
        // var tbTest = new cfg.TbTest(StaticConfig.LoadByteBuf("tbtest"));
        // Debug.Log($"name:{tbTest.Get(1002).Test1}");
        // Debug.Log($"name:{StaticConfig.Test.Get(1002).Test1}");

        // 加载界面示例
        UIFramework.OpenAsync<TestViewModel>();
    }

    private void OnTestEvent2(string message) {
        Debug.Log($"Received event message2: {message}");
    }

    void OnDestroy() {
        // 注销事件示例
        // if (EventManager.Instance == null) {
        //     return;
        // }
        //
        // EventManager.Instance.Unregister<string>(EventConstantId.OnTestEvent, OnTestEvent);
    }

    private void OnTestEvent(string message) {
        Debug.Log("Received event message: " + message);
    }
}