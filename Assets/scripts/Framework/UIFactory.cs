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

        /// <summary>输入框：白底+占位提示文字（Placeholder在输入后自动消失）</summary>
        public static InputField CreateInput(Transform parent, string placeholder, Vector2 size, Vector2 pos)
        {
            GameObject go = new GameObject($"Input_{placeholder}", typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.rectTransform.sizeDelta = size;
            img.rectTransform.anchoredPosition = pos;
            img.color = new Color(1f, 1f, 1f, 0.92f);

            InputField input = go.GetComponent<InputField>();
            input.targetGraphic = img;

            //占位提示（输入内容后自动隐藏——InputField内置行为）
            Text ph = CreateText(go.transform, placeholder, 24, new Color(0f, 0f, 0f, 0.4f));
            ph.rectTransform.sizeDelta = new Vector2(size.x - 20, size.y);
            ph.rectTransform.anchoredPosition = new Vector2(10, 0);
            ph.alignment = TextAnchor.MiddleLeft;
            ph.fontStyle = FontStyle.Italic;
            input.placeholder = ph;

            //实际输入文字
            GameObject tgo = new GameObject("InputText", typeof(Text));
            tgo.transform.SetParent(go.transform, false);
            Text t = tgo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 26;
            t.color = Color.black;
            t.alignment = TextAnchor.MiddleLeft;
            t.rectTransform.sizeDelta = new Vector2(size.x - 20, size.y);
            t.rectTransform.anchoredPosition = new Vector2(10, 0);
            t.supportRichText = false;
            input.textComponent = t;

            return input;
        }
    }
}
