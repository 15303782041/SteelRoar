using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 单个池子的数据：一个预制体对应一个池子，用自己的空父物体收纳闲置对象
    /// </summary>
    public class PoolData
    {
        public GameObject fatherObj;            // 该池子的父物体（Hierarchy整洁）
        public Queue<GameObject> poolQueue;     // 闲置对象队列

        public PoolData(GameObject obj, GameObject poolRootObj)
        {
            fatherObj = new GameObject(obj.name + "Pool");
            fatherObj.transform.SetParent(poolRootObj.transform);
            poolQueue = new Queue<GameObject>();
            // 注意：这里不入队！首个对象由 PoolManager.PushObj 统一入队，
            // 否则同一对象会被入队两次，导致一发子弹被"传唤"两次的诡异bug
        }

        public GameObject GetObj()
        {
            GameObject obj = poolQueue.Dequeue();
            obj.SetActive(true);
            obj.transform.SetParent(null);      // 取出时脱离池子，回到场景
            return obj;
        }

        public void PushObj(GameObject obj)
        {
            obj.SetActive(false);               // 失活即"回收"，不销毁
            obj.transform.SetParent(fatherObj.transform);
            poolQueue.Enqueue(obj);
        }
    }

    /// <summary>
    /// 对象池管理器：子弹、特效、掉落物等高频生灭对象统一走这里，
    /// 战斗全程不再 Instantiate/Destroy，消除运行时GC峰值。
    ///
    /// 使用规则（重要）：
    /// 1. 取出后由调用方立刻设置位置/旋转/状态
    /// 2. 回收前由调用方重置对象状态（速度清零、计数清零、血量回满），
    ///    否则池子里的"旧对象"会带着上一次的状态复活
    /// </summary>
    public class PoolManager : SingletonAutoMono<PoolManager>
    {
        /// <summary>
        /// 对象池总开关（Profiler A/B量化实验用）：false时GetObj退化为直接Instantiate、
        /// PushObj退化为Destroy——用于实测"池化 vs 直接创建"的GC Alloc差异，数据进README。平时保持true
        /// </summary>
        public static bool PoolsEnabled = true;

        private Dictionary<string, PoolData> poolDic = new Dictionary<string, PoolData>();
        private GameObject poolRootObj;         // 所有池子的总根节点

        /// <summary>从池子取对象：池中有闲置的直接复用，没有才真正Instantiate（且仅此一次）</summary>
        public GameObject GetObj(GameObject prefab)
        {
            GameObject obj = null;
            if (PoolsEnabled && poolDic.ContainsKey(prefab.name) && poolDic[prefab.name].poolQueue.Count > 0)
                obj = poolDic[prefab.name].GetObj();
            else
                obj = Instantiate(prefab);

            // 复用对象必须清空物理遗留状态：
            // 刚体速度不清零的话，对象会带着上一次"飞行"攒下的重力下坠速度复活，
            // 越复用坠得越快（Destroy旧方案每次都是全新对象，所以教程代码从没暴露过这个问题）
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            return obj;
        }

        /// <summary>回收对象进池子（按预制体名归池）；池总开关关闭时退化为Destroy（A/B对照用）</summary>
        public void PushObj(GameObject obj)
        {
            if (!PoolsEnabled)
            {
                Destroy(obj);
                return;
            }
            // Instantiate出来的对象名字带"(Clone)"后缀，必须剥掉才能和预制体名对上
            string name = obj.name.Replace("(Clone)", "");
            if (!poolDic.ContainsKey(name))
                poolDic.Add(name, new PoolData(obj, GetRoot()));
            poolDic[name].PushObj(obj);
        }

        private GameObject GetRoot()
        {
            if (poolRootObj == null)
            {
                poolRootObj = new GameObject("PoolRoot");
                DontDestroyOnLoad(poolRootObj);
            }
            return poolRootObj;
        }
    }
}
