using System;

/// <summary>联机消息ID统一登记（新增消息就在这里加，同事件中心思想）</summary>
public enum MsgId : ushort
{
    ReqJoin = 100,        // 客→主：请求加入
    AckJoin = 101,        // 主→客：加入应答
    TransformSync = 200,  // 双向15Hz：坦克位置与朝向
    FireEvent = 201,      // 即时双向：开火事件（本地预表现+对端生成表现）
    HpSync = 202,         // 主→客：血量同步（主机权威结算）
    GameOver = 300,       // 主→客：对局结束
    Disconnect = 400,     // 双向：断线通知（接收线程关闭时入队）
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
    public string winner;      // host / guest
}
