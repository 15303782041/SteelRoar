using UnityEngine;

/// <summary>
/// 巡逻状态：在巡逻点间游走。
/// 转移：玩家进入侦测半径→追击；自身残血→撤退
/// </summary>
public class PatrolState : IState
{
    private MonsterObj monster;

    public void Enter(MonsterObj monster)
    {
        this.monster = monster;
    }

    public void Update()
    {
        //最高优先级：残血先保命
        if (monster.NeedRetreat())
        {
            monster.ChangeState(monster.retreatState);
            return;
        }

        monster.PatrolMove();

        //发现玩家→追击
        if (monster.PlayerInDetectRange())
            monster.ChangeState(monster.chaseState);
    }

    public void Exit() { }
}
