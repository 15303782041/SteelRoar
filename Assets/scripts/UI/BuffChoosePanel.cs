using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 肉鸽三选一面板：波次清空后由WaveManager调起，全场冻结，玩家点选一张强化卡。
/// 面板与卡片全部运行时创建（原子化构造：组件随物体同生，避免旧Transform引用被替换）。
/// 抽卡规则：从未达叠层上限的Buff中抽3个不重复的
/// </summary>
public class BuffChoosePanel : SingletonAutoMono<BuffChoosePanel>
{
    public static bool IsOpen = false;

    private BuffInfo[] allBuffs;                 // Resources/Buffs 下的全部配置
    private readonly List<BuffInfo> choices = new List<BuffInfo>();
    private GameObject panelRoot;
    private readonly GameObject[] cardGo = new GameObject[3];
    private readonly Text[] cardName = new Text[3];
    private readonly Text[] cardDesc = new Text[3];
    private readonly Text[] cardStack = new Text[3];
    private PlayerObj player;

    public void Show()
    {
        //首次调用时加载全部Buff配置
        if (allBuffs == null || allBuffs.Length == 0)
            allBuffs = Resources.LoadAll<BuffInfo>("Buffs");

        player = FindObjectOfType<PlayerObj>();
        if (allBuffs == null || allBuffs.Length < 3 || player == null)
        {
            Debug.LogWarning("Buff配置不足3条或玩家缺失，跳过三选一");
            IsOpen = false;
            return;
        }

        BuildPanelOnce();
        RollChoices();
        RenderCards();
        panelRoot.SetActive(true);
        IsOpen = true;
    }

    /// <summary>键盘快捷选择：数字键1/2/3对应三张卡（鼠标异常时的永久兜底）</summary>
    void Update()
    {
        if (!IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) Pick(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Pick(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Pick(2);
    }

    /// <summary>点击卡片：应用Buff并关闭面板（WaveManager据此解冻继续下一波）</summary>
    public void Pick(int index)
    {
        if (!IsOpen || index < 0 || index >= choices.Count)
            return;
        player.AddBuff(choices[index]);
        Close();
    }

    public void Close()
    {
        IsOpen = false;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>面板骨架只建一次：暗化背景 + 标题 + 三张卡（卡片=色块按钮+三行文字）</summary>
    private void BuildPanelOnce()
    {
        if (panelRoot != null)
            return;

        //UGUI按钮依赖EventSystem响应点击：本工程教程UI是IMGUI体系，场景里没有它——运行时自动补建
        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        //原子化构造：Canvas+CanvasScaler+GraphicRaycaster（负责接收点击！）+背景Image与物体同生
        //缺少GraphicRaycaster时面板能渲染但按钮全部点不动——这是代码创建UI的最常见翻车点
        panelRoot = new GameObject("BuffChoosePanel", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        Canvas canvas = panelRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = panelRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Image bg = panelRoot.GetComponent<Image>();
        bg.rectTransform.sizeDelta = new Vector2(1920, 1080);
        bg.color = new Color(0f, 0f, 0f, 0.75f);          // 全屏暗化

        Text title = CreateText(panelRoot.transform, "选 择 一 项 强 化", 60, Color.white);
        title.rectTransform.sizeDelta = new Vector2(900, 80);
        title.rectTransform.anchoredPosition = new Vector2(0, 330);

        //三张卡：左中右排布
        float[] cardX = { -430f, 0f, 430f };
        for (int i = 0; i < 3; i++)
        {
            int index = i;   // 闭包捕获：按钮回调要用各自的编号
            GameObject card = new GameObject($"Card{i}", typeof(Image), typeof(Button));
            card.transform.SetParent(panelRoot.transform, false);
            Image cardImg = card.GetComponent<Image>();
            cardImg.rectTransform.sizeDelta = new Vector2(380, 460);
            cardImg.rectTransform.anchoredPosition = new Vector2(cardX[i], 0);
            cardImg.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

            Button btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() => Pick(index));

            //卡片标题
            Text name = CreateText(card.transform, "", 38, new Color(0.95f, 0.78f, 0.3f));
            name.rectTransform.sizeDelta = new Vector2(340, 60);
            name.rectTransform.anchoredPosition = new Vector2(0, 150);
            cardName[i] = name;

            //效果描述（自动换行）
            Text desc = CreateText(card.transform, "", 26, Color.white);
            desc.rectTransform.sizeDelta = new Vector2(330, 180);
            desc.rectTransform.anchoredPosition = new Vector2(0, 10);
            cardDesc[i] = desc;

            //叠层数
            Text stack = CreateText(card.transform, "", 24, new Color(0.6f, 0.85f, 1f));
            stack.rectTransform.sizeDelta = new Vector2(340, 40);
            stack.rectTransform.anchoredPosition = new Vector2(0, -170);
            cardStack[i] = stack;

            cardGo[i] = card;
        }

        panelRoot.SetActive(false);
    }

    /// <summary>从"未达上限"的Buff池中抽3个不重复的</summary>
    private void RollChoices()
    {
        choices.Clear();

        List<BuffInfo> available = new List<BuffInfo>();
        foreach (BuffInfo b in allBuffs)
            if (player.GetStack(b.type) < b.stackMax)
                available.Add(b);

        int draw = Mathf.Min(3, available.Count);
        for (int i = 0; i < draw; i++)
        {
            int idx = Random.Range(0, available.Count);
            choices.Add(available[idx]);
            available.RemoveAt(idx);
        }
    }

    /// <summary>把抽中的Buff渲染到三张卡上（先清空旧监听，防止回调叠加）</summary>
    private void RenderCards()
    {
        for (int i = 0; i < 3; i++)
        {
            bool has = i < choices.Count;
            cardGo[i].SetActive(has);
            if (!has) continue;

            BuffInfo info = choices[i];
            cardName[i].text = info.buffName;
            cardDesc[i].text = info.description;
            cardStack[i].text = $"当前 {player.GetStack(info.type)}/{info.stackMax} 层";

            Button btn = cardGo[i].GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            int index = i;
            btn.onClick.AddListener(() => Pick(index));
        }
    }

    /// <summary>创建文本（内置字体支持中文动态回退；关闭射线检测，避免文字挡住卡片按钮的点击）</summary>
    private Text CreateText(Transform parent, string content, int size, Color color)
    {
        GameObject go = new GameObject("Text", typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.text = content;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
}
