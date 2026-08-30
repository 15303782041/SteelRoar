using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LossPanel :BasePanel<LossPanel>
{
    public CustomGUIButton btnBack;
    public CustomGUIButton btnGoOn;
    // Start is called before the first frame update
    void Start()
    {
        btnBack.clickEvent += () =>
        {
            //联机：先告知对方"我离开本局"（对方弹提示并断开），再统一走流程管理器回主菜单
            if (NetCenter.Instance.Networking)
                NetCenter.Instance.NotifyLeaveAndShutdown();
            GameMgr.Instance.BackToBeginScene();
        };
        btnGoOn.clickEvent += () =>
        {
            //联机不走本面板（GameMgr终局分流到PvPResultPanel，准备确认制重开）；
            //保留兜底分支防止将来联机复用此面板时再犯"单人重开甩掉对方"的错误
            if (NetCenter.Instance.Networking)
            {
                GameMgr.Instance.RequestRematchReady();
                return;
            }
            //单机：再次切换到 游戏场景 就可以 达到所有内容重新加载 从头开始的 目的
            Time.timeScale = 0;
            SceneManager.LoadScene("GameScene");
        };
        HideMe();
    }

   
}
