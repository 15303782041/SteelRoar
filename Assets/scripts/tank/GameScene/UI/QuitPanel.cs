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
           //回到主界面
           SceneManager.LoadScene("BeginScene");
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
