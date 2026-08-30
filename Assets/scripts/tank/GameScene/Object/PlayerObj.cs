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

    [Header("肉鸽Buff")]
    private readonly List<BuffInfo> nowBuffs = new List<BuffInfo>();  // 本局已获得的Buff
    private int shieldLayers = 0;                                     // 护盾层数（每层挡一次伤害）
    private float lifesteal = 0f;                                     // 每次命中回复的生命

    [Header("联机同步")]
    private float netSyncTimer = 0;
    private const float NetSyncInterval = 1f / 15f;                   // 15Hz状态同步

    /// <summary>吸血数值（子弹命中敌方坦克时由BulletObj读取）</summary>
    public float LifestealValue => lifesteal;

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

        //5.联机：15Hz广播自身位姿（车体位置+车体与炮塔朝向）
        if (NetCenter.Instance.Networking)
        {
            netSyncTimer += Time.deltaTime;
            if (netSyncTimer >= NetSyncInterval)
            {
                netSyncTimer = 0f;
                TransformPayload p = new TransformPayload
                {
                    x = transform.position.x,
                    y = transform.position.y,
                    z = transform.position.z,
                    bodyRy = transform.eulerAngles.y,
                    headRy = tankHead != null ? tankHead.eulerAngles.y : 0f,
                };
                NetCenter.Instance.Send((ushort)MsgId.TransformSync, p);
            }
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
        //护盾优先：有护盾层时抵挡本次伤害（不掉血，掉一层盾）
        if (shieldLayers > 0)
        {
            shieldLayers--;
            EventCenter.Instance.EventTrigger(EEventType.PlayerHurt, new float[] { hp, maxHp });
            return;
        }

        base.Wound(other);
        //广播"玩家受伤"事件（参数：当前血量、最大血量），UI监听后刷新血条
        EventCenter.Instance.EventTrigger(EEventType.PlayerHurt, new float[] { hp, maxHp });
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

    #region 肉鸽Buff（三选一面板调用）

    /// <summary>某种Buff当前已叠的层数（面板据此过滤已达上限的选项）</summary>
    public int GetStack(BuffType type)
    {
        int n = 0;
        foreach (BuffInfo b in nowBuffs)
            if (b.type == type)
                n++;
        return n;
    }

    /// <summary>某种Buff的数值总和（弹种效果按层数×资产value叠加，如冰冻3层=0.6减速）</summary>
    public float GetBuffValue(BuffType type)
    {
        float v = 0;
        foreach (BuffInfo b in nowBuffs)
            if (b.type == type)
                v += b.value;
        return v;
    }

    /// <summary>应用一次Buff：立即修改对应属性（数值含义随BuffType变化）</summary>
    public void AddBuff(BuffInfo info)
    {
        nowBuffs.Add(info);
        switch (info.type)
        {
            case BuffType.Attack:
                atk += Mathf.RoundToInt(info.value);
                break;
            case BuffType.MoveSpeed:
                moveSpeed *= 1f + info.value;
                break;
            case BuffType.MaxHp:
                maxHp += Mathf.RoundToInt(info.value);
                hp += Mathf.RoundToInt(info.value);          // 上限提升的同时回复等量
                EventCenter.Instance.EventTrigger(EEventType.PlayerHurt, new float[] { hp, maxHp });
                break;
            case BuffType.Lifesteal:
                lifesteal += info.value;
                break;
            case BuffType.Shield:
                shieldLayers += Mathf.RoundToInt(info.value);
                break;
        }
        Debug.Log($"获得Buff：{info.buffName}（{GetStack(info.type)}/{info.stackMax}层）");
    }

    /// <summary>回复生命（吸血Buff用），不超过上限并刷新血条</summary>
    public void Heal(float amount)
    {
        hp = Mathf.Min(maxHp, hp + Mathf.RoundToInt(amount));
        EventCenter.Instance.EventTrigger(EEventType.PlayerHurt, new float[] { hp, maxHp });
    }

    #endregion
}
