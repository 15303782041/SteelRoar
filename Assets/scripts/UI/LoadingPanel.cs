using GameFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 加载进度面板：监听SceneMgr广播的Loading事件（0~1），进度条用宽度缩放显示（无Sprite依赖）。
/// 加载完成（进度=1）自动隐藏
/// </summary>
public class LoadingPanel : SingletonAutoMono<LoadingPanel>
{
    private GameObject panelRoot;
    private Image fill;
    private Text percent;
    private const float FullWidth = 600f;

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener(EEventType.Loading, OnLoading);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener(EEventType.Loading, OnLoading);
    }

    private void OnLoading(object info)
    {
        float progress = info is float f ? Mathf.Clamp01(f) : 0f;
        BuildOnce();
        panelRoot.SetActive(progress < 1f);          // 加载完成自动隐藏

        fill.rectTransform.sizeDelta = new Vector2(FullWidth * progress, 30);
        percent.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }

    private void BuildOnce()
    {
        if (panelRoot != null)
            return;

        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        panelRoot = new GameObject("LoadingPanel", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        //过继给跨场景存活的控制器物体：面板身体不再随场景切换被销毁
        panelRoot.transform.SetParent(transform, false);
        Canvas canvas = panelRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = panelRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Image bg = panelRoot.GetComponent<Image>();
        bg.rectTransform.sizeDelta = new Vector2(1920, 1080);
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);

        Text title = UIFactory.CreateText(panelRoot.transform, "加 载 中 …", 52, Color.white);
        title.rectTransform.sizeDelta = new Vector2(500, 70);
        title.rectTransform.anchoredPosition = new Vector2(0, 120);

        //进度条背景
        GameObject barBg = new GameObject("BarBg", typeof(Image));
        barBg.transform.SetParent(panelRoot.transform, false);
        Image barBgImg = barBg.GetComponent<Image>();
        barBgImg.rectTransform.sizeDelta = new Vector2(FullWidth, 30);
        barBgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        //进度填充（轴心左缘：宽度=进度比例）
        GameObject barFill = new GameObject("BarFill", typeof(Image));
        barFill.transform.SetParent(barBg.transform, false);
        fill = barFill.GetComponent<Image>();
        fill.rectTransform.pivot = new Vector2(0, 0.5f);
        fill.rectTransform.anchoredPosition = new Vector2(0, 0);
        fill.rectTransform.sizeDelta = new Vector2(0, 30);
        fill.color = new Color(0.3f, 0.8f, 0.45f, 1f);

        percent = UIFactory.CreateText(panelRoot.transform, "0%", 30, Color.white);
        percent.rectTransform.sizeDelta = new Vector2(300, 40);
        percent.rectTransform.anchoredPosition = new Vector2(0, -70);

        panelRoot.SetActive(false);
    }
}
