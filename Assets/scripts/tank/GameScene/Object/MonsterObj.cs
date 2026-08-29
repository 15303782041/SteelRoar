using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityEngine.UI;

public class MonsterObj : TankBaseObj
{
    //配置键：在Inspector里填MonsterConfig.json中的monsterName，运行时自动应用对应数值
    public string monsterName;
    //击杀得分（会被Json配置覆盖）
    public int score = 10;

    //1.要让坦克 在两点之间 来回移动
    private Transform targetPos;
    public Transform[] randomPos;

    //2.坦克要一直盯着自己的目标（波次管理器生成时注入玩家）
    public Transform lookAtTarget;

    //3.开火距离与冷却
    public float fireDis = 5;
    public float fireOffsetTime = 1;
    private float nowTime = 0;

    //开火点
    public Transform[] shootPos;

    //子弹预设体
    public GameObject bulletObj;

    [Header("AI状态机参数")]
    public float detectDis = 12;        //侦测半径：玩家进入则追击
    public float retreatHpRate = 0.3f;  //血量低于该比例→撤退保命

    //FSM：每个怪物持有一套状态实例，Update里只驱动当前状态
    public PatrolState patrolState = new PatrolState();
    public ChaseState chaseState = new ChaseState();
    public AttackState attackState = new AttackState();
    public RetreatState retreatState = new RetreatState();
    private IState nowState;

    [Header("头顶血条")]
    private Transform hpBarRoot;        //血条根节点（运行时创建的世界空间Canvas）
    private Image hpFill;               //填充图（fillAmount=血量比例）
    private float showTime = 0;         //受伤后血条显示时长

    //每次从池中取出（激活）时重置状态——对象池三定律之二：取出必重置
    void OnEnable()
    {
        hp = maxHp;
        nowTime = 0;
        showTime = 0;
        ChangeState(patrolState);
    }

    // Start is called before the first frame update
    void Start()
    {
        InitFromConfig();
        RandomPos();
        CreateHpBar();
    }

    // Update is called once per frame
    void Update()
    {
        //游戏暂停（timeScale=0）时AI冻结：
        //否则Time.deltaTime=0会让开火冷却永远满足→暂停状态下每帧无限开火
        if (Time.timeScale == 0f)
            return;

        //状态机驱动：所有行为和转移判定都在状态类里
        nowState?.Update();

        //血条显示计时
        if (showTime > 0)
            showTime -= Time.deltaTime;
    }

    void LateUpdate()
    {
        UpdateHpBar();
    }

    #region 状态机核心与公共行为（由状态类调用）

    /// <summary>切换状态：旧状态Exit→新状态Enter</summary>
    public void ChangeState(IState nextState)
    {
        if (nowState == nextState)
            return;
        nowState?.Exit();
        nowState = nextState;
        nowState.Enter(this);
    }

    /// <summary>巡逻移动：朝随机巡逻点走，到达换下一个点</summary>
    public void PatrolMove()
    {
        //巡逻点为空或已销毁（如外部物体被删）时重新随机
        if (targetPos == null)
        {
            RandomPos();
            if (targetPos == null)
                return;
        }
        this.transform.LookAt(targetPos);
        this.transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        if (Vector3.Distance(this.transform.position, targetPos.position) < 0.05f)
            RandomPos();
    }

