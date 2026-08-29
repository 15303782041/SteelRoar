using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterTower : TankBaseObj
{
    //自动旋转已经有了不用处理了
    //间隔开火 有开火时间 
    public float fireOffsetTime = 1;
    public float nowTime=0;
    //发射位置 
    public Transform[] shootPos;
    //子弹预设体 关联
    public GameObject bulletObj;

    // Start is called before the first frame update
    void Start()
    {
        //不停的累加时间并记录下来
        nowTime += Time.deltaTime;
        //用于累加时间 用于开火判断  超过时就开火
        if(nowTime > fireOffsetTime)
        {
            Fire();
        }
    }

    // Update is called once per frame
    void Update()
    {
        //不停累加时间并记录下来
        nowTime += Time.deltaTime;
        //当时间超过间隔时间 就开火
        if(nowTime >= fireOffsetTime)
        {
            Fire();
            nowTime = 0;
        } 
    }

    public override void Fire()
    {
        for (int i = 0; i < shootPos.Length; i++)
        {
            //实列话子弹
            GameObject obj = Instantiate(bulletObj,shootPos[i].position,shootPos[i].rotation);
            //设置子弹的拥有者方便以后进行属性计算
            BulletObj bullet = obj.GetComponent<BulletObj>();
            bullet.SetFather(this);
        }

    }

    public override void Wound(TankBaseObj other)
    {
        //这里面什么都不写
        //目的是固定的坦克不会受到伤害，就不会死亡了
    }
}
