using System.Collections.Concurrent;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

/// <summary>
/// 联机中枢：创建主机/加入房间、昵称握手、消息订阅与主线程分发。
/// 主机=只开NetServer（发消息走广播）；客机=只开NetClient（发消息直连主机）——
/// 不让主机额外连自己的环回，避免"自己发的消息被回显"的双处分发问题。
/// 铁律：Socket后台线程只收字节入并发队列；一切Unity API在主线程Update出队后调用
/// </summary>
public class NetCenter : SingletonAutoMono<NetCenter>
{
    private NetServer server;
    private NetClient client;
    private readonly Dictionary<ushort, System.Action<NetMsg>> handlers = new Dictionary<ushort, System.Action<NetMsg>>();

    public bool IsHost { get; private set; }
    public bool Networking => server != null || client != null;
    public string MyName { get; private set; } = "玩家";
    public string PeerName { get; private set; } = "对方";

    /// <summary>对方坦克的影子（本机生成的、由网络消息驱动的复制体）</summary>
    public RemoteTank Remote { get; private set; }

    /// <summary>
    /// 生成"对方坦克"的影子：复用Monster1模型（剥离AI脚本），加RemoteTank驱动。
    /// 出生点=本机玩家旁边（让双方开局就能互相看见）
    /// </summary>
    public void SpawnRemoteTank()
    {
        if (Remote != null)
            return;                              // 已生成过

        GameObject prefab = Resources.Load<GameObject>("Prefabs/Game/Object/Monster1");
        if (prefab == null)
        {
            Debug.LogWarning("[Net] 影子预制体不存在：Monster1");
            return;
        }

        PlayerObj localPlayer = FindObjectOfType<PlayerObj>();
        Vector3 spawnPos = localPlayer != null
            ? localPlayer.transform.position + localPlayer.transform.forward * 4f
            : Vector3.zero;

        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
        Destroy(go.GetComponent<MonsterObj>());  // 剥离AI：影子只由网络消息驱动
        Remote = go.AddComponent<RemoteTank>();  // 炮台引用来自Monster1预制体自身序列化数据
        go.name = "RemoteTank";
    }

    /// <summary>分发TransformSync到影子坦克</summary>
    private void ApplyTransformSync(TransformPayload p)
    {
        Remote?.ApplyTransform(p);
    }

    /// <summary>主机侧：客机带着昵称加入时触发（参数=客机昵称）</summary>
    public event System.Action<string> GuestJoined;
    /// <summary>客机侧：主机应答加入成功时触发（参数=主机昵称）</summary>
    public event System.Action<string> JoinAcked;

    /// <summary>创建主机：监听端口（成功后本机为权威结算方）</summary>
    public bool StartHost(int port, string myName)
    {
        MyName = string.IsNullOrEmpty(myName) ? "主机" : myName;
        PeerName = "等待对方…";
        server = new NetServer();
        bool ok = server.Start(port);
        if (!ok)
            server = null;
        IsHost = ok;
        return ok;
    }

    /// <summary>加入主机（失败返回false，LanPanel负责提示）</summary>
    public bool JoinGuest(string ip, int port, string myName)
    {
        MyName = string.IsNullOrEmpty(myName) ? "客机" : myName;
        PeerName = "主机";
        client = new NetClient();
        bool ok = client.Connect(ip, port);
        if (!ok)
            client = null;
        return ok;
    }

    /// <summary>断开并清空（返回主菜单/换模式时调用）</summary>
    public void Shutdown()
    {
        server?.Close();
        client?.Close();
        server = null;
        client = null;
        IsHost = false;
        PeerName = "对方";
    }

    /// <summary>
    /// 退出Play/退出程序时自动关闭网络。
    /// 没有它：编辑器进程不退出，后台监听线程和端口会跨Play会话残留——
    /// 下次创建房间报"端口被占用"（本机实测踩过的坑）
    /// </summary>
    /// <summary>退出Play/退出程序时自动关闭网络（释放监听端口与后台线程）</summary>
    private void OnApplicationQuit()
    {
        Shutdown();
    }

    /// <summary>订阅消息（主线程分发时回调；同ID重复订阅=覆盖）。
    /// 注意：ReqJoin/AckJoin两个握手消息由本类内部消化，不会进到这里</summary>
    public void Subscribe(ushort msgId, System.Action<NetMsg> handler)
    {
        handlers[msgId] = handler;
    }

    /// <summary>发送消息：主机=广播给客机；客机=发给主机</summary>
    public void Send(ushort msgId, object payload)
    {
        if (server != null)
            server.Broadcast(msgId, payload);
        else
            client?.Send(msgId, payload);
    }

    private void Update()
    {
        //主线程消费网络队列：后台线程只入队，这里出队分发（Unity API只在主线程碰）
        if (server != null)
            while (server.receiveQueue.TryDequeue(out NetMsg msg))
                Dispatch(msg);

        if (client != null)
            while (client.receiveQueue.TryDequeue(out NetMsg msg))
                Dispatch(msg);
    }

    private void Dispatch(NetMsg msg)
    {
        //握手消息由中枢内部消化（存昵称+触发事件），不放通用分发
        switch ((MsgId)msg.msgId)
        {
            case MsgId.TransformSync:
            {
                TransformPayload p = JsonUtility.FromJson<TransformPayload>(msg.json);
                ApplyTransformSync(p);
                return;
            }
            case MsgId.ReqJoin:
            {
                JoinPayload join = JsonUtility.FromJson<JoinPayload>(msg.json);
                PeerName = string.IsNullOrEmpty(join?.name) ? "客机" : join.name;
                Send((ushort)MsgId.AckJoin, new AckJoinPayload { ok = true, playerName = MyName });
                GuestJoined?.Invoke(PeerName);
                return;
            }
            case MsgId.AckJoin:
            {
                AckJoinPayload ack = JsonUtility.FromJson<AckJoinPayload>(msg.json);
                PeerName = string.IsNullOrEmpty(ack?.playerName) ? "主机" : ack.playerName;
                JoinAcked?.Invoke(PeerName);
                return;
            }
        }

        if (handlers.TryGetValue(msg.msgId, out var handler))
            handler?.Invoke(msg);
    }
}
