using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// 网络客户端：连接+收发。发送在主线程调用（锁保护网络流），
/// 接收在专用后台线程（stream.Read会阻塞，绝不能放主线程）——
/// 后台线程只把解析出的消息入并发队列，一切Unity API由主线程消费（线程安全铁律）
/// </summary>
public class NetClient
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread recvThread;
    private readonly object sendLock = new object();
    private readonly NetFrame.Parser parser = new NetFrame.Parser();

    public readonly ConcurrentQueue<NetMsg> receiveQueue = new ConcurrentQueue<NetMsg>();
    public bool Connected => client != null && client.Connected;

    public bool Connect(string ip, int port)
    {
        try
        {
            client = new TcpClient();
            client.Connect(ip, port);
            stream = client.GetStream();
            recvThread = new Thread(ReceiveLoop) { IsBackground = true };
            recvThread.Start();
            Debug.Log($"[Net] 已连接 {ip}:{port}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Net] 连接失败 {ip}:{port} — {e.Message}");
            return false;
        }
    }

    public void Send(ushort msgId, object payload)
    {
        if (!Connected)
            return;
        byte[] packet = NetFrame.Encode(msgId, JsonUtility.ToJson(payload));
        lock (sendLock)
        {
            try { stream.Write(packet, 0, packet.Length); }
            catch (System.Exception) { /* 断线由接收线程感知并广播 */ }
        }
    }

    public void Close()
    {
        try { stream?.Close(); client?.Close(); } catch { }
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
                    break;                                  // 对端正常关闭
                foreach (NetMsg msg in parser.Parse(buf, n))
                    receiveQueue.Enqueue(msg);
            }
        }
        catch { /* 连接异常中断 */ }

        //无论正常关闭还是异常断线，都广播一次断线消息供上层提示
        receiveQueue.Enqueue(new NetMsg { msgId = (ushort)MsgId.Disconnect, json = "" });
    }
}
