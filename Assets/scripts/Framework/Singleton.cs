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
        //退出保险丝：应用退出/停止运行时各物体销毁顺序随机，
        //若其他类的OnDestroy晚于本类销毁并访问Instance，会触发"退出中创建新物体"
        //→Unity报"Some objects were not cleaned up when closing the scene"
        private static bool isQuit = false;

        public static T Instance
        {
            get
            {
                //退出过程中不再创建，直接返回现有引用（可能已销毁，由调用方判空）
                if (instance == null && !isQuit)
                {
                    GameObject obj = new GameObject(typeof(T).Name);
                    DontDestroyOnLoad(obj);
                    instance = obj.AddComponent<T>();
                }
                return instance;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            isQuit = true;
        }
    }
}
