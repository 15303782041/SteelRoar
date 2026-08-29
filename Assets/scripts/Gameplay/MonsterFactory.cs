using GameFramework;
using UnityEngine;

/// <summary>
/// 怪物工厂：调用方只给配置名，不关心预制体来源、属性注入、对象池等实例化细节。
/// 新增怪物种类 = Json里加一段配置 + 场景/资源里放对应预制体，零代码改动。
/// </summary>
public class MonsterFactory
{
    private static MonsterConfig config;

    /// <summary>配置表懒加载：首次访问时从Resources读一次，之后全局复用</summary>
    public static MonsterConfig Config
    {
        get
        {
            if (config == null)
                config = JsonManager.Instance.LoadData<MonsterConfig>("MonsterConfig");
            return config;
        }
    }

    /// <summary>按名字查配置项，查不到返回null（调用方自行兜底）</summary>
    public static MonsterInfo GetInfo(string monsterName)
    {
        MonsterConfig c = Config;
        if (c == null || c.monsters == null) return null;
        return c.monsters.Find(m => m.monsterName == monsterName);
    }

    /// <summary>
    /// 按配置名生成怪物：预制体需位于 Resources/Prefabs/Game/ 下。
    /// 供 Day 7 波次系统调用；场景中手动摆放的怪物走 MonsterObj.Init 直接应用配置
    /// </summary>
    public static MonsterObj Create(string monsterName, Vector3 pos)
    {
        MonsterInfo info = GetInfo(monsterName);
        if (info == null)
        {
            Debug.LogWarning($"怪物配置不存在：{monsterName}");
            return null;
        }

        GameObject prefab = Resources.Load<GameObject>("Prefabs/Game/" + info.prefabName);
        if (prefab == null)
        {
            Debug.LogWarning($"怪物预制体不存在：Resources/Prefabs/Game/{info.prefabName}");
            return null;
        }

        GameObject obj = PoolManager.Instance.GetObj(prefab);
        obj.transform.position = pos;
        MonsterObj monster = obj.GetComponent<MonsterObj>();
        monster.Init(info);
        return monster;
    }
}
