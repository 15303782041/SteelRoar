using GameFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 联机结算面板（胜利/战败 + 准备确认制重开）。
/// 联机不复用单机的WinPanel/LossPanel（它们带排行榜输入，是单机流程），
/// 重开规则：**双方都在本面板点"准备"才进新一局**——任一方单方面点重开就把对方拽进新局，
/// 输的人可能连结算都没看清。运行时构建UGUI，不受timeScale冻结影响
/// </summary>
public class PvPResultPanel : SingletonAutoMono<PvPResultPanel>
{
    private GameObject panelRoot;
    private Text titleText;
    private Text statusText;
    private Text readyBtnLabel;

    public void Show(bool win)
    {
        BuildOnce();
        EnsureEventSystem();

        titleText.text = win ? "胜  利" : "战  败";
        titleText.color = win ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.45f, 0.4f);
        statusText.text = win ? "对方坦克已被击毁" : "你的坦克已被击毁";
        readyBtnLabel.text = "准  备";
        readyBtnLabel.transform.parent.GetComponent<Button>().interactable = true;

        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>我方已点准备：按钮置灰，状态提示等待对方</summary>
    public void SetMyReady()
    {
        readyBtnLabel.text = "已准备";
        readyBtnLabel.transform.parent.GetComponent<Button>().interactable = false;
        statusText.text = "已准备，等待对方确认…";
    }

    /// <summary>对方已点准备：状态提示（若我方也已准备，由GameMgr直接开新局）</summary>
    public void SetPeerReady()
    {
        if (statusText.text.StartsWith("已准备"))
            return;                          // 我方已准备且面板已提示等待：双方都好了马上开局，不闪提示
        statusText.text = "对方已准备，点击「准备」开始新一局";
    }

    private void BuildOnce()
    {
        if (panelRoot != null)
        {
            EnsureEventSystem();
            return;
        }

        EnsureEventSystem();

        panelRoot = new GameObject("PvPResultPanel", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        panelRoot.transform.SetParent(transform, false);   // 过继给跨场景存活的控制器
        Canvas canvas = panelRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;         // 低于NetLostTip(10)：断线提示永远盖在结算面板上
        CanvasScaler scaler = panelRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Image bg = panelRoot.GetComponent<Image>();
        bg.rectTransform.sizeDelta = new Vector2(1920, 1080);
        bg.color = new Color(0.06f, 0.07f, 0.12f, 0.92f);

        titleText = UIFactory.CreateText(panelRoot.transform, "", 88, Color.white);
        titleText.rectTransform.sizeDelta = new Vector2(800, 120);
        titleText.rectTransform.anchoredPosition = new Vector2(0, 220);

        statusText = UIFactory.CreateText(panelRoot.transform, "", 34, new Color(1f, 0.85f, 0.4f));
        statusText.rectTransform.sizeDelta = new Vector2(1200, 50);
        statusText.rectTransform.anchoredPosition = new Vector2(0, 80);

        Button readyBtn = UIFactory.CreateButton(panelRoot.transform, "准  备", new Vector2(320, 76), new Vector2(-200, -100),
            new Color(0.2f, 0.55f, 0.35f), OnReadyClicked);
        readyBtnLabel = readyBtn.GetComponentInChildren<Text>();

        UIFactory.CreateButton(panelRoot.transform, "返回主菜单", new Vector2(320, 76), new Vector2(200, -100),
            new Color(0.35f, 0.35f, 0.4f), OnBackToMenu);

        panelRoot.SetActive(false);      // 构建完成默认隐藏
    }

    private void OnReadyClicked()
    {
        GameMgr.Instance.RequestRematchReady();
    }

    private void OnBackToMenu()
    {
        //先告知对方"我离开本局"（对方弹断开提示），再统一走流程管理器回主菜单
        if (NetCenter.Instance.Networking)
            NetCenter.Instance.NotifyLeaveAndShutdown();
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
