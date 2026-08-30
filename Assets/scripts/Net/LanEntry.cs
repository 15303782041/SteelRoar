using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单"联机对战"入口：BeginScene自动显示，进入战斗自动隐藏。
/// 静态类+RuntimeInitializeOnLoadMethod自举——不依赖BeginPanel脚本版本，
/// 主菜单怎么改都不影响这个入口（解耦设计）
/// </summary>
public static class LanEntry
{
    private static GameObject canvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        //订阅场景加载事件：每次切场景后刷新入口显隐
        SceneManager.sceneLoaded += (scene, mode) => Refresh();
        Refresh();
    }

    private static void Refresh()
    {
        bool inBegin = SceneManager.GetActiveScene().name == "BeginScene";

        if (!inBegin)
        {
            if (canvas != null)
                canvas.SetActive(false);             // 进战斗：隐藏入口
            return;
        }

        EnsureEventSystem();
        if (canvas == null)
            Build();

        canvas.SetActive(true);                      // 回主菜单：显示入口
    }

    private static void Build()
    {
        canvas = new GameObject("LanEntryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvasComp = canvas.GetComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        //入口按钮：白底黑字，与教程菜单风格一致，位于"退出"按钮正下方
        GameObject btn = new GameObject("Btn_LanBattle", typeof(Image), typeof(Button));
        btn.transform.SetParent(canvas.transform, false);
        Image img = btn.GetComponent<Image>();
        img.rectTransform.sizeDelta = new Vector2(340, 70);
        img.rectTransform.anchoredPosition = new Vector2(0, -350);
        img.color = new Color(1f, 1f, 1f, 0.95f);

        Text label = CreateLabel(btn.transform, "联 机 对 战", 34, Color.black);
        label.rectTransform.sizeDelta = new Vector2(340, 70);

        btn.GetComponent<Button>().onClick.AddListener(() => LanPanel.Instance.Show());
    }

    /// <summary>BeginScene可能没有EventSystem（教程UI是IMGUI体系）——确保存在</summary>
    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    /// <summary>白底黑字按钮的专用文字（IMGUI菜单同款风格）</summary>
    private static Text CreateLabel(Transform parent, string content, int size, Color color)
    {
        GameObject go = new GameObject("Label", typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.text = content;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.raycastTarget = false;                     // 文字不挡按钮点击
        return t;
    }
}
