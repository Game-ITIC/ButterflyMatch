#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Utils.DebugTools
{
    /// <summary>
    /// Code-built UI pieces of the cheat panel: rounded images, labels, buttons, rows, section titles and the
    /// two-tap confirm row. Every button runs its action through <see cref="Run"/> so a broken cheat shows a toast.
    /// </summary>
    internal sealed class CheatUi
    {
        public const float RowHeight = 104f;
        private const float ConfirmSeconds = 3f;

        public static readonly Color CardColor = new(0.1f, 0.09f, 0.16f, 0.98f);
        public static readonly Color ButtonColor = new(0.27f, 0.25f, 0.42f, 1f);
        public static readonly Color FieldColor = new(0.05f, 0.05f, 0.09f, 1f);
        public static readonly Color DangerColor = new(0.62f, 0.2f, 0.22f, 1f);
        public static readonly Color FloatingColor = new(0.78f, 0.2f, 0.45f, 0.8f);
        public static readonly Color TitleColor = new(1f, 0.83f, 0.35f, 1f);

        private readonly Sprite _rounded;
        private TextMeshProUGUI _armedLabel;
        private string _armedText;
        private float _armedUntil;

        public CheatUi()
        {
            _rounded = MakeRoundedSprite(32);
        }

        /// <summary>An armed confirm button falls back to its label after a few seconds.</summary>
        public void Tick()
        {
            if(_armedLabel != null && Time.unscaledTime > _armedUntil)
            {
                Disarm();
            }
        }

        public void Section(RectTransform content, string title)
        {
            var rect = NewRect("Section " + title, content);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 66f;
            var text = AddText(rect, title, 34f, TitleColor);
            text.alignment = TextAlignmentOptions.BottomLeft;
        }

        public void Row(RectTransform content, params (string label, Action action)[] buttons)
        {
            var row = NewRowRect(content);
            foreach(var (label, action) in buttons)
            {
                AddRowButton(row, label, ButtonColor, action);
            }
        }

        /// <summary>A destructive action: the first tap only asks for a second one within a few seconds.</summary>
        public void ConfirmRow(RectTransform content, string label, Action action)
        {
            var row = NewRowRect(content);
            TextMeshProUGUI text = null;
            text = AddRowButton(row, label, DangerColor, () =>
            {
                if(_armedLabel == text)
                {
                    Disarm();
                    action();
                    return;
                }

                Disarm();
                _armedLabel = text;
                _armedText = label;
                _armedUntil = Time.unscaledTime + ConfirmSeconds;
                text.text = "Точно? Нажми ещё раз";
            });
        }

        public void Disarm()
        {
            if(_armedLabel != null)
            {
                _armedLabel.text = _armedText;
            }

            _armedLabel = null;
        }

        public static RectTransform NewRowRect(RectTransform content)
        {
            var row = NewRect("Row", content);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        public TextMeshProUGUI AddRowButton(RectTransform row, string label, Color color, Action action)
        {
            var rect = NewRect("Button " + label, row);
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            element.preferredWidth = 0f;
            return AddButton(rect, label, color, action);
        }

        public TextMeshProUGUI AddButton(RectTransform rect, string label, Color color, Action action)
        {
            var image = AddImage(rect, color);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => Run(action));

            var text = AddText(rect, label, 34f, Color.white);
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = 34f;
            text.margin = new Vector4(8f, 4f, 8f, 4f);
            return text;
        }

        public Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = _rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        public static TextMeshProUGUI AddText(RectTransform parent, string value, float size, Color color)
        {
            var host = parent;
            if(parent.GetComponent<Graphic>() != null)
            {
                host = NewRect("Label", parent);
                Stretch(host);
            }

            var text = host.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Vector2 ClampToSafeArea(Vector2 position, RectTransform child, RectTransform parent)
        {
            var half = (parent.rect.size - child.rect.size) * 0.5f;
            return new Vector2(
                Mathf.Clamp(position.x, -Mathf.Max(0f, half.x), Mathf.Max(0f, half.x)),
                Mathf.Clamp(position.y, -Mathf.Max(0f, half.y), Mathf.Max(0f, half.y)));
        }

        // A broken cheat must tell the tester instead of failing silently.
        private static void Run(Action action)
        {
            try
            {
                action();
            }
            catch(Exception e)
            {
                UnityEngine.Debug.LogException(e);
                Toast.Show("Ошибка чита: " + e.Message);
            }
        }

        private static Sprite MakeRoundedSprite(int radius)
        {
            var size = radius * 2 + 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            var center = size * 0.5f;
            for(var y = 0; y < size; y++)
            {
                for(var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    var alpha = (byte)(Mathf.Clamp01(radius - distance + 0.5f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
    }
}
#endif
