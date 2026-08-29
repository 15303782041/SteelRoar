using System;
using System.Collections.Generic;

namespace GameFramework
{
    /// <summary>
    /// 全游戏事件名统一登记处。新增事件就在这里加枚举——
    /// 用枚举做key而不是字符串：拼错编译期直接报错
    /// </summary>
    public enum EEventType
    {
        PlayerHurt,       // 参数：float[]{当前血量, 最大血量}
        PlayerDead,       // 参数：null
        MonsterDead,      // 参数：int 击杀得分
        ScoreChange,      // 参数：int 当前总分
        WaveStart,        // 参数：int 波次编号
        WaveClear,        // 参数：int 波次编号
        GameWin,          // 参数：null
        GameLose,         // 参数：int 本局得分
        BuffChooseStart,  // 参数：null
        Loading,          // 参数：float 场景加载进度 0~1
    }

    /// <summary>
    /// 事件中心（观察者模式）：模块之间通过事件通信，发送方不需要知道接收方是谁。
    /// 例：坦克受伤只管发PlayerHurt事件，UI监听它刷新血条——战斗代码里不允许出现任何UI引用。
    ///
    /// 使用规则（重要）：
    /// 1. 订阅时用委托成员变量保存引用，不要直接传lambda——匿名函数无法解绑
    /// 2. OnDisable/OnDestroy 里必须 RemoveEventListener，否则对象销毁后残留监听会空引用
    /// </summary>
    public class EventCenter : SingletonAutoMono<EventCenter>
    {
        private Dictionary<EEventType, Action<object>> eventDic = new Dictionary<EEventType, Action<object>>();

        /// <summary>订阅事件</summary>
        public void AddEventListener(EEventType type, Action<object> callback)
        {
            if (eventDic.ContainsKey(type))
                eventDic[type] += callback;
            else
                eventDic.Add(type, callback);
        }

        /// <summary>取消订阅（必须在对象销毁前调用）</summary>
        public void RemoveEventListener(EEventType type, Action<object> callback)
        {
            if (eventDic.ContainsKey(type))
                eventDic[type] -= callback;
        }

        /// <summary>触发事件，info是需要传递的数据（无数据传null）</summary>
        public void EventTrigger(EEventType type, object info)
        {
            if (eventDic.ContainsKey(type))
                eventDic[type]?.Invoke(info);
        }

        /// <summary>
        /// 清空所有监听。EventCenter本身 DontDestroyOnLoad，
        /// 只在游戏退出时销毁；这里兜底防止退出时机残留引用报错
        /// </summary>
        private void OnDestroy()
        {
            eventDic.Clear();
        }
    }
}
