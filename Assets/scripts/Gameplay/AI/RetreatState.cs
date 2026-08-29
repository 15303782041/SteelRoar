using UnityEngine;

/// <summary>
/// 撤退状态：背向玩家拉开距离，持续3秒或甩掉玩家后回巡逻
/// </summary>
public class RetreatState : IState
{
    private MonsterObj monster;
    private float retreatTime = 3;
    private float nowTime = 0;

    public void Enter(MonsterObj monster)
    {
        this.monster = monster;
        nowTime = 0;
    }

    public void Update()
    {
        monster.MoveAwayFromPlayer();

        nowTime += Time.deltaTime;
        //撤退时间到 或 玩家已被甩出侦测范围 → 回巡逻
        if (nowTime >= retreatTime || !monster.PlayerInDetectRange())
            monster.ChangeState(monster.patrolState);
    }

    public void Exit() { }
}
