using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

/// <summary>
/// 单波次的刷怪配置，对应 WaveConfig.json 中的一段
/// </summary>
[System.Serializable]
public class WaveInfo
{
    public int waveIndex;               // 第几波
    public List<string> monsterNames;   // 本波怪物类型池（每次生成随机抽取一种）
    public int monsterCount;            // 本波怪物总数
    public int maxAlive = 5;            // 场上同时存在的存活上限
    public float spawnInterval = 1.5f;  // 生成间隔（秒）
    public bool isBossWave;             // Boss波标记（Day 8使用）
}

/// <summary>整个波次配置表的根结构，对应 WaveConfig.json</summary>
[System.Serializable]
public class WaveConfig
{
    public List<WaveInfo> waves;
}

/// <summary>
/// 波次管理器：按配置逐波刷怪，本波全灭后进入下一波，全部清空则广播胜利。
///
/// 设计说明：本类需要Inspector拖入出生点/巡逻点/玩家引用（预制体无法引用场景对象），
/// 所以不用SingletonAutoMono自动创建，而是作为普通组件挂载在场景中的WaveManager物体上。
/// 刷怪节奏：总数达到monsterCount为止；期间场上存活数不超过maxAlive；全灭才开下一波。
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("怪物出生点")]
    public Transform[] spawnPoints;
    [Header("怪物巡逻点（生成时注入怪物）")]
    public Transform[] patrolPoints;
    [Header("玩家（生成时注入为瞄准目标）")]
    public Transform player;

    private List<WaveInfo> waves;
    private WaveInfo nowWave;
    private int aliveCount = 0;     // 场上存活怪物数（MonsterDead事件递减）
    private int spawnCount = 0;     // 本波已生成数量

    //事件委托成员变量：存住引用才能解绑（lambda匿名函数无法解绑）
    private System.Action<object> onMonsterDead;

    void Start()
    {
        //通知流程管理器"本局开始"（顺带完成GameMgr单例创建、读档、解冻时钟）
        GameMgr.Instance.BeginRun();

        //联机模式（V1）：纯PvP对战，不启用单机波次（怪物/Boss为单机内容）
        if (NetCenter.Instance.Networking)
        {
            Debug.Log("[Wave] 联机模式：波次系统关闭（V1对战不含怪物）");
            CleanupSoloOnlyObjects();
            SeparateLanSpawn();
            GrantLanInitialWeapon();
            enabled = false;
            return;
        }

        WaveConfig config = JsonManager.Instance.LoadData<WaveConfig>("WaveConfig");
        if (config == null || config.waves == null || config.waves.Count == 0)
        {
            Debug.LogWarning("WaveConfig.json加载失败或为空，波次系统未启动");
            return;
        }
        waves = config.waves;

        onMonsterDead = (info) => aliveCount--;
        EventCenter.Instance.AddEventListener(EEventType.MonsterDead, onMonsterDead);

        StartCoroutine(RunWaves());
    }

    /// <summary>波次主循环：逐波执行 刷怪→等全灭→广播波次清除</summary>
    private IEnumerator RunWaves()
    {
        //开局缓冲，让玩家准备
        yield return new WaitForSeconds(1.5f);

        foreach (WaveInfo wave in waves)
        {
            nowWave = wave;
            EventCenter.Instance.EventTrigger(EEventType.WaveStart, wave.waveIndex);

            if (wave.isBossWave)
            {
                //Boss波：只刷一只Boss，等待玩家击杀
                SpawnBoss();
            }
            else
            {
                //阶段一：持续刷怪，直到本波总数刷满；场上存活数随时不超过上限
                spawnCount = 0;
                while (spawnCount < wave.monsterCount)
                {
                    if (aliveCount < wave.maxAlive)
                    {
                        SpawnOne();
                        spawnCount++;
                        yield return new WaitForSeconds(wave.spawnInterval);
                    }
                    else
                    {
                        //场上满了，等一帧再查
                        yield return null;
                    }
                }
            }

            //阶段二：等本波怪物全部被消灭
            while (aliveCount > 0)
                yield return null;

            EventCenter.Instance.EventTrigger(EEventType.WaveClear, wave.waveIndex);
            //波间喘息
            yield return new WaitForSeconds(2f);

            //肉鸽三选一：非最终波结束后弹出，选择期间冻结战场，选完解冻进下一波
            if (nowWave != waves[waves.Count - 1])
            {
                Time.timeScale = 0f;
                BuffChoosePanel.Instance.Show();
                yield return new WaitUntil(() => !BuffChoosePanel.IsOpen);
                Time.timeScale = 1f;
            }
        }

        //所有波次清空 → 广播胜利
        EventCenter.Instance.EventTrigger(EEventType.GameWin, null);
    }

    /// <summary>
    /// 联机清场：移除单机专属的场景物体——
    /// ①场景摆的Monster1（带完整AI，PvP里会主动攻击双方，看起来像凭空冒出的"NPC坦克"）；
    /// ②MonsterTower炮塔（同为单机敌人）；③EndPoint终点（单机胜利条件，联机压过会误弹单机胜利面板）。
    /// 武器/道具奖励箱保留（联机的地图资源）。影子同样用Monster1模型，靠RemoteTank组件区分防误删
    /// </summary>
    private void CleanupSoloOnlyObjects()
    {
        foreach (MonsterObj m in FindObjectsByType<MonsterObj>(FindObjectsSortMode.None))
            if (m.GetComponent<RemoteTank>() == null)
                Destroy(m.gameObject);

        foreach (MonsterTower tower in FindObjectsByType<MonsterTower>(FindObjectsSortMode.None))
            Destroy(tower.gameObject);

        foreach (EndPoint ep in FindObjectsByType<EndPoint>(FindObjectsSortMode.None))
            Destroy(ep.gameObject);
    }

    /// <summary>
    /// 联机开局发放基础武器：玩家出生不带武器，单机靠出生点旁的武器奖励箱起步——
    /// 联机出生点离奖励箱远，没武器就打不了箱子、拿不到奖励，输出手段直接死锁。
    /// 武器引用从场景WeaponReward的weaponObj[0]取（基础炮）：两端拿到同一把枪，保证公平
    /// </summary>
    private void GrantLanInitialWeapon()
    {
        if (player == null)
            return;

        WeaponReward reward = FindFirstObjectByType<WeaponReward>();
        if (reward == null || reward.weaponObj == null || reward.weaponObj.Length == 0)
        {
            Debug.LogWarning("[Net] 场景无WeaponReward（或其武器列表为空），联机初始武器发放失败");
            return;
        }
        player.GetComponent<PlayerObj>().ChangeWeapon(reward.weaponObj[0]);
        Debug.Log("[Net] 联机初始武器已发放");
    }

    /// <summary>
    /// 联机出生点分离：双方加载同一份场景，玩家会出生在同一点，彼此的影子同步后也被
    /// 拉向同一个位置——两台坦克叠在一起时，贴脸出膛的子弹会互相打出"幻影伤害"
    /// （打中叠着的影子=给对方发幻影Damage；对方的表现弹出生就插在我方碰撞体里=我方莫名掉血）。
    /// 规则确定性：主机固定用第一个出生点、客机用最后一个，两端无需协商
    /// </summary>
    private void SeparateLanSpawn()
    {
        if (player == null)
            return;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform mine = NetCenter.Instance.IsHost
                ? spawnPoints[0]
                : spawnPoints[spawnPoints.Length - 1];
            player.position = mine.position;
            player.rotation = mine.rotation;
        }
        else if (!NetCenter.Instance.IsHost)
        {
            //没有配置出生点的兜底：客机硬偏移8米，至少不叠在一起
            player.position += Vector3.right * 8f;
        }
    }

    /// <summary>按配置生成一只怪物：随机出生点+随机类型，并注入运行时依赖</summary>
    private void SpawnOne()
    {
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        string monsterName = nowWave.monsterNames[Random.Range(0, nowWave.monsterNames.Count)];

        MonsterObj monster = MonsterFactory.Create(monsterName, point.position);
        if (monster == null)
            return;

        //预制体无法引用场景对象，运行时依赖由波次管理器注入
        monster.lookAtTarget = player;
        monster.randomPos = patrolPoints;

        aliveCount++;
    }

    /// <summary>Boss波：从出生点刷出Boss并注入玩家</summary>
    private void SpawnBoss()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Game/Object/Boss1");
        if (prefab == null)
        {
            Debug.LogWarning("Boss预制体不存在：Resources/Prefabs/Game/Object/Boss1");
            return;
        }

        Transform point = spawnPoints.Length > 0
            ? spawnPoints[Random.Range(0, spawnPoints.Length)]
            : transform;

        GameObject obj = PoolManager.Instance.GetObj(prefab);
        obj.transform.position = point.position;

        BossObj boss = obj.GetComponent<BossObj>();
        boss.Init(player);

        aliveCount++;
    }

    /// <summary>
    /// Boss召唤小怪专用：围绕center生成count只并计入存活数，返回实际生成数量。
    /// 计入存活数的原因：召唤怪被击杀同样会触发MonsterDead递减，
    /// 只减不增会让计数变负、导致Boss还活着就被判定"本波清空"
    /// </summary>
    public int SpawnSummons(string name, int count, Vector3 center)
    {
        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            //围绕Boss随机散开，避免小怪叠在一起
            Vector3 pos = center + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
            MonsterObj m = MonsterFactory.Create(name, pos);
            if (m == null)
                continue;

            m.lookAtTarget = player;
            m.randomPos = patrolPoints;
            aliveCount++;
            spawned++;
        }
        return spawned;
    }
}
