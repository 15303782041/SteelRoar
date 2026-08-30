using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 主菜单面板：双路径自愈设计——
/// ①场景数据完好时：沿用教程原版IMGUI按钮流程；
/// ②按钮序列化引用丢失时（本工程实际发生：历史保存剥掉了引用）：自动停用全部旧IMGUI控件、
///   运行时构建UGUI菜单（与联机/暂停面板同一体系）
/// </summary>
public class BeginPanel : BasePanel<BeginPanel>
{
    public CustomGUIButton btnBegin;
    public CustomGUIButton btnSetting;
    public CustomGUIButton btnQuit;
    public CustomGUIButton btnRank;

    private GameObject menuCanvas;

    // Start is called before the first frame update
    void Start()
    {
        //目的是为了方便控制坦克的头部转向  所有锁定鼠标在窗口内
        Cursor.lockState = CursorLockMode.Confined;

        //读取存档（Json+异或加密）：Console可见历史最高分
        SaveManager.Instance.Load();

        EnsureEventSystem();

        if (btnBegin != null)
        {
            //路径A：序列化链接完好，沿用教程原版IMGUI按钮流程
            btnBegin.clickEvent += OnBeginClick;
            btnSetting.clickEvent += () => SettingPanel.Instance.ShowMe();
            btnQuit.clickEvent += () => QuitPanel.Instance.ShowMe();
            btnRank.clickEvent += () =>
            {
                RankPanel.Instance.ShowMe();
                HideMe();
            };
        }
        else
        {
            //路径B：引用丢失（场景数据损坏）——停用旧IMGUI控件，构建UGUI菜单自愈
            DisableLegacyIMGUIControls();
            BuildMenu();
        }
    }

    /// <summary>BeginScene可能没有EventSystem（教程UI是IMGUI体系）——确保存在</summary>
    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    /// <summary>
    /// 停用全部旧IMGUI控件（主菜单按钮/Logo贴图），但保护其他子面板的控件
    /// （判定方式：父链上有"XXPanel"面板组件且不是BeginPanel，就属于子面板，跳过）
    /// </summary>
    private void DisableLegacyIMGUIControls()
    {
        foreach (var gui in FindObjectsOfType<CustomGUIControl>(true))
        {
            Transform t = gui.transform;
            bool belongsToOtherPanel = false;
            while (t != null)
            {
                foreach (var comp in t.GetComponents<Component>())
                {
                    if (comp == null) continue;          // missing script跳过
                    string tn = comp.GetType().Name;
                    if (tn.EndsWith("Panel") && tn != "BeginPanel")
                    {
                        belongsToOtherPanel = true;
                        break;
                    }
                }
                if (belongsToOtherPanel) break;
                t = t.parent;
            }
            if (!belongsToOtherPanel)
                gui.gameObject.SetActive(false);    // 禁用物体（配合DrawGUI卫语句，确保不再绘制）
        }
    }

    /// <summary>构建UGUI主菜单：标题/最高分/五个功能按钮（菜单Canvas挂在自身下，HideMe时一起隐藏）</summary>
    private void BuildMenu()
    {
        menuCanvas = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        menuCanvas.transform.SetParent(transform, false);   // 挂自身下：HideMe/ShowMe时菜单跟随显隐
        Canvas canvas = menuCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = menuCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        UIFactory.CreateText(menuCanvas.transform, "坦 克 迷 宫", 110, new Color(1f, 0.62f, 0.1f))
            .rectTransform.anchoredPosition = new Vector2(0, 330);

        UIFactory.CreateText(menuCanvas.transform, $"历史最高分：{SaveManager.Instance.NowData.highestScore}", 34, new Color(0.75f, 0.85f, 1f))
            .rectTransform.anchoredPosition = new Vector2(0, 220);

        UIFactory.CreateButton(menuCanvas.transform, "开 始 游 戏", new Vector2(420, 84), new Vector2(0, 100),
            new Color(0.2f, 0.55f, 0.35f), OnBeginClick);
        UIFactory.CreateButton(menuCanvas.transform, "设 置 游 戏", new Vector2(420, 84), new Vector2(0, 0),
            new Color(0.35f, 0.4f, 0.55f), () => SettingPanel.Instance.ShowMe());
        UIFactory.CreateButton(menuCanvas.transform, "排 行 榜", new Vector2(420, 84), new Vector2(0, -100),
            new Color(0.25f, 0.4f, 0.7f), () =>
            {
                RankPanel.Instance.ShowMe();
                HideMe();
            });
        UIFactory.CreateButton(menuCanvas.transform, "联 机 对 战", new Vector2(420, 84), new Vector2(0, -200),
            new Color(0.45f, 0.3f, 0.6f), () => LanPanel.Instance.Show());
        UIFactory.CreateButton(menuCanvas.transform, "退 出 游 戏", new Vector2(420, 84), new Vector2(0, -300),
            new Color(0.55f, 0.3f, 0.25f), () => QuitPanel.Instance.ShowMe());
    }

    /// <summary>开始游戏：异步加载战斗场景（统一走SceneMgr，自动带加载进度）</summary>
    private void OnBeginClick()
    {
        SceneMgr.Instance.LoadScene("Gamescene", null);
    }
}
