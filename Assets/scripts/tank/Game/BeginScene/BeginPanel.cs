using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

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

        //联机对战入口由LanEntry静态自举统一创建（与主菜单解耦，进战斗自动隐藏）——
        //本类绝不再自建入口按钮：两处各建一个=主菜单出现两个"联机对战"（Day N5实修）
              
}

}