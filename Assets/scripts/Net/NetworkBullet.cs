using GameFramework;
using UnityEngine;

/// <summary>
/// 联机表现子弹：由对端的开火事件在本机生成（对象池复用）。
/// 命中本机玩家→本地结算扣血（V1击中者算账：伤害值由发射方随消息携带）；
/// 命中其他任何东西或超时→回池。
/// 与教程BulletObj分离的原因：BulletObj依赖fatherObj的Tag体系做阵营判定，
/// 联机弹的阵营语义（打的是"本机玩家"）用独立组件更干净
/// </summary>
public class NetworkBullet : MonoBehaviour
{
    public int dmg;                 // 伤害（发射方随FireEvent携带，V1不再套防御）
    public float speed = 30f;

    private float life = 3f;
    private float armTimer = 0f;    // 武装倒计时：归零前不结算对玩家的命中（防贴脸幻影伤害）

    //每次从池中取出（激活）时全量重置（对象池三定律：取出必重置）
    void OnEnable()
    {
        life = 3f;
        armTimer = 0.05f;           // 约1.5米飞行距离内不打玩家：覆盖出膛点插进对方碰撞体的情形
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        life -= Time.deltaTime;
        armTimer -= Time.deltaTime;
        if (life <= 0f)
            Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        //武装期内不打玩家：坦克贴脸对轰时，表现弹的出膛点可能直接插在对方碰撞体里，
        //"刚开枪就莫名其妙掉血"的幻影伤害就是这么来的（出膛点前移+武装期双保险）
        if (armTimer > 0f && other.CompareTag("Player"))
            return;

        //打到本机玩家：本地结算扣血→刷新血条→死亡走GameMgr结算（链路已通）
        if (other.CompareTag("Player"))
        {
            PlayerObj p = other.GetComponent<PlayerObj>();
            if (p != null)
                p.ApplyNetworkDamage(dmg);
        }
        Despawn();                   // 命中任何东西都消失（含地面/墙）
    }

    private void Despawn()
    {
        PoolManager.Instance.PushObj(gameObject);
    }
}
