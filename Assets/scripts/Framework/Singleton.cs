using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 普通类单例基类：不挂在场景对象上的管理器使用（如数据管理器）
    /// 用法：public class SaveManager : Singleton<SaveManager> { ... }
    /// </summary>
    public class Singleton<T> where T : new()
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                    instance = new T();
                return instance;
            }
        }
    }

    /// <summary>
    /// Mono类单例基类：首次访问时自动创建GameObject并挂载脚本，切换场景不销毁。
    /// 场景里已有同类实例时直接复用（FindFirstObjectByType兜底），
    /// 退出清理过程中被访问也只会找到"正在销毁"的实例，绝不凭空重建物体
    /// </summary>
    public class SingletonAutoMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<T>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject(typeof(T).Name);
                        DontDestroyOnLoad(obj);
                        instance = obj.AddComponent<T>();
                    }
                }
                return instance;
            }
        }
    }
}
