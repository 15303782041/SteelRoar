 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponReward : MonoBehaviour
{
    //有多个随机用到的  武器的预设体
    public GameObject[] weaponObj;
    //获取特效
    public GameObject getEff;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //让玩家 切换武器
            int index = Random.Range(0, weaponObj.Length);
            //得到撞在玩家身上的脚本 命令切换武器
            PlayerObj player = other.GetComponent<PlayerObj>();
            player.ChangeWeapon(weaponObj[index]);
            //奖励特效
            GameObject eff = Instantiate(getEff, this.transform.position, this.transform.rotation);
            //控制获取音效
            AudioSource audio = eff.GetComponent<AudioSource>();
            //大小和开启状态
            audio.volume = GameDataMgr.Instance.musicData.soundValue;
            audio.mute = !GameDataMgr.Instance.musicData.isOpenSound;
            //获取到自己后 移除自己
            Destroy(this.gameObject);
        }
    }
}
