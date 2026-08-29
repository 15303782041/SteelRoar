using System.Collections.Generic;

/// <summary>
/// 单个怪物的数值配置。字段与MonsterObj/TankBaseObj的Inspector字段一一对应
/// </summary>
[System.Serializable]
public class MonsterInfo
{
    public string monsterName;      // 配置键：场景中怪物靠这个名字找到自己的配置
    public string prefabName;       // 预制体名（Day 7波次系统按此生成用）
    public int atk;                 // 攻击力
    public int def;                 // 防御力
    public int maxHp;               // 最大血量
    public float moveSpeed;         // 移动速度
    public float fireDis;           // 开火距离
    public float fireOffsetTime;    // 开火间隔（秒）
    public int score;               // 击杀得分
}

/// <summary>整个怪物配置表的根结构，对应 MonsterConfig.json</summary>
[System.Serializable]
public class MonsterConfig
{
    public List<MonsterInfo> monsters;
}
