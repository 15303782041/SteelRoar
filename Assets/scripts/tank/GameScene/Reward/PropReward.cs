using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum E_PropType
{
    //加属性的类型
    Atk,
    Def,
    MaxHp,
    Hp,
}

public class PropReWard : MonoBehaviour
{
    public E_PropType type = E_PropType.Atk;
    public int changeValue = 2;
    //获取特效
    public GameObject getEff;

    private void OnTriggerEnter(Collider other)
    {
        //玩家才能获取
        if (other.CompareTag("Player"))
        {
            //得到对应的玩家脚本
            PlayerObj player = other.GetComponent<PlayerObj>();
            //根据类型加属性
            switch (type)
            {
                case E_PropType.Atk:
                    player.atk += changeValue;
                    break;
                case E_PropType.Def:
                    player.def += changeValue;
                    break;
                case E_PropType.MaxHp:
                    player.maxHp += changeValue;
                    //更新血条
                    GamePanel.Instance.UpdateHP(player.maxHp, player.hp);
                    break;
                case E_PropType.Hp:
                    player.hp += changeValue;
                    //不能超过最大血量
                    if (player.hp > player.maxHp)
                       player.hp = player.maxHp;
                    GamePanel.Instance.UpdateHP(player.maxHp, player.hp);
                    break;
            }

            //创建特效
            //设置音效
            //奖励特效
            GameObject eff = Instantiate(getEff, this.transform.position, this.transform.rotation);
            //控制获取音效
            AudioSource audio = eff.GetComponent<AudioSource>();
            //大小和开启状态
            audio.volume = GameDataMgr.Instance.musicData.soundValue;
            audio.mute = !GameDataMgr.Instance.musicData.isOpenSound;
            Destroy(this.gameObject);
        }
    }
} 
