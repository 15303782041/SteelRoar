using UnityEngine;

/// <summary>
/// 攻击状态：炮台瞄准玩家，按冷却开火。
/// 转移：玩家脱离开火距离→追击（侦测不到则巡逻）；残血→撤退
/// </summary>
public class AttackState : IState
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

        //玩家跑出开火距离：还能侦测到就追，跟丢就回巡逻
        if (!monster.PlayerInFireRange())
        {
            monster.ChangeState(monster.PlayerInDetectRange()
                ? monster.chaseState
                : monster.patrolState);
            return;
        }

        monster.AimAndTryFire();
    }

    public void Exit() { }
}
