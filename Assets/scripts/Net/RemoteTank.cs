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
        obj.transform.position = new Vector3(p.px, p.py, p.pz);
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
        targetPos = new Vector3(p.x, p.y, p.z);
        targetBodyRy = p.bodyRy;
        targetHeadRy = p.headRy;
    }
}
