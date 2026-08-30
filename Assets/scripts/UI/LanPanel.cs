using GameFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 联机面板：昵称输入 + 创建房间(主机监听7777) / 加入房间(输入主机IP)。
/// 双方握手成功（GuestJoined/JoinAcked）→ 自动进入GameScene开始对战。
/// 面板运行时创建（自动补EventSystem与GraphicRaycaster）
/// </summary>
public class LanPanel : SingletonAutoMono<LanPanel>
{
    private const int GamePort = 7777;

    private GameObject panelRoot;
    private InputField nameInput;
    private InputField ipInput;
    private Text status;

    public void Show()
    {
        BuildOnce();
        EnsureEventSystem();
        BeginPanel.Instance.HideMe();       // 藏起IMGUI主菜单：IMGUI永远画在UGUI之上，不藏会两层叠加
        panelRoot.SetActive(true);
        status.text = "创建房间等待对方，或输入主机IP加入";
    }

    /// <summary>收起面板（返回按钮），并还原IMGUI主菜单</summary>
    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        BeginPanel.Instance.ShowMe();
    }

    private void BuildOnce()
    {
        if (panelRoot != null)
        {
            EnsureEventSystem();
            return;
        }

        EnsureEventSystem();

        panelRoot = new GameObject("LanPanel", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        panelRoot.transform.SetParent(transform, false);   // 过继给跨场景存活的控制器
        Canvas canvas = panelRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = panelRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Image bg = panelRoot.GetComponent<Image>();
        bg.rectTransform.sizeDelta = new Vector2(1920, 1080);
        bg.color = new Color(0.06f, 0.07f, 0.12f, 0.97f);

        Text title = UIFactory.CreateText(panelRoot.transform, "联 机 对 战", 64, Color.white);
        title.rectTransform.sizeDelta = new Vector2(700, 80);
        title.rectTransform.anchoredPosition = new Vector2(0, 340);

        Text nameLabel = UIFactory.CreateText(panelRoot.transform, "你的昵称", 28, new Color(0.7f, 0.8f, 1f));
        nameLabel.rectTransform.sizeDelta = new Vector2(400, 36);
        nameLabel.rectTransform.anchoredPosition = new Vector2(-380, 240);
        nameInput = UIFactory.CreateInput(panelRoot.transform, "输入昵称（默认：玩家）", new Vector2(420, 56), new Vector2(-380, 180));

        UIFactory.CreateButton(panelRoot.transform, "创建房间（我是主机）", new Vector2(420, 72), new Vector2(-380, 70),
            new Color(0.2f, 0.55f, 0.35f), OnCreateRoom);

        Text ipLabel = UIFactory.CreateText(panelRoot.transform, "主机IP", 28, new Color(0.7f, 0.8f, 1f));
        ipLabel.rectTransform.sizeDelta = new Vector2(400, 36);
        ipLabel.rectTransform.anchoredPosition = new Vector2(380, 240);
        ipInput = UIFactory.CreateInput(panelRoot.transform, "输入主机IP（本机测试填127.0.0.1）", new Vector2(420, 56), new Vector2(380, 180));

        UIFactory.CreateButton(panelRoot.transform, "加入房间", new Vector2(420, 72), new Vector2(380, 70),
            new Color(0.25f, 0.4f, 0.7f), OnJoinRoom);

        status = UIFactory.CreateText(panelRoot.transform, "", 30, new Color(1f, 0.85f, 0.4f));
        status.rectTransform.sizeDelta = new Vector2(1200, 50);
        status.rectTransform.anchoredPosition = new Vector2(0, -60);

        UIFactory.CreateButton(panelRoot.transform, "返回", new Vector2(200, 60), new Vector2(0, -320),
            new Color(0.35f, 0.35f, 0.4f), Hide);

        //双方握手成功→都进入GameScene（联机模式下波次系统自动关闭）
        NetCenter.Instance.GuestJoined += _ => EnterGame("对方已加入，进入战场！");
        NetCenter.Instance.JoinAcked += _ => EnterGame("连接成功，进入战场！");
    }

    /// <summary>BeginScene可能没有EventSystem（教程UI是IMGUI体系）——进面板前确保存在</summary>
    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void OnCreateRoom()
    {
        NetCenter.Instance.Shutdown();      // 先清理旧会话：防止"端口被占用"（重复点击/上局未关）
        string myName = string.IsNullOrEmpty(nameInput.text) ? "主机" : nameInput.text;
        if (NetCenter.Instance.StartHost(GamePort, myName))
            status.text = $"房间已创建（端口{GamePort}），等待对方加入…";
        else
            status.text = $"端口{GamePort}被占用，创建失败";
    }

    private void OnJoinRoom()
    {
        NetCenter.Instance.Shutdown();      // 同上：加入前清理旧会话
        string myName = string.IsNullOrEmpty(nameInput.text) ? "客机" : nameInput.text;
        string ip = string.IsNullOrEmpty(ipInput.text) ? "127.0.0.1" : ipInput.text;

        if (NetCenter.Instance.JoinGuest(ip.Trim(), GamePort, myName))
        {
            status.text = "已连接，正在握手…";
            NetCenter.Instance.Send((ushort)MsgId.ReqJoin, new JoinPayload { name = myName });
        }
        else
        {
            status.text = $"连接失败：检查主机是否已创建房间、IP是否正确";
        }
    }

    private void EnterGame(string message)
    {
        status.text = message;
        panelRoot.SetActive(false);
        GameMgr.Instance.BeginRun();                     // 联机对局也走流程管理器（解冻/重置）
        SceneMgr.Instance.LoadScene("GameScene", null);
    }
}
