using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        btnBegin.clickEvent += () => 
        {
          //切换场景
          SceneManager.LoadScene("Gamescene");
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
        // Update is called once per frame
      
}

}