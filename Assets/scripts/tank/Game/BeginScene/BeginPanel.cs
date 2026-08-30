using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BeginPanel : BasePanel<BeginPanel>
{

    public CustomGUIButton btnBegin;
    public CustomGUIButton btnSetting;
    public CustomGUIButton btnQuit;
    public CustomGUIButton btnRank;



    // Start is called before the first frame update
    void Start()
    {
        //目的是为了方便控制坦克的头部转向  所有锁定鼠标在窗口内
        Cursor.lockState= CursorLockMode.Confined;

        //读取存档（Json+异或加密）：Console可见历史最高分
        SaveManager.Instance.Load();

        btnBegin.clickEvent += () =>
        {
          //异步切换场景（原为同步LoadScene会卡帧，改由SceneMgr统一管理，后续可挂加载界面）
          SceneMgr.Instance.LoadScene("Gamescene", null);
        };

        btnSetting.clickEvent += () =>
        {
            SettingPanel.Instance.ShowMe();
        };

        btnQuit.clickEvent += () =>
        {
            Application.Quit();
        };

        btnRank.clickEvent += () =>
        {
            RankPanel.Instance.ShowMe();
            HideMe();
        };

        //联机对战入口：UGUI按钮依赖EventSystem（本场景教程UI是IMGUI体系，没有它——自动补建）
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        GameObject lanCanvas = new GameObject("LanEntryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        lanCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        UIFactory.CreateButton(lanCanvas.transform, "联 机 对 战", new Vector2(340, 76), new Vector2(0, -400),
            new Color(0.45f, 0.3f, 0.6f), () => LanPanel.Instance.Show());
        // Update is called once per frame
      
}

}