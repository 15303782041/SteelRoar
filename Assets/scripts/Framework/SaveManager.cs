using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace GameFramework
{
    /// <summary>存档数据结构：后续要存什么就往这里加字段</summary>
    [Serializable]
    public class GameSaveData
    {
        public int highestScore;        // 历史最高分
        public int totalKills;          // 累计击杀数
    }

    /// <summary>
    /// 存档管理器：Json序列化 + 异或加密（防玩家手改存档）。
    /// 与教程的PlayerPrefs体系（音乐设置/排行榜）并行，负责新系统的核心进度；
    /// 异或原理：a^k^k=a，加密解密同一把钥匙；局限：防君子不防小人，商业项目用AES
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        private const string FileName = "save.bin";
        private const byte XorKey = 0x5A;

        public GameSaveData NowData = new GameSaveData();

        /// <summary>读档（文件不存在或损坏时沿用默认数据，不报错中断）</summary>
        public void Load()
        {
            string path = Path.Combine(Application.persistentDataPath, FileName);
            if (!File.Exists(path))
                return;

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] ^= XorKey;                       // 逐字节异或解密
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(Encoding.UTF8.GetString(bytes));
                if (data != null)
                {
                    NowData = data;
                    Debug.Log($"[存档] 读取完成：历史最高分{NowData.highestScore} 累计击杀{NowData.totalKills}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[存档] 读取失败：{e.Message}（沿用默认数据）");
            }
        }

        /// <summary>记录一局得分：超过历史最高分则刷新并立即写盘</summary>
        public void RecordScore(int score)
        {
            if (score > NowData.highestScore)
            {
                NowData.highestScore = score;
                Debug.Log($"[存档] 新纪录！历史最高分刷新为 {score}");
            }
            Save();
        }

        /// <summary>累计击杀（Boss战/结算处调用）</summary>
        public void AddKill()
        {
            NowData.totalKills++;
        }

        /// <summary>写盘：Json→字节→逐字节异或→文件</summary>
        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(NowData);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] ^= XorKey;
                File.WriteAllBytes(Path.Combine(Application.persistentDataPath, FileName), bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"[存档] 写入失败：{e.Message}");
            }
        }
    }
}
