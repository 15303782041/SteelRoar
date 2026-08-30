using System.Collections.Concurrent;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    // ---- 保活与断线检测 ----
    private const float HeartbeatInterval = 1f;      // 心跳周期（秒）
    private const float PeerTimeoutSeconds = 5f;     // 连续收不到任何消息判为断线
    private float lastRecvTime;                      // 最近一次收到对端消息的时刻（unscaled）
    private float lastHeartbeatTime;                 // 最近一次发出心跳的时刻（unscaled）
    private bool peerLostHandled;                    // 断线只处理一次（超时与Disconnect可能先后到达）

    public bool IsHost { get; private set; }
    public bool Networking => server != null || client != null;
    public string MyName { get; private set; } = "玩家";
    public string PeerName { get; private set; } = "对方";

    /// <summary>对方坦克的影子（本机生成的、由网络消息驱动的复制体）</summary>
    public RemoteTank Remote { get; private set; }

    /// <summary>当前是否存在活跃对端连接（心跳/超时检测只在有对端时运行）</summary>
    private bool HasActivePeer =>
        (client != null && client.Connected) ||
        (server != null && server.HasSessions);

    private void Awake()
    {
        //打包后窗口失焦默认暂停Update→心跳停发→对端误判超时。
        //联机游戏必须允许后台运行（局域网双开时切窗口是常态）
        Application.runInBackground = true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>离开战斗场景/重载战斗场景时清掉影子引用：影子是场景物体，跨场景持有会变成悬空引用。
    /// 重载GameScene（再来一局）同样会触发sceneLoaded，所以不做场景名区分、一律清空
    /// （新影子由场景加载完成回调里的SpawnRemoteTank重建）</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Remote = null;
    }

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
        RemoteTank remote = go.AddComponent<RemoteTank>();
        MonsterObj legacyAI = go.GetComponent<MonsterObj>();
        if (legacyAI != null)
        {
            //AddComponent挂的是全新组件，序列化字段全是默认值（tankHead=null会让炮塔同步失效、
            //bulletObj=null会让对端开火不出弹）——趁剥离AI前把预制体里配好的引用抄过来
            remote.tankHead = legacyAI.tankHead;     // 炮台Transform
            remote.bulletObj = legacyAI.bulletObj;   // 子弹预制体（网络表现弹用）
            remote.deadEff = legacyAI.deadEff;       // 死亡爆炸特效
            Destroy(legacyAI);                       // 剥离AI：影子只由网络消息驱动
        }
        go.name = "RemoteTank";
        go.SetActive(false);        // 先隐身：收到对方首个位姿包才现身（RemoteTank.ApplyTransform里吸附到位）
        Remote = remote;
    }

    /// <summary>胜利方调用：对方坦克已阵亡——影子原地爆炸消失（碰撞体一并失效=禁止再被命中）</summary>
    public void ExplodeRemote()
    {
        Remote?.Die();
        Remote = null;
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
        ResetLinkState();
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
        ResetLinkState();
        return ok;
    }

    /// <summary>建立新连接前重置保活状态：上一次对局的断线标记/计时不得串局</summary>
    private void ResetLinkState()
    {
        lastRecvTime = Time.unscaledTime;
        lastHeartbeatTime = Time.unscaledTime;
        peerLostHandled = false;
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
        Remote = null;
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

        UpdateKeepAlive();
    }

    /// <summary>
    /// 保活与断线检测：有活跃对端时1Hz发心跳；任何消息（含心跳）都刷新lastRecvTime，
    /// 超过5秒收不到任何消息即判断线——TCP静默掉线（拔线/杀进程）不会主动通知，
    /// 只有应用层心跳能发现（操作系统层面的TCP保活默认要2小时才生效）
    /// </summary>
    private void UpdateKeepAlive()
    {
        if (!Networking || !HasActivePeer || peerLostHandled)
            return;

        if (Time.unscaledTime - lastHeartbeatTime >= HeartbeatInterval)
        {
            lastHeartbeatTime = Time.unscaledTime;
            Send((ushort)MsgId.Heartbeat, new HeartbeatPayload());
        }

        if (Time.unscaledTime - lastRecvTime > PeerTimeoutSeconds)
            OnPeerLost($"连接超时（{PeerTimeoutSeconds}秒未收到对方消息）");
    }

    /// <summary>对端失联（断线通知/心跳超时统一入口）：清理链路→战斗中弹提示面板</summary>
    private void OnPeerLost(string reason)
    {
        if (peerLostHandled)
            return;
        peerLostHandled = true;

        bool inGame = SceneManager.GetActiveScene().name == "GameScene";
        Shutdown();                          // 先断链路：队列停止消费，后续包不再触发本方法

        if (!inGame)
            return;                          // 主菜单里断线：静默清理即可，不打扰

        NetLostTip.Instance.Show(reason);    // 战斗中断线：明确告知并提供回主菜单出口
    }

    /// <summary>对方主动离开本局（PeerLeave消息）：断链；还在战斗中就弹提示告知</summary>
    private void OnPeerLeft()
    {
        peerLostHandled = true;              // 与断线检测共用守卫：本局网络到此为止

        bool inGame = SceneManager.GetActiveScene().name == "GameScene";
        Shutdown();

        if (!inGame)
            return;                          // 对方走时我已在主菜单：静默清理

        NetLostTip.Instance.Show("对方已返回主菜单，本局结束");
    }

    /// <summary>
    /// 联机中主动离开本局（死亡面板回主菜单/战斗中ESC退出）：先告知对方再断链。
    /// 顺序说明：Send是同步Write（字节已交给操作系统），随后Close以正常挥手收尾，
    /// 消息保证送达——对方会弹"对方已返回主菜单"并各自清理，不会留下一台机器悬在死局里
    /// </summary>
    public void NotifyLeaveAndShutdown()
    {
        if (!Networking)
            return;
        Send((ushort)MsgId.PeerLeave, new PeerLeavePayload());
        Shutdown();
    }

    private void Dispatch(NetMsg msg)
    {
        lastRecvTime = Time.unscaledTime;    // 任何到达的消息都是"对方活着"的证据

        //握手消息由中枢内部消化（存昵称+触发事件），不放通用分发
        switch ((MsgId)msg.msgId)
        {
            case MsgId.Heartbeat:
                return;                      // 只为保活，无业务处理

            case MsgId.TransformSync:
            {
                TransformPayload p = JsonUtility.FromJson<TransformPayload>(msg.json);
                ApplyTransformSync(p);
                return;
            }
            case MsgId.FireEvent:
            {
                //对端开火：在本机生成表现子弹（打到本机玩家时由NetworkBullet本地结算扣血）
                FirePayload fp = JsonUtility.FromJson<FirePayload>(msg.json);
                if (fp != null && Remote != null)
                    Remote.SpawnNetworkBullet(fp);
                return;
            }
            case MsgId.WallBroken:
            {
                //对端打穿了墙：本机同ID墙同款销毁（ID按坐标排序分配，双方一致）
                WallBrokenPayload wp = JsonUtility.FromJson<WallBrokenPayload>(msg.json);
                if (wp != null)
                    CubeObject.BreakByNetwork(wp.id);
                return;
            }
            case MsgId.Disconnect:
                OnPeerLost("对方已断线");
                return;
            case MsgId.PeerLeave:
                OnPeerLeft();
                return;
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
