using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    //摄像机看向的目标
    public Transform targetPlayer;
    public float H = 10;
    private Vector3 pos;
    void LateUpdate()
    {
        if(targetPlayer == null)
            return;
            //x和z和玩家一样
            pos.x = targetPlayer.position.x;
            pos.z = targetPlayer.position.z;
            pos.y = H;          //y才是高度
            this.transform.position = pos;
        
    }
}
