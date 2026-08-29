using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

public class BulletObj : MonoBehaviour
{
    public float moveSpeed = 50;
    //存活上限：没命中任何对象的子弹到时强制回池，防止泄漏堆积
    public float lifeTime = 3;
    private float nowLife = 0;
    //谁发射的子弹
    public TankBaseObj fatherObj;
    public GameObject effObj;

    //每次从池中取出（激活）时重置存活计时
    void OnEnable()
    {
        nowLife = 0;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);

        nowLife += Time.deltaTime;
        if (nowLife >= lifeTime)
        {
            PoolManager.Instance.PushObj(this.gameObject);
        }
    }

    //和别人碰撞时触发
    private void OnTriggerEnter(Collider other)
    {
        //子弹射击到立方体上面会爆炸
        //同样射击到不同阵营对象也会爆炸
        if (other.CompareTag("Cube")|| other.CompareTag("Player") && fatherObj.CompareTag("Monster")
            || other.CompareTag("Monster") && fatherObj.CompareTag("Player")) 
        { 
            //判断是否受伤
            //得到碰撞到的对象身上 是否有坦克相关的脚本 我们用里氏替换原则
            //通过父类去获取
            TankBaseObj obj = other.GetComponent<TankBaseObj>();
            if(obj != null)
            {
                obj.Wound(fatherObj);
            }
               

            //当子弹销毁时 可以创建一个爆炸特效
            if(effObj != null)
            {
                //特效从池中取出（不再Instantiate），音量/开关统一交给音乐管理器
                GameObject eff = PoolManager.Instance.GetObj(effObj);
                eff.transform.position = this.transform.position;
                eff.transform.rotation = this.transform.rotation;
                MusicManager.Instance.SetSourceVolume(eff.GetComponent<AudioSource>());
            }
            //子弹回池复用（不再Destroy，消除运行时GC）
            //本对象飞行期间无可变状态（moveSpeed恒定、fatherObj每次发射时重设），无需额外重置
            PoolManager.Instance.PushObj(this.gameObject);
        }
        
    }

    //设置拥有者
    public void SetFather(TankBaseObj obj)
    {
        fatherObj = obj;
    }
}
