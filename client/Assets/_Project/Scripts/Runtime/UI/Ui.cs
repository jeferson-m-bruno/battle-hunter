using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BattleHunter.Client.UI
{
    /// <summary>Fábrica de uGUI em código: canvas em retrato 1080×1920, painéis, textos e botões com fonte padrão.</summary>
    public static class Ui
    {
        public static readonly Color Panel = new(0.12f, 0.11f, 0.15f, 0.92f);
        public static readonly Color Accent = new(0.95f, 0.75f, 0.2f);
        public static readonly Color ButtonColor = new(0.25f, 0.23f, 0.32f);

        public static Font Font => _font != null ? _font : _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static Font _font;

        public static Canvas Canvas(string name = "Canvas")
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Object.DontDestroyOnLoad(es);
            }

            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform PanelRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color? color = null, Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin ?? Vector2.zero;
            rt.offsetMax = offsetMax ?? Vector2.zero;
            go.GetComponent<Image>().color = color ?? Panel;
            return rt;
        }

        public static Text Label(Transform parent, string name, string text, int size, TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax, Vector2? offsetMin = null, Vector2? offsetMax = null, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin ?? Vector2.zero;
            rt.offsetMax = offsetMax ?? Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color ?? Color.white;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string name, string caption, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick, Vector2? offsetMin = null, Vector2? offsetMax = null, int fontSize = 36, Color? color = null)
        {
            var rt = PanelRect(parent, name, anchorMin, anchorMax, color ?? ButtonColor, offsetMin, offsetMax);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            button.onClick.AddListener(onClick);
            Label(rt, "Label", caption, fontSize, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            return button;
        }

        public static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