    /// <summary>追击移动：面朝玩家直线逼近</summary>
    public void ChaseMove()
    {
        if (lookAtTarget == null)
            return;
        this.transform.LookAt(lookAtTarget);
        this.transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    /// <summary>攻击：炮台瞄准玩家，冷却到了就开火</summary>
    public void AimAndTryFire()
    {
        if (lookAtTarget == null || tankHead == null)
            return;
        tankHead.LookAt(lookAtTarget);
        nowTime += Time.deltaTime;
        if (nowTime >= fireOffsetTime)
        {
            Fire();
            nowTime = 0;
        }
    }

    /// <summary>撤退移动：背向玩家拉开距离</summary>
    public void MoveAwayFromPlayer()
    {
        if (lookAtTarget == null)
            return;
        Vector3 awayDir = (this.transform.position - lookAtTarget.position).normalized;
        this.transform.LookAt(this.transform.position + awayDir);
        this.transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    /// <summary>玩家是否在侦测半径内</summary>
    public bool PlayerInDetectRange()
    {
        return lookAtTarget != null &&
            Vector3.Distance(this.transform.position, lookAtTarget.position) <= detectDis;
    }

    /// <summary>玩家是否在开火距离内</summary>
    public bool PlayerInFireRange()
    {
        return lookAtTarget != null &&
            Vector3.Distance(this.transform.position, lookAtTarget.position) <= fireDis;
    }

    /// <summary>是否残血需要撤退</summary>
    public bool NeedRetreat()
    {
        return maxHp > 0 && (float)hp / maxHp <= retreatHpRate;
    }

    #endregion

    private void RandomPos()
    {
        if (randomPos.Length == 0)
            return;

        //随机挑一个"还活着"的巡逻点（被销毁的点跳过），最多尝试5次
        for (int i = 0; i < 5; i++)
        {
            Transform p = randomPos[Random.Range(0, randomPos.Length)];
            if (p != null)
            {
                targetPos = p;
                return;
            }
        }
        targetPos = null;
    }

    public override void Fire()
    {
        for (int i = 0; i < shootPos.Length; i++)
        {
            //从对象池取子弹（不再Instantiate，循环复用消除GC）
            GameObject obj = PoolManager.Instance.GetObj(bulletObj);
            obj.transform.position = shootPos[i].position;
            obj.transform.rotation = shootPos[i].rotation;
            //设计子弹的拥有者 方便之后进行伤害计算
            BulletObj bullet = obj.GetComponent<BulletObj>();
            bullet.SetFather(this);
        }

    }

    /// <summary>把配置数值灌入坦克属性（工厂生成和场景怪物共用这一个入口）</summary>
    public void Init(MonsterInfo info)
    {
        this.atk = info.atk;
        this.def = info.def;
        this.maxHp = info.maxHp;
        this.hp = info.maxHp;
        this.moveSpeed = info.moveSpeed;
        this.fireDis = info.fireDis;
        this.fireOffsetTime = info.fireOffsetTime;
        this.score = info.score;
    }

    /// <summary>场景中手动摆放的怪物：Start时按monsterName查Json应用数值；查不到保留Inspector数值并告警</summary>
    private void InitFromConfig()
    {
        if (string.IsNullOrEmpty(monsterName))
            return;

        MonsterInfo info = MonsterFactory.GetInfo(monsterName);
        if (info != null)
        {
            Init(info);
            //配置生效的可见凭据：Console出现这行=Json已应用；没出现=没生效
            Debug.Log($"[{gameObject.name}] 应用Json配置 {info.monsterName}：maxHp={info.maxHp} atk={info.atk} def={info.def} 移速={info.moveSpeed} 开火间隔={info.fireOffsetTime} 得分={info.score}");
        }
        else
            Debug.LogWarning($"怪物[{gameObject.name}]未在MonsterConfig.json中找到配置：{monsterName}，沿用Inspector数值");
    }

    public override void Dead()
    {
        base.Dead();
        //广播"怪物死亡"事件，UI监听后自行加分（战斗代码不再直接操作UI）
        EventCenter.Instance.EventTrigger(EEventType.MonsterDead, score);
    }

    public override void Wound(TankBaseObj other)
    {
        base.Wound(other);
        //受伤时显示血条3秒
        showTime = 3;
    }

    #region 头顶UGUI血条（运行时创建，替代已删除的IMGUI老血条）

    /// <summary>运行时创建头顶世界空间血条：黑底+红色填充条，受伤才显示</summary>
    private void CreateHpBar()
    {
        if (hpBarRoot != null)
            return;

        //标准姿势：UI组件在构造时原子化创建。
        //先new空物体再AddComponent<Image>的话，Image要求的RectTransform会替换掉
        //旧Transform，手里先拿住的Transform引用就变成"已销毁"→MissingReferenceException
        GameObject barGo = new GameObject("HpBar", typeof(Canvas), typeof(Image));
        hpBarRoot = barGo.transform;
        hpBarRoot.SetParent(this.transform);
        hpBarRoot.localPosition = new Vector3(0, 2.6f, 0);
        hpBarRoot.localScale = Vector3.one * 0.01f;

        Canvas canvas = barGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        //背景条（半透明黑）
        Image bg = barGo.GetComponent<Image>();
        bg.rectTransform.sizeDelta = new Vector2(200, 24);
        bg.color = new Color(0, 0, 0, 0.6f);

        //填充条（红色，轴心设在左边缘：血量减少时从右侧缩短）
        //注意：不用Image的Filled/fillAmount——没有Sprite的Image填充模式不生效
        GameObject fillObj = new GameObject("Fill", typeof(Image));
        fillObj.transform.SetParent(hpBarRoot, false);
        hpFill = fillObj.GetComponent<Image>();
        hpFill.rectTransform.sizeDelta = new Vector2(190, 18);
        hpFill.rectTransform.pivot = new Vector2(0, 0.5f);
        hpFill.rectTransform.anchoredPosition = new Vector2(-95, 0);
        hpFill.color = Color.red;

        barGo.SetActive(false);
    }

    /// <summary>受伤显示3秒；显示期间面向摄像机并刷新血量比例</summary>
    private void UpdateHpBar()
    {
        if (hpBarRoot == null)
            return;

        hpBarRoot.gameObject.SetActive(showTime > 0);
        if (showTime > 0)
        {
            //血量比例=填充条宽度（190为满血宽度）
            float ratio = maxHp > 0 ? (float)hp / maxHp : 0f;
            hpFill.rectTransform.sizeDelta = new Vector2(190f * ratio, 18);
            //血条始终面向摄像机（世界空间UI的标配），Camera.main判空防切换场景瞬间报错
            Camera cam = Camera.main;
            if (cam != null)
                hpBarRoot.forward = cam.transform.forward;
        }
    }

    #endregion
}
