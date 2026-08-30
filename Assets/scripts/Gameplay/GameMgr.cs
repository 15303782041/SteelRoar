using GameFramework;
using UnityEngine;

/// <summary>
/// 游戏流程管理器：统一裁决游戏状态与timeScale。
/// 职责：①监听PlayerDead/GameWin做终局结算（冻结→记最高分→弹对应面板）；
///      ②ESC暂停/恢复；③每局开始时重置状态并解冻时钟。
/// 全工程只有GameMgr和Buff选择流程允许改timeScale——修掉了教程遗留的
/// "失败面板重开时timeScale停留在0导致全场冻结"的软锁
/// </summary>
public class GameMgr : SingletonAutoMono<GameMgr>
{
    private bool runEnded = false;      // 本局是否已结算（防重复触发）
    private bool paused = false;
    private bool inRun = false;         // 是否在战斗局内（主菜单里ESC不响应暂停）
    private bool myReady = false;       // 联机重开：我方已在结算面板点"准备"
    private bool peerReady = false;     // 联机重开：对方已点"准备"
    private bool rematchHandled = false; // 重开是否已执行（防双方就绪瞬间重复重载）

    /// <summary>每局开始（GameScene加载后由WaveManager调用）：重置局内状态并解冻时钟</summary>
    public void BeginRun()
    {
        inRun = true;
        runEnded = false;
        paused = false;
        myReady = false;
        peerReady = false;
        rematchHandled = false;
        Time.timeScale = 1f;
        SaveManager.Instance.Load();     // 开局读档，供本局结算对比最高分
    }

    /// <summary>返回主菜单：先解冻时钟、收起悬浮面板并退出战斗状态，再异步切换场景</summary>
    public void BackToBeginScene()
    {
        inRun = false;
        Time.timeScale = 1f;
        PausePanel.Instance.HideMe();
        BuffChoosePanel.Instance.Close();
        SceneMgr.Instance.LoadScene("BeginScene", null);
    }

    void OnEnable()
    {
        EventCenter.Instance.AddEventListener(EEventType.PlayerDead, OnPlayerDead);
        EventCenter.Instance.AddEventListener(EEventType.GameWin, OnGameWin);
        //联机终局：对端宣告"我赢了"（对端先死，把自己死亡的GameOver发过来）。
        //Subscribe是字典覆盖式、本对象跨场景存活，无需解绑
        NetCenter.Instance.Subscribe((ushort)MsgId.GameOver, OnNetGameOver);
        //联机重开（准备确认制）：对端在结算面板点了"准备"
        NetCenter.Instance.Subscribe((ushort)MsgId.RematchReady, OnNetRematchReady);
    }

    void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener(EEventType.PlayerDead, OnPlayerDead);
        EventCenter.Instance.RemoveEventListener(EEventType.GameWin, OnGameWin);
    }

    private void OnPlayerDead(object info) => EndRun(false);
    private void OnGameWin(object info) => EndRun(true);

    /// <summary>
    /// 联机：收到对端死亡通知→本机是胜者。先让对方影子爆炸消失，再走统一结算
    /// （EndRun的runEnded守卫保证与"本机也恰好死了"的极端时序下只结算一次）
    /// </summary>
    private void OnNetGameOver(NetMsg msg)
    {
        if (!inRun)
            return;                          // 已回主菜单后的错序包不结算
        NetCenter.Instance.ExplodeRemote();
        EndRun(true);
    }

    /// <summary>联机重开（准备确认制）：双方都在结算面板点过"准备"才一起进新一局——
    /// 任一方单方面点重开就把对方拽进新局，输的人可能连结算都没看清</summary>
    public void RequestRematchReady()
    {
        if (!inRun || myReady)
            return;
        myReady = true;
        NetCenter.Instance.Send((ushort)MsgId.RematchReady, new RematchPayload());
        PvPResultPanel.Instance.SetMyReady();
        TryRematch();
    }

    private void OnNetRematchReady(NetMsg msg)
    {
        if (!inRun)
            return;                          // 已回主菜单后的错序包：不更新面板不重开
        peerReady = true;
        PvPResultPanel.Instance.SetPeerReady();
        TryRematch();
    }

    private void TryRematch()
    {
        if (!inRun || !myReady || !peerReady || rematchHandled)
            return;
        DoRematch();
    }

    /// <summary>
    /// 双方同步重开：重载GameScene，新影子由场景加载完成回调重建。
    /// 重载后WaveManager.Start会再次调用BeginRun（重置就绪标记、解冻时钟）
    /// </summary>
    private void DoRematch()
    {
        if (rematchHandled)
            return;
        rematchHandled = true;

        Time.timeScale = 1f;
        PvPResultPanel.Instance.Hide();
        SceneMgr.Instance.LoadScene("GameScene", () =>
        {
            NetCenter.Instance.SpawnRemoteTank();
        });
    }

    /// <summary>终局结算：冻结时钟→记录最高分→弹出对应面板（只结算一次）。
    /// 联机走专用UGUI结算面板（准备确认制重开）；WinPanel/LossPanel带排行榜输入，是单机流程</summary>
    private void EndRun(bool win)
    {
        if (runEnded)
            return;
        runEnded = true;
        Time.timeScale = 0f;

        if (NetCenter.Instance.Networking)
        {
            //联机：PvP分数不进单机排行榜（击杀计分未接入PvP），只弹胜负+准备面板
            PvPResultPanel.Instance.Show(win);
            return;
        }

        SaveManager.Instance.RecordScore(GamePanel.Instance.nowScore);

        if (win)
            WinPanel.Instance.ShowMe();
        else
            LossPanel.Instance.ShowMe();
    }

    void Update()
    {
        //只在战斗局内响应ESC；终局后ESC不再响应（防死亡/胜利后误触暂停）
        if (!inRun || runEnded)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    /// <summary>暂停/恢复切换（ESC和暂停面板的"继续游戏"都走这里）</summary>
    public void TogglePause()
    {
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
        if (paused)
            PausePanel.Instance.ShowMe();
        else
            PausePanel.Instance.HideMe();
    }
}
