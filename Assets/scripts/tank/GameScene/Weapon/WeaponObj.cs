using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponObj : MonoBehaviour
{

    public GameObject buttle;

    //外部决定几个发射位置
    public Transform[] shootPos;

    //武器的拥有者
    public TankBaseObj fatherObj;

    public void SetFather(TankBaseObj obj)
    {
        fatherObj = obj;
    }

    public void Fire()
    {
        //根据位置创建出对应的子弹
        for (int i = 0;i < shootPos.Length; i++)
        {
            //创建子弹预设体
            GameObject obj = Instantiate(buttle, shootPos[i].position, shootPos[i].rotation);
            //控制子弹做什么
            BulletObj bulletObj = obj.GetComponent<BulletObj>();
            bulletObj.SetFather(fatherObj);
        }
    }

  
}
