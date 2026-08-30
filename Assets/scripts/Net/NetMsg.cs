using System;

/// <summary>联机消息ID统一登记（新增消息就在这里加，同事件中心思想）</summary>
public enum MsgId : ushort
{
    ReqJoin = 100,        // 客→主：请求加入
    AckJoin = 101,        // 主→客：加入应答
    TransformSync = 200,  // 双向15Hz：坦克位置与朝向
    FireEvent = 201,      // 即时双向：开火事件（本地预表现+对端生成表现）
    HpSync = 202,         // 主→客：血量同步（主机权威结算）
    Damage = 203,         // 击中者→被击者：你被命中，扣value点血（V1击中者算账模式）
    WallBroken = 204,     // 即时双向：墙被打穿（参数=墙的稳定ID，对端同ID墙同款销毁）
    GameOver = 300,       // 双向：我阵亡→对方获胜（败者发出）
    RematchReady = 301,   // 双向：结算面板点了"准备"——双方都发过即一起进新一局（准备确认制）
    PeerLeave = 302,      // 双向：我回主菜单了，本局解散——对方收后断链并弹提示
    Disconnect = 400,     // 双向：断线通知（接收线程关闭时入队）
    Heartbeat = 401,      // 双向1Hz保活：只用于刷新"对方还活着"的时间戳，5秒收不到任何消息判超时
}

/// <summary>网络消息：ID + JSON正文（正文结构见下方各Payload类）</summary>
public class NetMsg
{
    public ushort msgId;
    public string json;
}

// ---- 各消息的载荷结构（JsonUtility序列化，字段public即可）----

[Serializable]
public class JoinPayload
{
    public string name;
}

[Serializable]
public class AckJoinPayload
{
    public bool ok;
    public string playerName;
}

[Serializable]
public class TransformPayload
{
    public float x, y, z;      // 车体位置
    public float bodyRy;       // 车体朝向（Y轴欧拉角）
    public float headRy;       // 炮塔朝向（Y轴欧拉角）
}

[Serializable]
public class FirePayload
{
    public float px, py, pz;   // 发射点
    public float ry;           // 发射方向（Y轴欧拉角）
    public int dmg;            // 这发子弹的伤害（被击中方收到Damage时不再计算）
}

[Serializable]
public class DamagePayload
{
    public int dmg;            // 你被命中，扣这么多血
}

[Serializable]
public class HpSyncPayload
{
    public string target;      // 谁的血量（host/guest）
    public int hp;
}

[Serializable]
public class GameOverPayload
{
    public string winner;      // host / guest（V1两人局：接收方即胜者，字段为扩展保留）
}

[Serializable]
public class WallBrokenPayload
{
    public int id;             // 被打穿的墙的稳定ID（对端按ID找到同一面墙同步销毁）
}

[Serializable]
public class HeartbeatPayload
{
    // 空载荷：心跳只承担"证明连接活着"的职责，无业务数据
}

[Serializable]
public class RematchPayload
{
    // 空载荷：收到即"对方在结算面板点了准备"（双方都点过才开新局，由GameMgr裁决）
}

[Serializable]
public class PeerLeavePayload
{
    // 空载荷：收到即"对方已回主菜单，本局解散"
}
