using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TankBaseObj : MonoBehaviour
{
    public int atk;
    public int def;
    public int maxHp;
    public int hp;

    //所有坦克都有炮台相关
    public Transform tankHead;

    public float moveSpeed=10;
    public float roundSpeed=100;
    public float headRoundSpeed=100;

    public GameObject deadEff;

    public abstract void Fire();
    public virtual void Wound(TankBaseObj other)
    {
        int dmg = other.atk - this.def; //this可省略，这个是说明了哪个this
        if (dmg <= 0)
            return;
        //如果伤害大于0，就应该减血
        this.hp-= dmg;
        //判断 如果血量<=0 就应该死亡
        if(this.hp<= 0)
        {
            this.hp = 0; 
            this.Dead();
        }

       
    }
    public virtual void Dead()
    {
        //对象死亡就是移除对象
        Destroy(this.gameObject);
        if(deadEff!=null)
        {
            GameObject effObj = Instantiate(deadEff, this.transform.position, this.transform.rotation);
            //由于该特效对象身上 直接关联了音效 所有我们可以在此处 把音效播放也控制了
            AudioSource audioSource = effObj.GetComponent<AudioSource>();
            //根据音乐数据 设置 音效 大小 和是否 播放
            //设置音效大小
            audioSource.volume = GameDataMgr.Instance.musicData.soundValue;
            //音效是否播放
            audioSource.mute = !GameDataMgr.Instance.musicData.isOpenSound;
            audioSource.Play();
        }
    }
}
