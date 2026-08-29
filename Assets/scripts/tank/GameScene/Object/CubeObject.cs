 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeObject : MonoBehaviour
{
    //奖励预设体关联
    public GameObject[] rewardObject;
    //死亡预设体关联
    public GameObject deadEff;
    private void OnTriggerEnter(Collider other)
    {
        //1.打到自己的子弹 应该销毁
        //把箱子改成Cube就会相应销毁处理。

        //2.打到自己 应该处理 随机创建奖励的逻辑

        //随机一个数来获取奖励
        int rangeInt = Random.Range(0, 100);
        if (rangeInt < 50)
        {
            //随机创建一个  奖励预设体 在当前位置
            rangeInt = Random.Range(0, rewardObject.Length);
            //放在箱子当前所在的位置即可
            Instantiate(rewardObject[rangeInt],this.transform.position,this.transform.rotation);
        }

        //创建特效预设体
        GameObject effObj = Instantiate(deadEff, this.transform.position, this.transform.rotation);
        //控制音效
        AudioSource audioS = effObj.GetComponent<AudioSource>();
        audioS.volume = GameDataMgr.Instance.musicData.soundValue;
        audioS.mute = !GameDataMgr.Instance.musicData.isOpenSound; 


        Destroy(this.gameObject);
    }
}
