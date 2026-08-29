using System.Collections;
using GameFramework;
using UnityEngine;

/// <summary>
/// 特效自动回收：计时结束后回对象池而不是销毁。
/// 注意用OnEnable重置计时——池化后Start只在首次激活执行一次，
/// 复用的特效不会再跑Start，计时必须每次激活时重启
/// </summary>
public class AutoDestroy : MonoBehaviour
{
    public float time = 2;

    void OnEnable()
    {
        StartCoroutine(HideWait(time));
    }

    IEnumerator HideWait(float t)
    {
        yield return new WaitForSeconds(t);
        //计时结束：回池复用（原为Destroy，池化对象禁止真销毁）
        PoolManager.Instance.PushObj(this.gameObject);
    }
}
