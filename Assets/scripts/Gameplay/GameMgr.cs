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

    /// <summary>每局开始（GameScene加载后由WaveManager调用）：重置局内状态并解冻时钟</summary>
    public void BeginRun()
    {
        inRun = true;
        runEnded = false;
        paused = false;
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
    }

    void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener(EEventType.PlayerDead, OnPlayerDead);
        EventCenter.Instance.RemoveEventListener(EEventType.GameWin, OnGameWin);
    }

    private void OnPlayerDead(object info) => EndRun(false);
    private void OnGameWin(object info) => EndRun(true);

    /// <summary>终局结算：冻结时钟→记录最高分→弹出对应面板（只结算一次）</summary>
    private void EndRun(bool win)
    {
        if (runEnded)
            return;
        runEnded = true;
        Time.timeScale = 0f;

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
