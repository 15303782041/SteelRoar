using UnityEngine;

/// <summary>
/// 远端坦克影子：由网络消息驱动，无任何本地输入逻辑。
/// 收到TransformSync→记录目标位姿；Update里向目标插值——
/// 15Hz的消息间隔靠插值补平滑（网络同步的标准手法：状态低频同步+本地插值）
/// </summary>
public class RemoteTank : MonoBehaviour
{
    public Transform tankHead;              // 炮台（预制体自带关联）

    private Vector3 targetPos;              // 目标位置
    private float targetBodyRy;             // 目标车体朝向
    private float targetHeadRy;             // 目标炮塔朝向

    private const float PosLerpSpeed = 10f;
    private const float RotLerpSpeed = 12f;

    void Update()
    {
        //位置插值
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * PosLerpSpeed);

        //车体朝向插值（Yaw用LerpAngle避免角度绕圈跳变）
        float bodyY = Mathf.LerpAngle(transform.eulerAngles.y, targetBodyRy, Time.deltaTime * PosLerpSpeed);
        transform.rotation = Quaternion.Euler(0f, bodyY, 0f);

        //炮塔朝向插值
        if (tankHead != null)
        {
            float headY = Mathf.LerpAngle(tankHead.eulerAngles.y, targetHeadRy, Time.deltaTime * RotLerpSpeed);
            tankHead.rotation = Quaternion.Euler(0f, headY, 0f);
        }
    }

    /// <summary>应用网络下发的目标位姿（由NetCenter分发TransformSync时调用）</summary>
    public void ApplyTransform(TransformPayload p)
    {
        targetPos = new Vector3(p.x, p.y, p.z);
        targetBodyRy = p.bodyRy;
        targetHeadRy = p.headRy;
    }
}
