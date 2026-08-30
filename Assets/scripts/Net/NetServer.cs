using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>服务器与单个客户端的会话：独立收发线程+解析器</summary>
public class NetSession
{
    private readonly TcpClient client;
    private readonly NetworkStream stream;
    private readonly Thread recvThread;
    private readonly object sendLock = new object();
    private readonly NetFrame.Parser parser = new NetFrame.Parser();
    private readonly System.Action<NetSession, NetMsg> onReceive;

    public NetSession(TcpClient client, System.Action<NetSession, NetMsg> onReceive)
    {
        this.client = client;
        this.onReceive = onReceive;
        stream = client.GetStream();
        recvThread = new Thread(ReceiveLoop) { IsBackground = true };
        recvThread.Start();
    }

    public void Send(NetMsg msg)
    {
        Send(msg.msgId, msg.json);
    }

    public void Send(ushort msgId, object payload)
    {
        byte[] packet = NetFrame.Encode(msgId, JsonUtility.ToJson(payload));
        lock (sendLock)
        {
            try { stream.Write(packet, 0, packet.Length); }
            catch (System.Exception) { /* 断线由接收线程感知 */ }
        }
    }

    private void ReceiveLoop()
    {
        byte[] buf = new byte[4096];
        try
        {
            while (true)
            {
                int n = stream.Read(buf, 0, buf.Length);
                if (n <= 0)
                    break;
                foreach (NetMsg msg in parser.Parse(buf, n))
                    onReceive?.Invoke(this, msg);
            }
        }
        catch { }

        onReceive?.Invoke(this, new NetMsg { msgId = (ushort)MsgId.Disconnect, json = "" });
    }

    public void Close()
    {
        try { stream?.Close(); client?.Close(); } catch { }
    }
}

/// <summary>
/// 网络服务器：监听端口，为每个接入的客户端建立会话。
/// 收到任意会话的消息→入主线程队列+中继给其他会话（2人局=客机↔主机的中继站）
/// </summary>
public class NetServer
{
    private TcpListener listener;
    private Thread acceptThread;
    private readonly List<NetSession> sessions = new List<NetSession>();
    private readonly object sessionLock = new object();
    private readonly NetFrame.Parser parser = new NetFrame.Parser();

    public ConcurrentQueue<NetMsg> receiveQueue = new ConcurrentQueue<NetMsg>();
    public bool Listening => listener != null;

    /// <summary>是否有客户端会话在线（心跳/超时检测只对"真有对端"的状态运行，主机等待加入期间不算）</summary>
    public bool HasSessions
    {
        get { lock (sessionLock) return sessions.Count > 0; }
    }

    public bool Start(int port)
    {
        try
        {
            listener = new TcpListener(System.Net.IPAddress.Any, port);
            listener.Start();
            acceptThread = new Thread(AcceptLoop) { IsBackground = true };
            acceptThread.Start();
            Debug.Log($"[Net] 服务器已监听端口 {port}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Net] 监听失败 端口{port} — {e.Message}");
            return false;
        }
    }

    private void AcceptLoop()
    {
        while (true)
        {
            TcpClient client;
            try
            {
                client = listener.AcceptTcpClient();
            }
            catch (SocketException)
            {
                //监听器已关闭（退出Play时的正常关闭路径）：安静退出线程，
                //否则阻塞中的Accept被中断会抛SocketException刷屏
                break;
            }
            NetSession session = new NetSession(client, OnSessionReceive);
            lock (sessionLock)
                sessions.Add(session);
            Debug.Log($"[Net] 客户端接入（当前会话数 {sessions.Count}）");
        }
    }

    private void OnSessionReceive(NetSession session, NetMsg msg)
    {
        receiveQueue.Enqueue(msg);          // 入主线程队列（主机侧消费）

        //中继给其他会话（2人局：主机本地不建会话，这里只有远程客机一个会话）
        lock (sessionLock)
            foreach (NetSession s in sessions)
                if (s != session)
                    s.Send(msg);
    }

    /// <summary>广播给所有会话（主机发消息给客机用）</summary>
    public void Broadcast(ushort msgId, object payload)
    {
        lock (sessionLock)
            foreach (NetSession s in sessions)
                s.Send(msgId, payload);
    }

    public void Close()
    {
        try { listener?.Stop(); } catch { }
        lock (sessionLock)
        {
            foreach (NetSession s in sessions)
                s.Close();
            sessions.Clear();
        }
    }
}
