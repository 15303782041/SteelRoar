using GameFramework;
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
        //先取死亡特效（要用自身位置，必须在回池前取）
        if(deadEff!=null)
        {
            //特效从池中取出（不再Instantiate）
            GameObject effObj = PoolManager.Instance.GetObj(deadEff);
            effObj.transform.position = this.transform.position;
            effObj.transform.rotation = this.transform.rotation;
            //特效自带音效，音量/开关/播放统一交给音乐管理器
            MusicManager.Instance.SetSourceVolume(effObj.GetComponent<AudioSource>(), true);
        }
        //自身回池复用（不再Destroy；玩家在子类重写Dead，不会走到这里）
        PoolManager.Instance.PushObj(this.gameObject);
    }
}
