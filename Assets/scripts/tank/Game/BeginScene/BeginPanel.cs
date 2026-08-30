using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 主菜单面板：原教程为CustomGUI/IMGUI体系，场景切回后按钮文字渲染异常（变底色），
/// 现整体迁移为UGUI运行时构建——与联机面板/暂停面板统一UI体系，文字不再丢失
/// </summary>
public class BeginPanel : BasePanel<BeginPanel>
{
    private GameObject menuCanvas;

    // Start is called before the first frame update
    void Start()
    {
        //目的是为了方便控制坦克的头部转向  所有锁定鼠标在窗口内
        Cursor.lockState = CursorLockMode.Confined;

        //读取存档（Json+异或加密）：Console可见历史最高分
        SaveManager.Instance.Load();

        //停用教程的IMGUI控件（场景切回后文字渲染异常的根源），整体换成UGUI菜单
        //注意：全场景搜索，但跳过设置/排行榜/退出子面板里的控件（它们的显隐流程还在用）
        foreach (var gui in FindObjectsOfType<CustomGUIControl>(true))
        {
            Transform t = gui.transform;
            bool inSubPanel = false;
            while (t != null)
            {
                string n = t.name;
                if (n.Contains("Setting") || n.Contains("Rank") || n.Contains("Quit"))
                {
                    inSubPanel = true;
                    break;
                }
                t = t.parent;
            }
            if (!inSubPanel)
                gui.enabled = false;
        }

        //UGUI按钮依赖EventSystem（本场景原为IMGUI体系没有它）
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        BuildMenu();
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

    private void OnBeginClick()
    {
        SceneMgr.Instance.LoadScene("Gamescene", null);
    }
}
