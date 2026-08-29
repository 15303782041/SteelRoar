 using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

public class PlayerObj : TankBaseObj
{
    //当前装备的武器
    public WeaponObj nowWeapon;
    //武器父对象位置
    public Transform weaponPos;

    // Start is called before the first frame update

    void Update()
    {
        //1.ws健 控制 前进后退
        //知识点
        //1.transformer的位移
        //2.input轴向输入检测
        this.transform.Translate(Input.GetAxis("Vertical") * Vector3.forward * moveSpeed * Time.deltaTime);

        //2.ad健 控制 左右旋转
        //知识点
        //1.transformer的旋转
        //2.input轴向输入检测 
        this.transform.Rotate(Input.GetAxis("Horizontal") * Vector3.up * roundSpeed * Time.deltaTime);
        //3.鼠标左右移动 控制 炮台旋转
        //知识点
        //1.transform旋转
        //2.input 鼠标轴向输入检测
        tankHead.transform.Rotate(Input.GetAxis("Mouse X") * Vector3.up * headRoundSpeed * Time.deltaTime);

        //4.鼠标左键开火
        //input
        if (Input.GetMouseButtonDown(0))
        {
            this.Fire();
        }


    }
    public override void Fire()
    {
        if (nowWeapon != null)
        {
            nowWeapon.Fire();
        }
    }
    public override void Dead()
    {
        //这里不执行 父类的死亡 因为 玩家坦克 摄像机 是它的子对象 如果执行父类死亡
        //会把玩家坦克从场景上移除 那么就间接的移除了 摄像机
        //base.Dead();
        //广播"玩家死亡"事件，由UI层决定暂停和显示结算面板（战斗代码不再直接操作UI）
        EventCenter.Instance.EventTrigger(EEventType.PlayerDead, null);
    }
    public override void Wound(TankBaseObj other)
    {
        base.Wound(other);
        //广播"玩家受伤"事件（参数：当前血量、最大血量），UI监听后刷新血条
        EventCenter.Instance.EventTrigger(EEventType.PlayerHurt, new float[] { this.hp, this.maxHp });
    }

    public void ChangeWeapon(GameObject weapon)
    {
        //移除当前武器
        if (nowWeapon != null)
        {
            Destroy(nowWeapon.gameObject);
            nowWeapon = null;
        }
        //切换武器
        //创建出武器 设置他的父对象 保证缩放没什么问题
        GameObject weaponObj = Instantiate(weapon, weaponPos, false);
        //获取到新的武器组件
        nowWeapon = weaponObj.GetComponent<WeaponObj>();
        //设置武器拥有者
        nowWeapon.SetFather(this);
    }
}
