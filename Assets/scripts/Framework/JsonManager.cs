using System;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// Json配置加载器：从 Resources/Configs/ 下读取Json文本并反序列化为数据类。
    /// 解析失败返回null并打印错误，由调用方决定兜底行为（如沿用Inspector数值）
    /// </summary>
    public class JsonManager : Singleton<JsonManager>
    {
        /// <summary>fileName不带扩展名，例如 "MonsterConfig"</summary>
        public T LoadData<T>(string fileName) where T : class
        {
            TextAsset asset = Resources.Load<TextAsset>("Configs/" + fileName);
            if (asset == null)
            {
                Debug.LogWarning($"Json配置不存在：Resources/Configs/{fileName}.json，将使用调用方默认值");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"Json解析失败：{fileName}\n{e.Message}");
                return null;
            }
        }
    }
}
