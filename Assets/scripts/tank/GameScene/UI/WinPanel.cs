using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

public class WinPanel : BasePanel<WinPanel>
{
    //关联控件
    public CustomGUIInput inputInfo;
    public CustomGUIButton btnSure;
    // Start is called before the first frame update
    void Start()
    {
        btnSure.clickEvent += () =>
        {
            //把数据记录到排行榜中
            GameDataMgr.Instance.AddRankInfo(inputInfo.content.text,
                GamePanel.Instance.nowScore,
                GamePanel.Instance.nowTime);

            //返回主菜单统一走流程管理器（重置时钟/收起悬浮面板/异步加载）
            GameMgr.Instance.BackToBeginScene();
        };

        HideMe();
    }


}
