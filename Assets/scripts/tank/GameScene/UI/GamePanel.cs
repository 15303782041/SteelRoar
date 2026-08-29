using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
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

    //事件委托必须用成员变量存住引用——直接传lambda的话OnDisable时无法解绑（匿名函数每次引用不同）
    private Action<object> onPlayerHurt;
    private Action<object> onMonsterDead;
    private Action<object> onPlayerDead;
    private Action<object> onGameWin;

    void OnEnable()
    {
        //组装委托（订阅与退订必须用同一个引用）
        onPlayerHurt = (info) =>
        {
            //参数约定：float[]{当前血量, 最大血量}
            float[] hpInfo = info as float[];
            UpdateHP((int)hpInfo[1], (int)hpInfo[0]);
        };
        onMonsterDead = (info) => AddScore((int)info);
        onPlayerDead = (info) =>
        {
            //暂停与结算面板属于UI层职责（Day 10统一收编GameMgr）
            Time.timeScale = 0;
            LossPanel.Instance.ShowMe();
        };
        onGameWin = (info) =>
        {
            Time.timeScale = 0;
            WinPanel.Instance.ShowMe();
        };

        EventCenter.Instance.AddEventListener(EEventType.PlayerHurt, onPlayerHurt);
        EventCenter.Instance.AddEventListener(EEventType.MonsterDead, onMonsterDead);
        EventCenter.Instance.AddEventListener(EEventType.PlayerDead, onPlayerDead);
        EventCenter.Instance.AddEventListener(EEventType.GameWin, onGameWin);
    }

    void OnDisable()
    {
        //解绑必须在禁用时执行，否则对象销毁后残留监听→空引用
        EventCenter.Instance.RemoveEventListener(EEventType.PlayerHurt, onPlayerHurt);
        EventCenter.Instance.RemoveEventListener(EEventType.MonsterDead, onMonsterDead);
        EventCenter.Instance.RemoveEventListener(EEventType.PlayerDead, onPlayerDead);
        EventCenter.Instance.RemoveEventListener(EEventType.GameWin, onGameWin);
    }

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
