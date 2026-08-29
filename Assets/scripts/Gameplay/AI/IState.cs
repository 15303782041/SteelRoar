/// <summary>
/// 怪物AI状态接口——状态机三件套：进入/每帧/退出。
/// 每个状态类只做两件事：执行本状态的行为 + 检查转移条件
/// </summary>
public interface IState
{
    /// <summary>进入状态时调用（owner=持有本状态的怪物）</summary>
    void Enter(MonsterObj monster);

    /// <summary>每帧执行：行为 + 转移条件判定</summary>
    void Update();

    /// <summary>离开状态时调用（清理用，当前无）</summary>
    void Exit();
}
