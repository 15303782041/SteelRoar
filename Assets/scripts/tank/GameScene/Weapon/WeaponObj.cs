using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

public class WeaponObj : MonoBehaviour
{

    //注意：字段名buttle是拼写错误，但改名会使预制体上已序列化的引用丢失，故保留
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
            //从对象池取子弹（不再Instantiate，循环复用消除GC）
            GameObject obj = PoolManager.Instance.GetObj(buttle);
            obj.transform.position = shootPos[i].position;
            obj.transform.rotation = shootPos[i].rotation;
            //控制子弹做什么
            BulletObj bulletObj = obj.GetComponent<BulletObj>();
            bulletObj.SetFather(fatherObj);
        }
    }

  
}
