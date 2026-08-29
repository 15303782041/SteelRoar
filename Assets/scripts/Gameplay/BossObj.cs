using System.Collections;
using GameFramework;
using UnityEngine;

/// <summary>
/// Boss：三阶段阶段机（按血量比例驱动）
/// 阶段一(100%~60%)：扇形弹幕（默认5发/2秒）
/// 阶段二(60%~30%)：入场触发——召唤小怪 + 朝玩家冲撞（接触伤害）
/// 阶段三(30%以下)：狂暴——弹幕加密（9发/1秒）+ 移速提升50%
/// 所有弹幕走对象池；击杀得分经MonsterDead事件交给UI
/// </summary>
public class BossObj : TankBaseObj
{
    [Header("Boss通用")]
    public int score = 100;
    public Transform lookAtTarget;       //玩家（波次管理器生成时注入）
    public GameObject bulletObj;         //弹幕子弹预制体
    public float fireDis = 14;           //开火距离
    public float detectDis = 18;         //索敌距离

    [Header("弹幕参数（阶段一/阶段三）")]
    public int barrageCount1 = 5;
    public float fireOffsetTime1 = 2f;
    public int barrageCount3 = 9;
    public float fireOffsetTime3 = 1f;
    public float barrageSpread = 70f;    //扇形总张角（度）

    [Header("阶段二：冲撞与召唤")]
    public float chargeSpeed = 14f;
    public float chargeTime = 1.0f;
    public string summonName = "Monster1";
    public int summonCount = 2;

    private enum BossPhase { One, Two, Three }
    private BossPhase nowPhase = BossPhase.One;

    private float fireTimer = 0;
    private float speedMult = 1f;        //阶段三移速倍率
    private bool charging = false;
    private float chargeTimer = 0;
    private Vector3 chargeDir;
    private bool chargeHit = false;
    private bool summoned = false;

    private PlayerObj playerObj;

    /// <summary>波次管理器生成Boss时注入玩家</summary>
    public void Init(Transform player)
    {
        lookAtTarget = player;
        playerObj = player != null ? player.GetComponent<PlayerObj>() : null;
    }

    //从池中取出时重置——对象池三定律之二
    void OnEnable()
    {
        hp = maxHp;
        fireTimer = 0;
        speedMult = 1f;
        nowPhase = BossPhase.One;
        charging = false;
        summoned = false;
    }

    void Update()
    {
        //暂停冻结（同怪物AI）
        if (Time.timeScale == 0f || lookAtTarget == null)
            return;

        RefreshPhase();

        //冲撞进行中：直线突进+接触伤害，其余行为暂停
        if (charging)
        {
            transform.position += chargeDir * chargeSpeed * Time.deltaTime;
            chargeTimer += Time.deltaTime;

            float d = Vector3.Distance(transform.position, lookAtTarget.position);
            if (!chargeHit && d < 2.2f && playerObj != null)
            {
                playerObj.Wound(this);   //撞到玩家：结算一次碰撞伤害
                chargeHit = true;
            }

            if (chargeTimer >= chargeTime)
                charging = false;
            return;
        }

        //索敌：玩家太远原地待命
        float dist = Vector3.Distance(transform.position, lookAtTarget.position);
        if (dist > detectDis)
            return;

        //面向玩家；距离远则缓步逼近（阶段三狂暴移速）
        transform.LookAt(lookAtTarget);
        if (dist > fireDis * 0.6f)
            transform.Translate(Vector3.forward * moveSpeed * speedMult * Time.deltaTime);

        //弹幕冷却
        fireTimer += Time.deltaTime;
        float interval = nowPhase == BossPhase.Three ? fireOffsetTime3 : fireOffsetTime1;
        if (dist <= fireDis && fireTimer >= interval)
        {
            FireBarrage();
            fireTimer = 0;
        }
    }

    /// <summary>按血量比例推进阶段（只能前进不能回退）</summary>
    private void RefreshPhase()
    {
        float rate = maxHp > 0 ? (float)hp / maxHp : 0f;

        if (rate <= 0.3f && nowPhase < BossPhase.Three)
        {
            nowPhase = BossPhase.Three;
            speedMult = 1.5f;            //狂暴
        }
        else if (rate <= 0.6f && nowPhase == BossPhase.One)
        {
            nowPhase = BossPhase.Two;
            EnterPhaseTwo();
        }
    }

    /// <summary>阶段二入场：召唤小怪 + 发起一次冲撞</summary>
    private void EnterPhaseTwo()
    {
        //召唤小怪（走工厂+对象池），并计入波次存活数
        WaveManager wm = FindObjectOfType<WaveManager>();
        if (wm != null)
            wm.SpawnSummons(summonName, summonCount, transform.position);
        else
            MonsterFactory.Create(summonName, transform.position + Vector3.right * 2f);

        StartCharge();
    }

    private void StartCharge()
    {
        if (lookAtTarget == null)
            return;
        chargeDir = (lookAtTarget.position - transform.position).normalized;
        chargeTimer = 0;
        chargeHit = false;
        charging = true;
    }

    /// <summary>实现基类抽象方法：Boss的"开火"即一轮扇形弹幕</summary>
    public override void Fire()
    {
        FireBarrage();
    }

    /// <summary>扇形弹幕：以玩家方向为中心均匀展开，全部走对象池</summary>
    private void FireBarrage()
    {
        if (bulletObj == null)
            return;

        int count = nowPhase == BossPhase.Three ? barrageCount3 : barrageCount1;
        Vector3 center = (lookAtTarget.position - transform.position).normalized;

        for (int i = 0; i < count; i++)
        {
            //扇形均匀取角：-张角/2 ~ +张角/2
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float angle = Mathf.Lerp(-barrageSpread / 2f, barrageSpread / 2f, t);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * center;

            GameObject obj = PoolManager.Instance.GetObj(bulletObj);
            obj.transform.position = transform.position + Vector3.up * 1.2f + dir * 1.5f;
            obj.transform.rotation = Quaternion.LookRotation(dir);

            BulletObj bullet = obj.GetComponent<BulletObj>();
            bullet.SetFather(this);
        }
    }

    public override void Dead()
    {
        base.Dead();
        //广播击杀事件（Boss得分较高，UI同一通道处理）
        EventCenter.Instance.EventTrigger(EEventType.MonsterDead, score);
    }
}
