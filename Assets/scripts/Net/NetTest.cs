using UnityEngine;

/// <summary>
/// Day N1联机管道验收脚本（临时，Day N2接入LanPanel后删除）：
/// 挂到任意物体上，Play后通过组件右键菜单依次执行：1_开主机 → 2_连接本机 → 3_发测试消息
/// 单编辑器自环回测试：主机+本机客户端在同进程内走完整收发链路
/// </summary>
public class NetTest : MonoBehaviour
{
    void Start()
    {
        //订阅ReqJoin：主机队列与客机队列两条路都会走到这里
        NetCenter.Instance.Subscribe((ushort)MsgId.ReqJoin, msg =>
            Debug.Log($"[Net测试] 收到ReqJoin: {msg.json}"));
    }

    [ContextMenu("1_开主机")]
    void Host()
    {
        Debug.Log(NetCenter.Instance.StartHost(7777, "tester") ? "[Net测试] 主机已监听7777" : "[Net测试] 监听失败");
    }

    [ContextMenu("2_连接本机")]
    void Join()
    {
        Debug.Log(NetCenter.Instance.JoinGuest("127.0.0.1", 7777, "tester")
            ? "[Net测试] 本机客户端已连接"
            : "[Net测试] 连接失败");
    }

    [ContextMenu("3_发测试消息")]
    void Send()
    {
        NetCenter.Instance.Send((ushort)MsgId.ReqJoin, new JoinPayload { name = "tester" });
        Debug.Log("[Net测试] 已发送ReqJoin（若上方收到日志则链路通）");
    }
}
