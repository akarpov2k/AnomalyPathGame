#nullable enable
using System;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Загружает кадры кнопок-действий и пластины строк для сборщиков префабов.</summary>
    internal static class ButtonArtAssets
    {
        /// <summary>Кадр кнопки, когда курсор не наведён.</summary>
        public const string NormalPath = "Assets/Art/Buttons/ui_btn_mp_ind_empty.png";

        /// <summary>Кадр кнопки с зелёным индикатором при наведении.</summary>
        public const string HighlightedPath = "Assets/Art/Buttons/ui_btn_mp_ind_green.png";

        /// <summary>Пластина строки выпадашки и быстрого слота.</summary>
        public const string PlateNormalPath = "Assets/Art/Buttons/ui_plate_normal.png";

        /// <summary>Пластина с тёплой подсветкой для наведения и выбора.</summary>
        public const string PlateHighlightedPath = "Assets/Art/Buttons/ui_plate_highlight.png";

        /// <summary>Толщина канта пластины в пикселях спрайта — border для 9-slice.</summary>
        public const int PlateBorder = 8;

        /// <summary>С какого размера кнопки кант пластины не уменьшается.</summary>
        private const float SmallButtonSize = 60f;

        /// <summary>Площадь, с которой кнопка считается плашкой-перехватчиком и не обрамляется.</summary>
        private const float CatcherArea = 400000f;

        /// <summary>Сколько добавляется к имени иконочной кнопки, чтобы имя плашки было читаемым.</summary>
        private const string PlateSuffix = "Plate";

        /// <summary>Насколько плашка выступает за иконку по умолчанию, в единицах канваса.</summary>
        public const float IconPlatePadding = 6f;

        /// <summary>Возвращает кадр без подсветки.</summary>
        /// <returns>Спрайт кнопки.</returns>
        public static Sprite Normal() => Load(NormalPath, 0);

        /// <summary>Возвращает кадр с зелёным индикатором.</summary>
        /// <returns>Спрайт подсвеченной кнопки.</returns>
        public static Sprite Highlighted() => Load(HighlightedPath, 0);

        /// <summary>
        /// Возвращает пластину строки без подсветки: 9-slice с кантом 8 px. Той же текстурой
        /// сборщики закрывают панели, у которых раньше была плоская заливка.
        /// </summary>
        /// <returns>Спрайт пластины.</returns>
        public static Sprite Plate() => Load(PlateNormalPath, PlateBorder);

        /// <summary>
        /// Ставит кнопке пластину с рамкой: 9-slice и смена кадра при наведении и выборе.
        /// </summary>
        /// <param name="button">Кнопка строки, слота или служебного действия.</param>
        /// <param name="pixelsPerUnitMultiplier">Во сколько раз уменьшить кант; 2 — для мелких кнопок.</param>
        /// <param name="tint">Цвет поверх пластины; <c>null</c> — белый.</param>
        public static void ApplyPlate(Button button, float pixelsPerUnitMultiplier = 1f, Color? tint = null)
        {
            var image = button.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var normal = Load(PlateNormalPath, PlateBorder);
            var highlighted = Load(PlateHighlightedPath, PlateBorder);
            image.sprite = normal;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = Mathf.Max(1f, pixelsPerUnitMultiplier);
            image.color = tint ?? Color.white;
            var art = button.GetComponent<ButtonArt>() ?? button.gameObject.AddComponent<ButtonArt>();
            art.Configure(image, normal, highlighted);
        }

        /// <summary>
        /// Обрамляет пластиной все кнопки панели, у которых ещё нет своего спрайта:
        /// служебные кнопки экранов получают тот же металл, что строки выпадашек.
        /// </summary>
        /// <param name="root">Собранная панель.</param>
        /// <returns>Сколько кнопок получило пластину.</returns>
        public static int ApplyPlatesToFlatButtons(GameObject root)
        {
            var count = 0;
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                var button = image.GetComponent<Button>();
                if (button == null || image.sprite != null)
                {
                    continue;
                }

                if (image.color.a < 0.1f)
                {
                    continue;
                }

                var rect = image.rectTransform.rect;
                if (rect.width * rect.height > CatcherArea)
                {
                    continue;
                }

                var scale = Mathf.Min(rect.width, rect.height) < SmallButtonSize ? 2f : 1f;
                ApplyPlate(button, scale, TintFor(image.color));
                count++;
            }

            return count;
        }

        /// <summary>
        /// Кладёт плашку позади иконочной кнопки: 9-slice, кадр подсветки при наведении и выборе.
        /// Плашка становится соседом кнопки в её родителе и выступает за неё на <paramref name="padding"/>,
        /// поэтому иконка остаётся на своём месте и не растягивается.
        /// </summary>
        /// <param name="button">Кнопка, у которой сейчас только иконка без фона.</param>
        /// <param name="padding">Насколько плашка больше кнопки с каждой стороны; 0 — ровно по кнопке.</param>
        /// <returns>Изображение плашки; <c>null</c>, если у кнопки нет родителя.</returns>
        /// <exception cref="ArgumentNullException">Кнопка не задана.</exception>
        public static Image? ApplyPlateBehind(Button button, float padding = IconPlatePadding)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            var source = (RectTransform)button.transform;
            var parent = source.parent;
            if (parent == null)
            {
                return null;
            }

            var plateObject = new GameObject(button.name + PlateSuffix, typeof(RectTransform), typeof(Image));
            var plate = (RectTransform)plateObject.transform;
            plate.SetParent(parent, false);
            plate.anchorMin = source.anchorMin;
            plate.anchorMax = source.anchorMax;
            plate.pivot = source.pivot;
            plate.sizeDelta = source.sizeDelta + new Vector2(2f * padding, 2f * padding);
            plate.anchoredPosition = source.anchoredPosition +
                new Vector2((source.pivot.x - 0.5f) * 2f * padding, (source.pivot.y - 0.5f) * 2f * padding);
            plate.localScale = source.localScale;
            plate.SetSiblingIndex(source.GetSiblingIndex());

            var normal = Load(PlateNormalPath, PlateBorder);
            var highlighted = Load(PlateHighlightedPath, PlateBorder);
            var image = plateObject.GetComponent<Image>();
            image.sprite = normal;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = PlateScale(source);
            image.color = Color.white;
            image.raycastTarget = false;

            var art = button.GetComponent<ButtonArt>() ?? button.gameObject.AddComponent<ButtonArt>();
            art.Configure(image, normal, highlighted);
            return image;
        }

        /// <summary>
        /// Во сколько раз уменьшить кант плашки: у кнопок меньше 60 px он вдвое тоньше, иначе 8 px
        /// съедают половину иконки. Растянутая по родителю кнопка считается крупной.
        /// </summary>
        /// <param name="source">Прямоугольник кнопки.</param>
        /// <returns>Множитель канта для <see cref="Image.pixelsPerUnitMultiplier"/>.</returns>
        private static float PlateScale(RectTransform source)
        {
            var side = Mathf.Min(source.rect.width, source.rect.height);
            if (side <= 0f)
            {
                side = Mathf.Min(Mathf.Abs(source.sizeDelta.x), Mathf.Abs(source.sizeDelta.y));
            }

            return side > 0f && side < SmallButtonSize ? 2f : 1f;
        }

        /// <summary>Подбирает цвет пластины под прежнюю плашку: зелёная остаётся зелёной.</summary>
        /// <param name="color">Цвет плашки до обрамления.</param>
        /// <returns>Цвет поверх пластины.</returns>
        private static Color TintFor(Color color)
        {
            var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            if (max - min < 0.12f)
            {
                return Color.white;
            }

            if (color.g >= color.r && color.g >= color.b)
            {
                return new Color(0.62f, 0.95f, 0.66f);
            }

            if (color.r >= color.g && color.r >= color.b)
            {
                return new Color(0.95f, 0.62f, 0.55f);
            }

            return new Color(0.62f, 0.75f, 0.95f);
        }

        private static Sprite Load(string path, int border)
        {
            EnsureBorder(path, border);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Нет спрайта кнопки {path}. Соберите каталог спрайтов: Tools/AnomalyPath/Собрать каталог спрайтов");
            }

            return sprite;
        }

        private static void EnsureBorder(string path, int border)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                return;
            }

            var changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            var current = importer.spriteBorder;
            if (current.x != border || current.y != border || current.z != border || current.w != border)
            {
                importer.spriteBorder = new Vector4(border, border, border, border);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
