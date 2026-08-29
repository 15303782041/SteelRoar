using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameFramework
{
    /// <summary>
    /// 运行时UGUI小工厂：统一字体（中文动态回退）与射线设置（文字不挡点击）。
    /// 所有"代码创建的UI"都从这里出，避免各面板重复造轮子
    /// </summary>
    public static class UIFactory
    {
        public static Text CreateText(Transform parent, string content, int size, Color color)
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
            t.raycastTarget = false;          // 文字不挡点击
            return t;
        }

        public static Button CreateButton(Transform parent, string content, Vector2 size, Vector2 pos, Color color, UnityAction onClick)
        {
            GameObject go = new GameObject($"Btn_{content}", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.rectTransform.sizeDelta = size;
            img.rectTransform.anchoredPosition = pos;
            img.color = color;

            Button btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onClick);

            Text label = CreateText(go.transform, content, 30, Color.white);
            label.rectTransform.sizeDelta = size;
            return btn;
        }
    }
}
