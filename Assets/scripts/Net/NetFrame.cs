using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 帧编解码：[4字节长度][2字节msgId][JSON正文UTF8]。
/// TCP是字节流、没有消息边界——长度前缀分帧解决粘包/半包（面试高频）
/// </summary>
public static class NetFrame
{
    public static byte[] Encode(ushort msgId, string json)
    {
        byte[] idBytes = BitConverter.GetBytes(msgId);
        byte[] body = Encoding.UTF8.GetBytes(json ?? "");
        byte[] packet = new byte[4 + 2 + body.Length];
        BitConverter.GetBytes(2 + body.Length).CopyTo(packet, 0);   // 长度=长度字段之外的全部
        idBytes.CopyTo(packet, 4);
        body.CopyTo(packet, 6);
        return packet;
    }

    /// <summary>
    /// 流式解析器：持有半包余料，每次喂入新到达的字节块，吐出其中所有完整帧。
    /// 演示实现用List+RemoveRange（O(n)拷贝），面试可讲优化方向：环形缓冲/ArraySegment零拷贝
    /// </summary>
    public class Parser
    {
        private readonly List<byte> buffer = new List<byte>();

        public List<NetMsg> Parse(byte[] chunk, int length)
        {
            for (int i = 0; i < length; i++)
                buffer.Add(chunk[i]);

            List<NetMsg> result = new List<NetMsg>();
            while (buffer.Count >= 4)
            {
                int len = BitConverter.ToInt32(buffer.ToArray(), 0);
                if (len < 2 || len > 64 * 1024)
                {
                    buffer.Clear();                    // 长度字段异常：丢弃缓冲自保
                    break;
                }
                if (buffer.Count < 4 + len)
                    break;                             // 半包：剩余数据不足一帧，等下一次

                ushort msgId = BitConverter.ToUInt16(buffer.ToArray(), 4);
                string json = Encoding.UTF8.GetString(buffer.ToArray(), 6, len - 2);
                result.Add(new NetMsg { msgId = msgId, json = json });

                buffer.RemoveRange(0, 4 + len);
            }
            return result;
        }
    }
}
