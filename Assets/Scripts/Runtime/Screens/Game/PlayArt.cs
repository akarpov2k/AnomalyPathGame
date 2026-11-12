#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Геометрические примитивы игрового экрана, которые рисуются в памяти: плашка объекта
    /// (круг, растягивается в эллипс), свечение при наведении и указатель перехода. Это не иконки —
    /// значки, часы и карта берутся из префаба и каталога спрайтов. Крестик остаётся здесь для
    /// редакторного инструмента, который пишет <c>ui_icon_close.png</c>.
    /// </summary>
    public static class PlayArt
    {
        private const int Size = 128;
        private const int GlowPixels = 256;
        private const int SignWidth = 128;
        private const int SignHeight = 160;

        private static Sprite? _circle;
        private static Sprite? _glow;
        private static Sprite? _signpost;
        private static Sprite? _close;

        /// <summary>Крестик закрытия: две диагональные полосы.</summary>
        /// <returns>Спрайт крестика.</returns>
        public static Sprite Close()
        {
            if (_close == null)
            {
                _close = BuildClose();
            }

            return _close;
        }

        /// <summary>Белый круг для области объекта; растягивается в эллипс.</summary>
        /// <returns>Спрайт круга.</returns>
        public static Sprite Circle()
        {
            if (_circle == null)
            {
                _circle = BuildCircle();
            }

            return _circle;
        }

        /// <summary>Мягкое круглое свечение для подсветки объекта при наведении.</summary>
        /// <returns>Спрайт свечения.</returns>
        public static Sprite Glow()
        {
            if (_glow == null)
            {
                _glow = BuildGlow();
            }

            return _glow;
        }

        /// <summary>Общий указатель для локации-цели, у которой нет собственной вырезки.</summary>
        /// <returns>Спрайт указателя.</returns>
        public static Sprite Signpost()
        {
            if (_signpost == null)
            {
                _signpost = BuildSignpost();
            }

            return _signpost;
        }

        private static Sprite BuildCircle()
        {
            var center = (Size - 1) * 0.5f;
            var radius = (Size * 0.5f) - 2f;
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var alpha = Mathf.Clamp01(radius - distance + 1f);
                    pixels[(y * Size) + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            return CreateSprite(pixels, "PlayCircle");
        }

        private static Sprite BuildClose()
        {
            var center = (Size - 1) * 0.5f;
            var half = Size * 0.36f;
            var thickness = Size * 0.07f;
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var reach = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                    var distance = Mathf.Min(Mathf.Abs(dy - dx), Mathf.Abs(dy + dx)) / Mathf.Sqrt(2f);
                    var alpha = reach <= half ? Mathf.Clamp01(thickness - distance + 0.5f) : 0f;
                    pixels[(y * Size) + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            return CreateSprite(pixels, "PlayClose");
        }

        private static Sprite BuildGlow()
        {
            var center = (GlowPixels - 1) * 0.5f;
            var radius = GlowPixels * 0.5f;
            var pixels = new Color32[GlowPixels * GlowPixels];
            for (var y = 0; y < GlowPixels; y++)
            {
                for (var x = 0; x < GlowPixels; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt((dx * dx) + (dy * dy)) / radius;
                    var falloff = Mathf.Clamp01(1f - distance);
                    var alpha = falloff * falloff;
                    pixels[(y * GlowPixels) + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            return CreateSprite(pixels, "PlayGlow", GlowPixels);
        }

        private static Sprite BuildSignpost()
        {
            var pixels = new Color32[SignWidth * SignHeight];
            for (var y = 0; y < SignHeight; y++)
            {
                for (var x = 0; x < SignWidth; x++)
                {
                    var alpha = 0f;
                    if (x >= 58 && x <= 70 && y >= 6 && y <= 108)
                    {
                        alpha = 1f;
                    }

                    var onBoard = x >= 6 && x <= 122 && y >= 100 && y <= 152;
                    var cornerCut = (x < 18 && y < 112) || (x > 110 && y < 112) ||
                                    (x < 18 && y > 140) || (x > 110 && y > 140);
                    if (onBoard && !cornerCut)
                    {
                        alpha = 1f;
                    }

                    pixels[(y * SignWidth) + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            return CreateSprite(pixels, "PlaySignpost", SignWidth, SignHeight);
        }

        private static Sprite CreateSprite(Color32[] pixels, string name, int size = Size) =>
            CreateSprite(pixels, name, size, size);

        private static Sprite CreateSprite(Color32[] pixels, string name, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
