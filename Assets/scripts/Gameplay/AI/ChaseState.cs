using UnityEngine;

/// <summary>
/// 追击状态：朝玩家移动。
/// 转移：进入开火距离→攻击；玩家脱离侦测半径→巡逻；残血→撤退
/// </summary>
public class ChaseState : IState
{
    private MonsterObj monster;

    public void Enter(MonsterObj monster)
    {
        this.monster = monster;
    }

    public void Update()
    {
        if (monster.NeedRetreat())
        {
            monster.ChangeState(monster.retreatState);
            return;
        }

        //贴近了→攻击
        if (monster.PlayerInFireRange())
        {
            monster.ChangeState(monster.attackState);
            return;
        }

        //跟丢了→巡逻
        if (!monster.PlayerInDetectRange())
        {
            monster.ChangeState(monster.patrolState);
            return;
        }

        monster.ChaseMove();
    }

    public void Exit() { }
}
