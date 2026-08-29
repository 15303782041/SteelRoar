using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class GamePanel : BasePanel<GamePanel>
{
    // Start is called before the first frame update

    //public CustomGUILabel texXX;
    //public CustomGUITexture texMap;
    //public CustomGUITexture texicon;
    //public CustomGUITexture texBK;
    //public CustomGUILabel labDF;
    //public CustomGUILabel labSJ;
    public CustomGUIButton btnQuit;
    public CustomGUIButton btnSetting;
    public CustomGUILabel labScore;
    public CustomGUILabel labTime;
    public CustomGUITexture texHP;
    [HideInInspector]
    public int nowScore=0;
    [HideInInspector]
    public float nowTime=0;
    private int time;
    public float hpW = 350;

    void Start()
    {
        btnQuit.clickEvent += () =>
        {
            QuitPanel.Instance.ShowMe();
            //打开设置时候时间为0
            Time.timeScale = 0;
        };
        btnSetting.clickEvent += () =>
        {
            SettingPanel.Instance.ShowMe(); 
            //打开设置时候时间为0
            Time.timeScale = 0;

        };
 
    }

    private void BtnQuit_clickEvent()
    {
        throw new System.NotImplementedException();
    }

    // Update is called once per frame
    void Update()
    {
        nowTime += Time.deltaTime;

        int time = (int)nowTime;
        labTime.content.text = "";
        if (time / 3600 > 0)
        {
            labTime.content.text += time / 3600 + "时";
        }
        if (time % 3600 / 60 > 0|| labTime.content.text !="")
        {
            labTime.content.text += time % 3600 / 60 + "分";
        }

        labTime.content.text += time % 60 + "秒";
    }

    public void AddScore(int score)
    {
        nowScore += score;
        //更新界面显示
        labScore.content.text = nowScore.ToString(); 
    }
    public void UpdateHP(int maxHP,int HP)
    {
        texHP.guiPos.width = (float)HP / maxHP*hpW;
    }
}
