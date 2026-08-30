using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitPanel : BasePanel<QuitPanel>
{
    public CustomGUIButton btnQuit;
    public CustomGUIButton btnGoOn;
    public CustomGUIButton btnClose;



    // Start is called before the first frame update
    void Start()
    {
        btnQuit.clickEvent += () =>
        {
           //战斗里=回主界面；主菜单里=真退出程序
           if (SceneManager.GetActiveScene().name == "BeginScene")
           {
#if UNITY_EDITOR
               //编辑器里Application.Quit无效果，用退出Play模式代替
               UnityEditor.EditorApplication.isPlaying = false;
#else
               Application.Quit();
#endif
           }
           else
           {
//联机：先告知对方"我离开本局"（对方弹提示并断开）
               if (NetCenter.Instance.Networking)
                   NetCenter.Instance.NotifyLeaveAndShutdown();
               //统一走流程管理器回主菜单：重置时钟+收起悬浮面板+异步加载
               GameMgr.Instance.BackToBeginScene();
           }
        };
        btnGoOn.clickEvent += () =>
        {
            HideMe();
        };
        btnClose.clickEvent += () =>
        {
            HideMe();
        };
        //开始时隐藏自己
        HideMe();
    }
    public override void HideMe()
    {
        base.HideMe();
        //隐藏时候时间就会重置回1了。
        Time.timeScale = 1;
    }

}
