using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

public class MonsterObj : TankBaseObj
{
    //配置键：在Inspector里填MonsterConfig.json中的monsterName，运行时自动应用对应数值
    public string monsterName;
    //击杀得分（会被Json配置覆盖）
    public int score = 10;

    //1.要让坦克 在两点之间 来回移动
    private Transform targetPos;
    public Transform[] randomPos;
    
    //2.坦克要一直盯着自己的目标
    public Transform lookAtTarget;
    //3.当目标到达一定范围后 间隔一段时间 攻击一下目标
    //开火距离小于这个距离时就会主动攻击

    public float fireDis = 5;
    //为了避免太难，加一个攻击间隔时间
    public float fireOffsetTime = 1;
    private float nowTime = 0;

    //开火点
    public Transform[] shootPos;

    //子弹预设体
    public GameObject bulletObj;

    public Texture maxHpBK;
    public Texture hpBk; 

    //之所以没有new 是因为是结构体 可以不用new 直接在下面赋值
    private Rect maxHpRect;
    private Rect hpRect ;

    private float showTime;



    // Start is called before the first frame update
    void Start()
    {
        InitFromConfig();
        RandomPos();
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
            Init(info);
        else
            Debug.LogWarning($"怪物[{gameObject.name}]未在MonsterConfig.json中找到配置：{monsterName}，沿用Inspector数值");
    }

    // Update is called once per frame
    void Update()
    {
        //看向自己的目标点
        this.transform.LookAt(targetPos);

        //不停的向自己的面朝向位移
        this.transform.Translate(Vector3.forward*moveSpeed*Time.deltaTime);

        //知识点 Vector3里面有一个得到两个点之间距离的方法；
        //当距离过小时 认为到达了目的地 重新随机一个点
        if(Vector3.Distance(this.transform.position, targetPos.position) < 0.05f)
        {
            RandomPos();
        }

        //看向自己的目标
        if(lookAtTarget!= null)
        {
            tankHead.LookAt(lookAtTarget);
            //当自己和目标对象的距离 小于等于  配置的 开火距离时
            if(Vector3.Distance(this.transform.position, lookAtTarget.position) < fireDis)
            {
                nowTime += Time.deltaTime;
                if(nowTime > fireOffsetTime)
                {
                    Fire();
                    nowTime = 0;
                }
            }
        }

    }
    private void RandomPos()
    {
        if (randomPos.Length==0) 
            return;
        targetPos = randomPos[Random.Range(0,randomPos.Length)];
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
    public override void Dead()
    {
        base.Dead();
        //移动怪物死亡时加分（分值来自Json配置）
        GamePanel.Instance.AddScore(score);
    }
    private void OnGUI()
    {
        if(showTime > 0)
        {
            showTime -=Time.deltaTime;

            //画图 画血条
            //1.把怪物当前位置 转换成屏幕位置
            //摄像机里面提供了API 可以将 世界坐标 转为 屏幕坐标
            Vector3 screenPos = Camera.main.WorldToScreenPoint(this.transform.position);
            //2.屏幕位置转换成 GUI位置
            screenPos.y = Screen.height - screenPos.y;
            //然后再绘制
            //知识点：Gui中的 图片绘制
            maxHpRect.x = screenPos.x - 50;
            maxHpRect.y = screenPos.y - 100;
            maxHpRect.width = 100;
            maxHpRect.height = 15;
            //画地图
            GUI.DrawTexture(maxHpRect, maxHpBK);

            hpRect.x = screenPos.x - 50;
            hpRect.y = screenPos.y - 100;
            //根据血量和最大血量的百分比 决定画多宽
            hpRect.width = (float)hp / maxHp * 100f;
            hpRect.height = 15;
            //画血条
            GUI.DrawTexture(hpRect, hpBk); 
        }
    }
    public override void Wound(TankBaseObj other)
    {
        base.Wound(other);
        //设置显示血条的时间
        showTime = 3;
    }
}
