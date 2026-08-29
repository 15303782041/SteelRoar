using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFramework
{
    /// <summary>
    /// 场景管理器：统一异步加载场景，加载进度通过事件中心广播（UI监听后可刷新进度条）。
    /// 面板切换场景一律走这里，不再直接调用 SceneManager（同步加载会卡死画面一帧）
    /// </summary>
    public class SceneMgr : SingletonAutoMono<SceneMgr>
    {
        /// <summary>异步加载场景，完成后触发callback。场景名以Build Settings中注册的名字为准</summary>
        public void LoadScene(string name, Action callback)
        {
            StartCoroutine(ReallyLoad(name, callback));
        }

        private IEnumerator ReallyLoad(string name, Action callback)
        {
            AsyncOperation ao = SceneManager.LoadSceneAsync(name);
            while (!ao.isDone)
            {
                // 广播加载进度0~1，LoadingPanel监听刷新进度条
                EventCenter.Instance.EventTrigger(EEventType.Loading, ao.progress);
                yield return null;
            }
            //加载完成：进度拉满（LoadingPanel据此隐藏）
            EventCenter.Instance.EventTrigger(EEventType.Loading, 1f);
            callback?.Invoke();
        }
    }
}
