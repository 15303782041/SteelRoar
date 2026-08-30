using GameFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 断线提示面板：对方退出/心跳超时→告知原因并提供"返回主菜单"出口。
/// 运行时构建UGUI（LanPanel同款手法）；UGUI不受timeScale影响，
/// 即使对局已冻结（终局面板弹出）按钮依然可点
/// </summary>
public class NetLostTip : SingletonAutoMono<NetLostTip>
{
    private GameObject panelRoot;
    private Text reasonText;

    public void Show(string reason)
    {
        BuildOnce();
        EnsureEventSystem();
        reasonText.text = reason;
        panelRoot.SetActive(true);
    }

    private void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void BuildOnce()
    {
        if (panelRoot != null)
        {
            EnsureEventSystem();
            return;
        }

        EnsureEventSystem();

        panelRoot = new GameObject("NetLostTip", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        panelRoot.transform.SetParent(transform, false);   // 过继给跨场景存活的控制器
        Canvas canvas = panelRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;        // 高于PvPResultPanel(5)：断线提示必须盖在结算面板上
        CanvasScaler scaler = panelRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Image bg = panelRoot.GetComponent<Image>();
        bg.rectTransform.sizeDelta = new Vector2(1920, 1080);
        bg.color = new Color(0.06f, 0.07f, 0.12f, 0.92f);

        Text title = UIFactory.CreateText(panelRoot.transform, "连接已断开", 56, Color.white);
        title.rectTransform.sizeDelta = new Vector2(800, 80);
        title.rectTransform.anchoredPosition = new Vector2(0, 120);

        reasonText = UIFactory.CreateText(panelRoot.transform, "", 32, new Color(1f, 0.85f, 0.4f));
        reasonText.rectTransform.sizeDelta = new Vector2(1200, 60);
        reasonText.rectTransform.anchoredPosition = new Vector2(0, 20);

        UIFactory.CreateButton(panelRoot.transform, "返回主菜单", new Vector2(320, 72), new Vector2(0, -120),
            new Color(0.35f, 0.35f, 0.4f), OnBackToMenu);

        panelRoot.SetActive(false);        // 构建完成默认隐藏，Show时再激活
    }

    /// <summary>断线只可能发生在战斗场景（主菜单断线走静默清理），但按钮做场景分流更稳</summary>
    private void OnBackToMenu()
    {
        Hide();
        if (SceneManager.GetActiveScene().name == "GameScene")
            GameMgr.Instance.BackToBeginScene();
    }

    /// <summary>BeginScene可能没有EventSystem（教程UI是IMGUI体系）——进面板前确保存在</summary>
    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
