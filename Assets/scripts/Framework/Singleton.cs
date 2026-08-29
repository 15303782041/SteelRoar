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
    /// Mono类单例基类：首次访问时自动创建GameObject并挂载脚本，切换场景不销毁
    /// 用法：public class PoolManager : SingletonAutoMono<PoolManager> { ... }
    /// 注意：MonoBehaviour禁止new（必须AddComponent创建），所以与普通单例分成两个基类
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
                    // 用类型名命名对象，Hierarchy里一眼能认出是哪个管理器
                    GameObject obj = new GameObject(typeof(T).Name);
                    // 管理器属于全局模块，切换场景不能被销毁
                    DontDestroyOnLoad(obj);
                    instance = obj.AddComponent<T>();
                }
                return instance;
            }
        }
    }
}
