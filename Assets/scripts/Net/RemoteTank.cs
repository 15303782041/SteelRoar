using GameFramework;
using UnityEngine;

/// <summary>
/// 远端坦克影子：由网络消息驱动，无任何本地输入逻辑。
/// 收到TransformSync→记录目标位姿；Update里向目标插值——
/// 15Hz的消息间隔靠插值补平滑（网络同步的标准手法：状态低频同步+本地插值）
/// </summary>
public class RemoteTank : MonoBehaviour
{
    public Transform tankHead;              // 炮台（预制体自带关联）
    public GameObject bulletObj;            // 网络表现子弹预制体（生成时由NetCenter从原坦克抓取）
    public GameObject deadEff;              // 死亡爆炸特效（生成时由NetCenter从怪物预制体抓取）

    /// <summary>对方坦克是否已阵亡（阵亡后忽略一切后续网络消息）</summary>
    public bool IsDead { get; private set; }

    private bool firstSyncApplied = false;  // 是否收到过首个位姿包（影子此时才现身并吸附到位）
    private Vector3 targetPos;              // 目标位置
    private float targetBodyRy;             // 目标车体朝向
    private float targetHeadRy;             // 目标炮塔朝向

    private const float PosLerpSpeed = 10f;
    private const float RotLerpSpeed = 12f;

    /// <summary>
    /// 对端屏幕上我的开火表现：在本机生成"网络子弹"（打到本机玩家时由其本地结算扣血）。
    /// 子弹走对象池；飞行与命中判定由NetworkBullet组件负责
    /// </summary>
    public void SpawnNetworkBullet(FirePayload p)
    {
        if (bulletObj == null)
            return;

        GameObject obj = PoolManager.Instance.GetObj(bulletObj);

        //剥离教程弹逻辑（它依赖未设置的fatherObj，会NRE），换上网络弹组件
        BulletObj legacy = obj.GetComponent<BulletObj>();
        if (legacy != null)
            Destroy(legacy);
        NetworkBullet nb = obj.GetComponent<NetworkBullet>();
        if (nb == null)
            nb = obj.AddComponent<NetworkBullet>();

        nb.dmg = p.dmg;
        nb.speed = 30f;

        //出膛点沿炮口方向前移1米：出生点若贴着/插着本机玩家碰撞体，会造成"刚开枪我就掉血"的幻影伤害
        Vector3 dir = Quaternion.Euler(0f, p.ry, 0f) * Vector3.forward;
        obj.transform.position = new Vector3(p.px, p.py, p.pz) + dir * 1f;
        obj.transform.rotation = Quaternion.Euler(0f, p.ry, 0f);

        //忽略与影子自身碰撞体的接触：子弹出生点贴近炮口，否则出膛即撞影子直接消失
        var bulletCol = obj.GetComponent<Collider>();
        var shadowCol = GetComponent<Collider>();
        if (bulletCol != null && shadowCol != null)
            Physics.IgnoreCollision(bulletCol, shadowCol);
    }

    void Update()
    {
        //位置插值
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * PosLerpSpeed);

        //车体朝向插值（Yaw用LerpAngle避免角度绕圈跳变）
        float bodyY = Mathf.LerpAngle(transform.eulerAngles.y, targetBodyRy, Time.deltaTime * PosLerpSpeed);
        transform.rotation = Quaternion.Euler(0f, bodyY, 0f);

        //炮塔朝向插值
        if (tankHead != null)
        {
            float headY = Mathf.LerpAngle(tankHead.eulerAngles.y, targetHeadRy, Time.deltaTime * RotLerpSpeed);
            tankHead.rotation = Quaternion.Euler(0f, headY, 0f);
        }
    }

    /// <summary>应用网络下发的目标位姿（由NetCenter分发TransformSync时调用）</summary>
    public void ApplyTransform(TransformPayload p)
    {
        if (IsDead)
            return;
        targetPos = new Vector3(p.x, p.y, p.z);
        targetBodyRy = p.bodyRy;
        targetHeadRy = p.headRy;

        if (!firstSyncApplied)
        {
            //首包直接吸附到对方真实位置并现身：影子出生时只能站在本机玩家旁边（还不知道对方在哪），
            //若从那里一路插值过去，会看到对方坦克"嗖"地横穿地图滑过来——首包吸附是网络同步的常规处理
            firstSyncApplied = true;
            transform.position = targetPos;
            transform.rotation = Quaternion.Euler(0f, targetBodyRy, 0f);
            if (tankHead != null)
                tankHead.rotation = Quaternion.Euler(0f, targetHeadRy, 0f);
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 影子死亡：原地爆炸→失活。失活后Collider一并关闭——
    /// 后续到达的网络子弹物理上打不中它（"死亡后禁止再被命中"）
    /// </summary>
    public void Die()
    {
        if (IsDead)
            return;
        IsDead = true;

        if (deadEff != null)
        {
            //特效从池中取出，音量/开关统一交给音乐管理器（与坦克死亡表现一致）
            GameObject eff = PoolManager.Instance.GetObj(deadEff);
            eff.transform.position = transform.position;
            eff.transform.rotation = transform.rotation;
            MusicManager.Instance.SetSourceVolume(eff.GetComponent<AudioSource>(), true);
        }
        gameObject.SetActive(false);
    }
}
