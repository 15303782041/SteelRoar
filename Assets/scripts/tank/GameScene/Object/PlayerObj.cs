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

    //地面瞄准平面（y=0的数学平面，只做射线求交，不依赖场景碰撞体）
    private Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
    private Camera mainCam;

    // Start is called before the first frame update
    void Start()
    {
        mainCam = Camera.main;
    }

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
        //3.鼠标绝对瞄准：摄像机→鼠标射线与地面平面求交，炮台直接朝向落点
        //（原教程为MouseX相对旋转：帧率越高转得越慢、且需甩鼠标追人，改为射线绝对瞄准）
        Ray aimRay = mainCam.ScreenPointToRay(Input.mousePosition);
        if (groundPlane.Raycast(aimRay, out float enter))
        {
            Vector3 aimPoint = aimRay.GetPoint(enter);
            Vector3 aimDir = aimPoint - tankHead.position;
            aimDir.y = 0;
            if (aimDir.sqrMagnitude > 0.01f)
                tankHead.rotation = Quaternion.LookRotation(aimDir);
        }

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
