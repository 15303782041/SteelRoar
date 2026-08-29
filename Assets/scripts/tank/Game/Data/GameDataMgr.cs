using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RankInfo;

public class GameDataMgr
{
    //这个是游戏数据管理类 是一个单例模式对象 
    private static GameDataMgr instance = new GameDataMgr();
    public static GameDataMgr Instance { get => instance; }
    //音效数据对象
    public MusicData musicData;
    //排行榜数据对象
    public RankList rankData;
    private GameDataMgr()
    {
        musicData = PlayerPrefsDataMgr.Instance.LoadData(typeof(MusicData), "Music") as MusicData;


        if (!musicData.notFirst)
        {
            musicData.notFirst = true;
            musicData.isOpenBK = true;
            musicData.isOpenSound = true;
            musicData.bkValue = 1;
            musicData.soundValue = 1;
            PlayerPrefsDataMgr.Instance.SaveData(musicData, "Music");
        }

        //初始化读取排行榜数据
        rankData = PlayerPrefsDataMgr.Instance.LoadData(typeof(RankList),"Rank") as RankList;
    }


    public void AddRankInfo(string name, int score, float time)
    {
        rankData.list.Add(new RankInfo(name, score, time));
        //排序
        rankData.list.Sort((a, b) => a.time < b.time ? -1 : 1);
        //排序过后超过10条以外的数据移除
        for (int i = rankData.list.Count - 1; i >=10 ; i++)
        {
            rankData.list.RemoveAt(i);
        }
        PlayerPrefsDataMgr.Instance.SaveData(rankData, "Rank");
    }
    //开启或者关闭音乐
    public void OpenOrCloseBKMusic(bool isOpen) 
    {
        musicData.isOpenBK = isOpen;
        //在这里控制场景上的背景音乐开关
        BKMusic.Instance.ChangeOpen(isOpen);

        //存储改变后的数据
        PlayerPrefsDataMgr.Instance.SaveData(musicData, "Music");
    }
    //开启或者关闭音效
    public void OpenOrCloseSound(bool isOpen) 
    {
        musicData.isOpenSound = isOpen;
        PlayerPrefsDataMgr.Instance.SaveData(musicData, "Music");
    }
    //改变音乐大小
    public void ChangeBKValue(float value)
    {
        musicData.bkValue = value;

        BKMusic.instance.ChangeValue(value);

        PlayerPrefsDataMgr.Instance.SaveData(musicData, "Music");
    }
    //改变音效大小
    public void ChangeSoundValue(float value)
    {
        musicData.soundValue = value;
        PlayerPrefsDataMgr.Instance.SaveData(musicData, "Music");
    }

}

