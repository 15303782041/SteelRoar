using GameFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 暂停面板：ESC呼出/恢复（GameMgr.TogglePause触发）。
/// 面板运行时创建（含自动补建EventSystem与GraphicRaycaster）
/// </summary>
public class PausePanel : SingletonAutoMono<PausePanel>
{
    private GameObject panelRoot;
    private bool built = false;

    public void ShowMe()
    {
        BuildOnce();
        panelRoot.SetActive(true);
    }

    public void HideMe()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void BuildOnce()
    {
        if (built)
            return;
        built = true;

        //UGUI按钮依赖EventSystem：场景里没有就自动补建
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        panelRoot = new GameObject("PausePanel", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        //过继给跨场景存活的控制器物体：面板身体不再随场景切换被销毁
        panelRoot.transform.SetParent(transform, false);
        Canvas canvas = panelRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = panelRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Image bg = panelRoot.GetComponent<Image>();
        bg.rectTransform.sizeDelta = new Vector2(1920, 1080);
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        Text title = UIFactory.CreateText(panelRoot.transform, "已 暂 停", 72, Color.white);
        title.rectTransform.sizeDelta = new Vector2(600, 90);
        title.rectTransform.anchoredPosition = new Vector2(0, 220);

        UIFactory.CreateButton(panelRoot.transform, "继续游戏", new Vector2(360, 80), new Vector2(0, 80),
            new Color(0.2f, 0.55f, 0.35f), () => GameMgr.Instance.TogglePause());
        UIFactory.CreateButton(panelRoot.transform, "返回主菜单", new Vector2(360, 80), new Vector2(0, -60),
            new Color(0.55f, 0.3f, 0.25f), () => GameMgr.Instance.BackToBeginScene());

        panelRoot.SetActive(false);
    }
}
