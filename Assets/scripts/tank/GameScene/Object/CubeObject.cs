using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

/// <summary>
/// 可破坏箱子：只有玩家子弹能破坏（怪物子弹/坦克碰撞无效），需打掉maxHp点血。
/// 血量归零：概率掉落奖励 + 播放破坏特效 + 销毁
/// </summary>
public class CubeObject : MonoBehaviour
{
    //奖励预设体关联
    public GameObject[] rewardObject;
    //死亡预设体关联
    public GameObject deadEff;
    [Header("可破坏设置")]
    public int maxHp = 3;               // 需要命中次数（玩家子弹每发-1）

    private int hp;

    //场景加载/重新激活时重置血量（箱子不进对象池，是场景常驻物体）
    void OnEnable()
    {
        hp = maxHp;
    }

    private void OnTriggerEnter(Collider other)
    {
        //只有玩家子弹能破坏箱子：怪物子弹打上去只会自己爆炸，箱子毫发无伤
        BulletObj bullet = other.GetComponent<BulletObj>();
        if (bullet == null || !(bullet.fatherObj is PlayerObj))
            return;

        hp--;
        if (hp > 0)
            return;                      // 还没打穿：只扣血不销毁

        //血量归零：随机掉落奖励
        int rangeInt = Random.Range(0, 100);
        if (rangeInt < 50)
        {
            rangeInt = Random.Range(0, rewardObject.Length);
            //放在箱子当前所在的位置即可
            Instantiate(rewardObject[rangeInt], this.transform.position, this.transform.rotation);
        }

        //创建破坏特效，音量/开关统一交给音乐管理器
        GameObject effObj = Instantiate(deadEff, this.transform.position, this.transform.rotation);
        MusicManager.Instance.SetSourceVolume(effObj.GetComponent<AudioSource>(), true);

        Destroy(this.gameObject);
    }
}
