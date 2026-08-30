using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 可破坏箱子：只有玩家子弹能破坏（怪物子弹/坦克碰撞无效），需打掉maxHp点血。
/// 血量归零：概率掉落奖励 + 播放破坏特效 + 销毁。
/// 联机：打穿时广播WallBroken(稳定ID)，对端找到同ID箱子做同款销毁——
/// 只同步"打穿"事件不同步血量（V1边界：血量进度不对齐，打穿瞬间对齐）
/// </summary>
public class CubeObject : MonoBehaviour
{
    //奖励预设体关联
    public GameObject[] rewardObject;
    //死亡预设体关联
    public GameObject deadEff;
    [Header("可破坏设置")]
    public int maxHp = 3;               // 需要命中次数（玩家子弹每发-1）

    private int hp;
    private bool broken;                // 本箱子是否已销毁处理过（防本地/网络双触发）

    // ---- 联机稳定ID ----
    //双方加载同一份GameScene，箱子集合一致；按坐标排序分配ID → 两端ID一一对应。
    //不直接用名字/实例ID：场景里可能重名，实例ID更是每台机器各自不同
    private static readonly Dictionary<int, CubeObject> netIdMap = new Dictionary<int, CubeObject>();
    private static int builtSceneHandle = int.MinValue;   // 注册表构建时的场景句柄（切场景即重建）

    /// <summary>本箱子在网络同步中的稳定ID（EnsureRegistry按坐标排序分配）</summary>
    public int NetId { get; private set; } = -1;

    //场景加载/重新激活时重置血量（箱子不进对象池，是场景常驻物体）
    void OnEnable()
    {
        hp = maxHp;
    }

    void Start()
    {
        //Start时机注册（场景物体已全部就位）：首次访问时按坐标排序给全场景箱子编号
        EnsureRegistry();
    }

    /// <summary>确保当前场景的ID注册表已构建：换场景后句柄变化自动重建</summary>
    private static void EnsureRegistry()
    {
        int handle = SceneManager.GetActiveScene().handle;
        if (handle == builtSceneHandle)
            return;

        netIdMap.Clear();
        builtSceneHandle = handle;

        CubeObject[] cubes = FindObjectsByType<CubeObject>(FindObjectsSortMode.None);
        //按坐标排序保证确定性：双方场景相同 → 排序结果相同 → 同一面墙ID相同
        System.Array.Sort(cubes, (a, b) =>
        {
            Vector3 pa = a.transform.position;
            Vector3 pb = b.transform.position;
            int c = pa.x.CompareTo(pb.x);
            if (c != 0) return c;
            c = pa.z.CompareTo(pb.z);
            if (c != 0) return c;
            return pa.y.CompareTo(pb.y);
        });
        for (int i = 0; i < cubes.Length; i++)
        {
            cubes[i].NetId = i;
            netIdMap[i] = cubes[i];
        }
    }

    /// <summary>
    /// 对端广播"ID为id的墙被打穿"（NetCenter分发WallBroken时调用）。
    /// 静态入口：消息到达时本机可能还没碰到过这个箱子
    /// </summary>
    public static void BreakByNetwork(int id)
    {
        EnsureRegistry();
        if (netIdMap.TryGetValue(id, out CubeObject cube) && cube != null)
            cube.BreakDown(broadcast: false);   // 对端事件不再转发，否则死循环回声
    }

    private void OnTriggerEnter(Collider other)
    {
        if (broken)
            return;                          // 已被对端事件销毁处理过：忽略后续碰撞

        //只有玩家子弹能破坏箱子：怪物子弹打上去只会自己爆炸，箱子毫发无伤
        BulletObj bullet = other.GetComponent<BulletObj>();
        if (bullet == null || !(bullet.fatherObj is PlayerObj))
            return;

        hp--;
        if (hp > 0)
            return;                      // 还没打穿：只扣血不销毁

        BreakDown(broadcast: true);
    }

    /// <summary>打穿销毁：掉落奖励+破坏特效+移除；本地打穿额外广播给对端</summary>
    private void BreakDown(bool broadcast)
    {
        if (broken)
            return;
        broken = true;
        netIdMap.Remove(NetId);

        //血量归零：随机掉落奖励（联机时两端各自独立掉落，随机结果可能不同——
        //各端掉各端的，本机玩家捡本机掉落，V1可接受）
        int rangeInt = Random.Range(0, 100);
        if (rangeInt < 50)
        {
            rangeInt = Random.Range(0, rewardObject.Length);
            //放在箱子当前所在的位置即可
            Instantiate(rewardObject[rangeInt], this.transform.position, this.transform.rotation);
        }

        //创建破坏特效，音量/开关统一交给音乐管理器
        GameObject effObj = Instantiate(deadEff, this.transform.position, this.transform.rotation);
        MusicManager.Instance.SetSourceVolume(effObj.GetComponent<AudioSource>(), true);

        Destroy(this.gameObject);

        if (broadcast && NetCenter.Instance.Networking)
            NetCenter.Instance.Send((ushort)MsgId.WallBroken, new WallBrokenPayload { id = NetId });
    }
}
