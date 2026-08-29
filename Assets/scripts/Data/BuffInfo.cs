using UnityEngine;

/// <summary>Buff的效果类别（玩家收到Buff后按类别应用效果）</summary>
public enum BuffType
{
    Attack,      // 攻击力 +value（每层）
    MoveSpeed,   // 移速 ×(1+value)（每层）
    MaxHp,       // 最大生命 +value，并回复等量
    Lifesteal,   // 每次命中敌方坦克回复value点生命
    Shield,      // 获得 value 层护盾（每层抵挡一次伤害）
}

/// <summary>
/// 肉鸽Buff配置（ScriptableObject资产）。
/// 新增一种Buff = Project窗口右键 Create → 肉鸽Buff → BuffInfo 建一条资产，零代码。
/// 资产统一放在 Assets/Resources/Buffs/ 下（面板启动时按文件夹全部加载）
/// </summary>
[CreateAssetMenu(menuName = "肉鸽Buff/BuffInfo", fileName = "Buff_新强化")]
public class BuffInfo : ScriptableObject
{
    public string buffName = "新强化";
    [TextArea] public string description = "效果描述";
    public BuffType type = BuffType.Attack;
    public float value = 5;             // 效果数值（含义随type变化）
    public int stackMax = 3;            // 最大叠层数：达到上限后不再出现在三选一中
}
