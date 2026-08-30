using System.Collections.Concurrent;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

/// <summary>
/// 联机中枢：创建主机/加入房间、消息订阅与主线程分发。
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

    /// <summary>创建主机：监听端口（成功后本机为权威结算方）</summary>
    public bool StartHost(int port)
    {
        server = new NetServer();
        bool ok = server.Start(port);
        if (!ok)
            server = null;
        IsHost = ok;
        return ok;
    }

    /// <summary>加入主机（失败返回false，LanPanel负责提示）</summary>
    public bool JoinGuest(string ip, int port)
    {
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
    }

    /// <summary>订阅消息（主线程分发时回调；同ID重复订阅=覆盖）</summary>
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
        if (handlers.TryGetValue(msg.msgId, out var handler))
            handler?.Invoke(msg);
    }
}
