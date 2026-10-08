using BattleHunter.Client.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BattleHunter.Client.UI
{
    /// <summary>Fábrica de uGUI em código: canvas em retrato 1080×1920, painéis, textos, botões, campos e listas roláveis.</summary>
    public static class Ui
    {
        // Com moldura (SpriteCatalog.Panel) a cor vira tint: moldura na cor cheia, centro a ~40% dela.
        public static readonly Color Panel = new(0.30f, 0.28f, 0.36f, 0.95f);
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

        /// <summary>Painel; com <paramref name="framed"/> usa a moldura 9-slice do pacote de arte (se existir) tingida pela cor.</summary>
        public static RectTransform PanelRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color? color = null, Vector2? offsetMin = null, Vector2? offsetMax = null, bool framed = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin ?? Vector2.zero;
            rt.offsetMax = offsetMax ?? Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color ?? Panel;
            if (framed)
                Frame(image, SpriteCatalog.Panel());
            return rt;
        }

        /// <summary>Aplica uma moldura 9-slice; sem sprite, a Image continua na cor chapada.</summary>
        public static void Frame(Image image, Sprite frame)
        {
            if (frame == null)
                return;
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
        }

        /// <summary>Ícone quadrado de <paramref name="size"/> px encostado à esquerda (ou centrado no topo) do pai.</summary>
        public static Image Icon(Transform parent, Sprite sprite, float size = 48f, bool top = false)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            if (top)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -8f);
            }
            else
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(12f, 0f);
            }
            rt.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
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

        /// <summary>Botão com moldura do pacote; <paramref name="icon"/> opcional à esquerda (<paramref name="iconTop"/>: centrado no topo, texto abaixo).</summary>
        public static Button Button(Transform parent, string name, string caption, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick, Vector2? offsetMin = null, Vector2? offsetMax = null, int fontSize = 36, Color? color = null, Sprite icon = null, bool iconTop = false, Sprite frame = null)
        {
            var rt = PanelRect(parent, name, anchorMin, anchorMax, color ?? ButtonColor, offsetMin, offsetMax, framed: false);
            Frame(rt.GetComponent<Image>(), frame != null ? frame : SpriteCatalog.Button());
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            button.onClick.AddListener(onClick);
            var labelMin = Vector2.zero;
            var labelMax = Vector2.zero;
            if (icon != null)
            {
                var size = iconTop ? 40f : 48f;
                Icon(rt, icon, size, iconTop);
                if (iconTop)
                    labelMax = new Vector2(0f, -(size + 10f));
                else
                    labelMin = new Vector2(size + 20f, 0f);
            }
            var label = Label(rt, "Label", caption, fontSize, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, labelMin, labelMax);
            label.supportRichText = true;
            return button;
        }

        public static InputField Field(Transform parent, string name, string value, Vector2 anchorMin, Vector2 anchorMax, int fontSize = 28, int maxLength = 16)
        {
            var rt = PanelRect(parent, name, anchorMin, anchorMax, new Color(0.2f, 0.19f, 0.25f), framed: false);
            Frame(rt.GetComponent<Image>(), SpriteCatalog.Button());
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
            var viewport = PanelRect(parent, name, anchorMin, anchorMax, new Color(0, 0, 0, 0.25f), framed: false);
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
        public static Button ListItem(RectTransform content, string name, string text, UnityEngine.Events.UnityAction onClick, string action = null, UnityEngine.Events.UnityAction onAction = null, Color? color = null, int fontSize = 26, Sprite icon = null)
        {
            var height = content.GetComponent<LayoutItemHeight>()?.height ?? 110f;
            var rt = PanelRect(content, name, Vector2.zero, Vector2.one, color ?? ButtonColor, framed: false);
            Frame(rt.GetComponent<Image>(), SpriteCatalog.Button());
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            if (onClick != null)
                button.onClick.AddListener(onClick);
            var textLeft = 16f;
            if (icon != null)
            {
                var size = Mathf.Min(48f, height - 24f);
                Icon(rt, icon, size);
                textLeft = size + 24f;
            }
            var label = Label(rt, "Label", text, fontSize, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(action == null ? 1f : 0.72f, 1f), new Vector2(textLeft, 0), new Vector2(-8, 0));
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
