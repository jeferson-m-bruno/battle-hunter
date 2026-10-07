using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BattleHunter.Client.UI
{
    /// <summary>Fábrica de uGUI em código: canvas em retrato 1080×1920, painéis, textos, botões, campos e listas roláveis.</summary>
    public static class Ui
    {
        public static readonly Color Panel = new(0.12f, 0.11f, 0.15f, 0.92f);
        public static readonly Color Accent = new(0.95f, 0.75f, 0.2f);
        public static readonly Color ButtonColor = new(0.25f, 0.23f, 0.32f);
        public static readonly Color Background = new(0.08f, 0.07f, 0.10f);

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
            var label = Label(rt, "Label", caption, fontSize, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            label.supportRichText = true;
            return button;
        }

        public static InputField Field(Transform parent, string name, string value, Vector2 anchorMin, Vector2 anchorMax, int fontSize = 28, int maxLength = 16)
        {
            var rt = PanelRect(parent, name, anchorMin, anchorMax, new Color(0.2f, 0.19f, 0.25f));
            var field = rt.gameObject.AddComponent<InputField>();
            var text = Label(rt, "Text", value, fontSize, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-16, 0));
            text.raycastTarget = true;
            text.supportRichText = false;
            field.textComponent = text;
            field.characterLimit = maxLength;
            field.text = value;
            return field;
        }

        /// <summary>Lista rolável vertical; devolve o conteúdo onde os itens entram (altura preferida por item).</summary>
        public static RectTransform ScrollList(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float itemHeight = 110f, float spacing = 8f)
        {
            var viewport = PanelRect(parent, name, anchorMin, anchorMax, new Color(0, 0, 0, 0.25f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();

            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = new Vector2(8, 0);
            content.offsetMax = new Vector2(-8, 0);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.padding = new RectOffset(0, 0, 8, 8);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            content.gameObject.AddComponent<LayoutItemHeight>().height = itemHeight;
            return content;
        }

        /// <summary>Item de lista: botão de altura fixa com texto à esquerda e, opcionalmente, um botão de ação à direita.</summary>
        public static Button ListItem(RectTransform content, string name, string text, UnityEngine.Events.UnityAction onClick, string action = null, UnityEngine.Events.UnityAction onAction = null, Color? color = null, int fontSize = 26)
        {
            var height = content.GetComponent<LayoutItemHeight>()?.height ?? 110f;
            var rt = PanelRect(content, name, Vector2.zero, Vector2.one, color ?? ButtonColor);
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            if (onClick != null)
                button.onClick.AddListener(onClick);
            var label = Label(rt, "Label", text, fontSize, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(action == null ? 1f : 0.72f, 1f), new Vector2(16, 0), new Vector2(-8, 0));
            label.supportRichText = true;
            if (action != null)
                Button(rt, "Action", action, new Vector2(0.74f, 0.15f), new Vector2(0.98f, 0.85f), onAction ?? (() => { }), fontSize: 24, color: new Color(0.35f, 0.3f, 0.45f));
            return button;
        }

        public static void Clear(Transform parent)
        {
            foreach (Transform child in parent)
                Object.Destroy(child.gameObject);
        }

        /// <summary>Guarda a altura padrão dos itens de uma lista rolável.</summary>
        public sealed class LayoutItemHeight : MonoBehaviour
        {
            public float height = 110f;
        }
    }
}
