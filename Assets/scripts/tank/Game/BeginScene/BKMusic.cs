using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BKMusic : MonoBehaviour
{
    public static BKMusic instance;
    public static BKMusic Instance => instance;

    private AudioSource audioSource;

    void Awake()
    {
        instance = this;
        //得到了自己依附的游戏对象上挂载的音频源脚本
        audioSource = this.GetComponent<AudioSource>();
        //初始化时 把大小和开关根据数据 进行设置
        ChangeValue(GameDataMgr.Instance.musicData.bkValue);
        ChangeOpen(GameDataMgr.Instance.musicData.isOpenBK);
    }

    public void ChangeValue(float value)
    {
        audioSource.volume = value;
    }

    public void ChangeOpen(bool isOpen)
    {
        //如果开启就是不静音
        //没有开启就是静音
        audioSource.mute =!isOpen;
    }


    
}
