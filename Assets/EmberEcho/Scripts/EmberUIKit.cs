using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmberEcho
{
    public static class EmberUI
    {
        public static readonly Color Ink = new Color32(11, 13, 12, 255);
        public static readonly Color Deep = new Color32(17, 20, 18, 255);
        public static readonly Color Surface = new Color32(27, 31, 27, 255);
        public static readonly Color PanelLight = new Color32(38, 42, 36, 255);
        public static readonly Color Divider = new Color32(71, 73, 62, 255);
        public static readonly Color Muted = new Color32(159, 155, 139, 255);
        public static readonly Color Parchment = new Color32(235, 225, 199, 255);
        public static readonly Color Ember = new Color32(226, 139, 65, 255);
        public static readonly Color Gold = new Color32(213, 181, 105, 255);
        public static readonly Color Moss = new Color32(106, 137, 94, 255);
        public static readonly Color Aqua = new Color32(94, 151, 157, 255);
        public static readonly Color Blood = new Color32(176, 70, 55, 255);
        public static readonly Color PaleBlue = new Color32(146, 182, 193, 255);

        private static Font _font;
        private static Sprite _rounded;
        private static Sprite _circle;
        private static Sprite _ring;
        private static Sprite _glow;
        private static Sprite _vignette;
        private static Sprite _grain;
        private static Sprite _diamond;
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        public static Font UiFont
        {
            get
            {
                if (_font != null) return _font;
                var installed = Font.GetOSInstalledFontNames();
                var preferred = new[]
                {
                    "Microsoft YaHei UI",
                    "Microsoft YaHei",
                    "Noto Sans CJK SC",
                    "Source Han Sans SC",
                    "PingFang SC",
                    "SimHei"
                };

                foreach (var fontName in preferred)
                {
                    if (Array.IndexOf(installed, fontName) < 0) continue;
                    _font = Font.CreateDynamicFontFromOSFont(fontName, 24);
                    if (_font != null)
                    {
                        _font.name = fontName;
                        return _font;
                    }
                }

                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static Sprite Art(string key)
        {
            return GetSprite("EmberArt/" + key, false);
        }

        public static Sprite PixelArt(string key)
        {
            return GetSprite("EmberArt/" + key, true);
        }

        public static Sprite Rounded
        {
            get
            {
                if (_rounded == null) _rounded = BuildRoundedSprite();
                return _rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null) _circle = BuildCircleSprite(false);
                return _circle;
            }
        }

        public static Sprite Ring
        {
            get
            {
                if (_ring == null) _ring = BuildCircleSprite(true);
                return _ring;
            }
        }

        public static Sprite Glow
        {
            get
            {
                if (_glow == null) _glow = BuildGlowSprite();
                return _glow;
            }
        }

        public static Sprite Vignette
        {
            get
            {
                if (_vignette == null) _vignette = BuildVignetteSprite();
                return _vignette;
            }
        }

        public static Sprite Grain
        {
            get
            {
                if (_grain == null) _grain = BuildGrainSprite();
                return _grain;
            }
        }

        public static Sprite Diamond
        {
            get
            {
                if (_diamond == null) _diamond = BuildDiamondSprite();
                return _diamond;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(string name, Transform parent, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static RectTransform Center(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static Image Panel(Transform parent, Vector2 position, Vector2 size, Color color, float alpha = 1f)
        {
            var rect = Center("Panel", parent, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = WithAlpha(color, alpha);
            return image;
        }

        public static RawImage Raw(Transform parent, string textureKey, Vector2 position, Vector2 size, Vector2 uvScale, Color color)
        {
            var rect = Center("Raw", parent, position, size);
            var image = rect.gameObject.AddComponent<RawImage>();
            var texture = GetTexture(textureKey);
            texture.wrapMode = TextureWrapMode.Repeat;
            image.texture = texture;
            image.uvRect = new Rect(0f, 0f, uvScale.x, uvScale.y);
            image.color = color;
            return image;
        }

        public static Image Picture(Transform parent, string key, Vector2 position, Vector2 size, Color color, bool pixel = false)
        {
            var rect = Center("Picture", parent, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = pixel ? PixelArt(key) : Art(key);
            image.preserveAspect = true;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Image Shape(Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color)
        {
            var rect = Center("Shape", parent, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(
            Transform parent,
            string text,
            int size,
            Color color,
            TextAnchor anchor,
            Vector2 position,
            Vector2 dimensions,
            FontStyle style = FontStyle.Normal)
        {
            var rect = Center("Label", parent, position, dimensions);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = UiFont;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = anchor;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            label.supportRichText = true;
            label.lineSpacing = 1.08f;
            return label;
        }

        public static Text LabelStretch(
            Transform parent,
            string text,
            int size,
            Color color,
            TextAnchor anchor,
            float left,
            float right,
            float top,
            float bottom,
            FontStyle style = FontStyle.Normal)
        {
            var rect = Stretch("Label", parent, left, right, top, bottom);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = UiFont;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = anchor;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            label.supportRichText = true;
            label.lineSpacing = 1.08f;
            return label;
        }

        public static Button Button(
            Transform parent,
            string text,
            Vector2 position,
            Vector2 size,
            Color fill,
            Action onClick,
            int fontSize = 22,
            Color? textColor = null)
        {
            var rect = Center("Button", parent, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = fill;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.12f, 1.12f, 1.08f, 1f),
                pressedColor = new Color(0.82f, 0.82f, 0.8f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.52f, 0.52f, 0.5f, 0.7f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var label = Label(
                rect,
                text,
                fontSize,
                textColor ?? Parchment,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                size - new Vector2(18f, 8f),
                FontStyle.Bold);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return button;
        }

        public static Image Bar(Transform parent, Vector2 position, Vector2 size, Color background, Color fill, float ratio)
        {
            var bg = Panel(parent, position, size, background);
            bg.name = "BarBackground";
            var fillRect = Stretch("BarFill", bg.transform, 3f, 3f, 3f, 3f);
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            fillRect.offsetMax = new Vector2(0f, -3f);
            fillRect.offsetMin = new Vector2(3f, 3f);
            var fillImage = fillRect.gameObject.AddComponent<Image>();
            fillImage.sprite = Rounded;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = fill;
            fillImage.raycastTarget = false;
            return fillImage;
        }

        public static Image Line(Transform parent, Vector2 from, Vector2 to, float width, Color color)
        {
            var midpoint = (from + to) * 0.5f;
            var delta = to - from;
            var rect = Center("Line", parent, midpoint, new Vector2(delta.magnitude, width));
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color CardColor(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Attack:
                    return new Color32(154, 68, 50, 255);
                case CardKind.Skill:
                    return new Color32(47, 100, 111, 255);
                case CardKind.Power:
                    return new Color32(77, 105, 68, 255);
                default:
                    return PanelLight;
            }
        }

        public static Color RarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Starter:
                    return Muted;
                case CardRarity.Common:
                    return Parchment;
                case CardRarity.Uncommon:
                    return Aqua;
                case CardRarity.Rare:
                    return Gold;
                default:
                    return Parchment;
            }
        }

        public static float EaseOutCubic(float value)
        {
            var inverse = 1f - Mathf.Clamp01(value);
            return 1f - inverse * inverse * inverse;
        }

        private static Sprite GetSprite(string path, bool point)
        {
            var key = path + (point ? "#point" : "#smooth");
            if (Sprites.TryGetValue(key, out var cached)) return cached;

            var texture = GetTexture(path);
            if (point)
            {
                texture.filterMode = FilterMode.Point;
            }
            texture.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = path;
            Sprites[key] = sprite;
            return sprite;
        }

        public static Texture2D GetTexture(string path)
        {
            if (Textures.TryGetValue(path, out var cached)) return cached;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogWarning("Missing texture: " + path);
                texture = Texture2D.whiteTexture;
            }
            Textures[path] = texture;
            return texture;
        }

        private static Sprite BuildRoundedSprite()
        {
            const int size = 64;
            const float radius = 14f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "EmberRounded";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var colors = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var px = x + 0.5f;
                    var py = y + 0.5f;
                    var cx = Mathf.Clamp(px, radius, size - radius);
                    var cy = Mathf.Clamp(py, radius, size - radius);
                    var distance = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                    var alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels32(colors);
            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(18f, 18f, 18f, 18f));
        }

        private static Sprite BuildCircleSprite(bool ring)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = ring ? "EmberRing" : "EmberCircle";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var colors = new Color32[size * size];
            var center = new Vector2(size * 0.5f, size * 0.5f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    var radius = size * 0.46f;
                    var alpha = ring
                        ? Mathf.Clamp01(4f - Mathf.Abs(distance - (radius - 3f)))
                        : Mathf.Clamp01(radius - distance + 0.5f);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels32(colors);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildGlowSprite()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "EmberGlow";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var colors = new Color32[size * size];
            var center = new Vector2(size * 0.5f, size * 0.5f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / (size * 0.5f);
                    var alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.2f);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels32(colors);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildVignetteSprite()
        {
            const int width = 256;
            const int height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "EmberVignette";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var colors = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var nx = Mathf.Abs(x / (float)(width - 1) * 2f - 1f);
                    var ny = Mathf.Abs(y / (float)(height - 1) * 2f - 1f);
                    var edge = Mathf.Pow(Mathf.Max(nx, ny), 2.4f);
                    colors[y * width + x] = new Color(0f, 0f, 0f, Mathf.Clamp01(edge * 0.9f));
                }
            }
            texture.SetPixels32(colors);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildGrainSprite()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "EmberGrain";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Point;
            var colors = new Color32[size * size];
            var random = new System.Random(7713);
            for (var i = 0; i < colors.Length; i++)
            {
                var value = random.NextDouble() < 0.18 ? (byte)random.Next(90, 180) : (byte)0;
                colors[i] = new Color32(255, 245, 220, value);
            }
            texture.SetPixels32(colors);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildDiamondSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "EmberDiamond";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var colors = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var nx = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                    var ny = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                    var alpha = Mathf.Clamp01((0.92f - nx - ny) * 10f);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels32(colors);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform _rect;
        private Vector2 _basePosition;
        private float _baseAngle;
        private float _baseScale = 1f;
        private bool _over;
        private Selectable _selectable;

        public bool CanInteract { get; set; }

        public void Configure(Vector2 basePosition, float baseAngle, bool canInteract, float baseScale = 1f)
        {
            _rect = (RectTransform)transform;
            _basePosition = basePosition;
            _baseAngle = baseAngle;
            _baseScale = baseScale;
            CanInteract = canInteract;
            _rect.anchoredPosition = basePosition;
            _rect.localEulerAngles = new Vector3(0f, 0f, baseAngle);
            transform.localScale = Vector3.one * baseScale;
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _over = CanInteract && (_selectable == null || _selectable.IsInteractable());
            if (_over)
            {
                transform.SetAsLastSibling();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _over = false;
        }

        private void Update()
        {
            if (_rect == null) return;
            var active = _over && CanInteract && (_selectable == null || _selectable.IsInteractable());
            var targetPosition = active ? _basePosition + new Vector2(0f, 58f) : _basePosition;
            var targetScale = Vector3.one * ((active ? 1.09f : 1f) * _baseScale);
            var targetAngle = active ? 0f : _baseAngle;
            var speed = 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime);
            _rect.anchoredPosition = Vector2.Lerp(_rect.anchoredPosition, targetPosition, speed);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, speed);
            var angle = Mathf.LerpAngle(_rect.localEulerAngles.z, targetAngle, speed);
            _rect.localEulerAngles = new Vector3(0f, 0f, angle);
        }
    }

    public class PulseLoop : MonoBehaviour
    {
        public float Speed = 1.8f;
        public float Amount = 0.05f;
        public float AlphaAmount = 0.12f;
        private Image _image;
        private Vector3 _baseScale;
        private Color _baseColor;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _baseScale = transform.localScale;
            if (_image != null) _baseColor = _image.color;
        }

        private void Update()
        {
            var wave = Mathf.Sin(Time.unscaledTime * Speed);
            transform.localScale = _baseScale * (1f + wave * Amount);
            if (_image != null)
            {
                var color = _baseColor;
                color.a = Mathf.Clamp01(color.a + wave * AlphaAmount);
                _image.color = color;
            }
        }
    }

    public class FloatBob : MonoBehaviour
    {
        public float Speed = 1.6f;
        public float Distance = 9f;
        private RectTransform _rect;
        private Vector2 _base;
        private float _phase;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _base = _rect.anchoredPosition;
            _phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (_rect == null) return;
            _rect.anchoredPosition = _base + Vector2.up * Mathf.Sin(Time.unscaledTime * Speed + _phase) * Distance;
        }
    }

    public class DriftTexture : MonoBehaviour
    {
        public float SpeedX = 0.003f;
        public float SpeedY = 0.0012f;
        private RawImage _image;

        private void Awake()
        {
            _image = GetComponent<RawImage>();
        }

        private void Update()
        {
            if (_image == null) return;
            var uv = _image.uvRect;
            uv.x += SpeedX * Time.unscaledDeltaTime;
            uv.y += SpeedY * Time.unscaledDeltaTime;
            _image.uvRect = uv;
        }
    }

    public class AudioBank : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private readonly AudioSource[] _sources = new AudioSource[5];
        private int _next;

        private void Awake()
        {
            for (var i = 0; i < _sources.Length; i++)
            {
                _sources[i] = gameObject.AddComponent<AudioSource>();
                _sources[i].playOnAwake = false;
                _sources[i].spatialBlend = 0f;
            }
        }

        public void Play(string key, float volume = 0.45f, float pitch = 1f)
        {
            if (!_clips.TryGetValue(key, out var clip))
            {
                clip = Resources.Load<AudioClip>("EmberAudio/" + key);
                _clips[key] = clip;
            }

            if (clip == null) return;
            var source = _sources[_next++ % _sources.Length];
            source.pitch = pitch;
            source.volume = volume;
            source.PlayOneShot(clip);
        }
    }

    public class SlowParticle : MonoBehaviour
    {
        private RectTransform _rect;
        private float _speed;
        private float _wobble;
        private float _phase;
        private float _life;
        private float _age;
        private Image _image;
        private Color _baseColor;

        public void Configure(float speed, float life)
        {
            _rect = (RectTransform)transform;
            _image = GetComponent<Image>();
            _baseColor = _image != null ? _image.color : Color.white;
            _speed = speed;
            _life = life;
            _wobble = UnityEngine.Random.Range(8f, 26f);
            _phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            _age = UnityEngine.Random.Range(0f, life);
        }

        private void Update()
        {
            if (_rect == null) return;
            _age += Time.unscaledDeltaTime;
            _rect.anchoredPosition += new Vector2(
                Mathf.Sin(Time.unscaledTime * 1.3f + _phase) * _wobble * Time.unscaledDeltaTime,
                _speed * Time.unscaledDeltaTime);
            if (_image != null)
            {
                var color = _baseColor;
                color.a *= Mathf.Sin(Mathf.Clamp01(_age / _life) * Mathf.PI);
                _image.color = color;
            }
            if (_age >= _life) Destroy(gameObject);
        }
    }
}
