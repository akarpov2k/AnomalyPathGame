#nullable enable
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Текстуры панелей: рамка всплывающих окон <c>dialog_panel</c> и пластина металла для строк
    /// и окон экранов — спрайт с 9-slice, его импорт и раскладка.
    /// </summary>
    internal static class PanelArtAssets
    {
        /// <summary>Путь к PNG рамки в проекте.</summary>
        public const string Path = "Assets/Art/Backdrops/dialog_panel.png";

        /// <summary>Толщина рамки в пикселях спрайта — она же border для 9-slice.</summary>
        public const int Border = 20;

        /// <summary>Нейтральный тинт текстурной панели: текстура тёмная, тинт её лишь чуть гасит.</summary>
        public static readonly Color NeutralTint = new Color(0.92f, 0.95f, 0.92f, 1f);

        /// <summary>Тинт панели с зелёным оттенком: зона успеха в мини-игре взлома.</summary>
        public static readonly Color GreenTint = new Color(0.80f, 0.95f, 0.80f, 1f);

        /// <summary>Загружает спрайт рамки, при необходимости поправив импорт.</summary>
        /// <returns>Спрайт рамки.</returns>
        /// <exception cref="InvalidOperationException">Файла нет или он не импортирован как спрайт.</exception>
        public static Sprite Load()
        {
            EnsureImporter();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Нет спрайта рамки {Path}. Соберите каталог спрайтов: Tools/AnomalyPath/Собрать каталог спрайтов");
            }

            return sprite;
        }

        /// <summary>Ставит панели рамку и режим 9-slice, не растягивая углы.</summary>
        /// <param name="image">Плашка панели.</param>
        /// <param name="alpha">Прозрачность всей панели; 1 — как в спрайте.</param>
        public static void Apply(Image image, float alpha = 1f)
        {
            image.sprite = Load();
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, alpha);
            image.preserveAspect = false;
        }

        /// <summary>
        /// Закрывает панель текстурой металла вместо плоской заливки: 9-slice с кантом 8 px
        /// из пластины кнопок. Текстура тёмная, поэтому тинт её только подкрашивает.
        /// </summary>
        /// <param name="image">Изображение панели.</param>
        /// <param name="tint">Тинт поверх текстуры; <c>null</c> — нейтральный.</param>
        public static void ApplyPanel(Image image, Color? tint = null)
        {
            image.sprite = ButtonArtAssets.Plate();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = tint ?? NeutralTint;
            image.preserveAspect = false;
        }

        /// <summary>Переводит PNG в режим Sprite и задаёт border, если они ещё не заданы.</summary>
        private static void EnsureImporter()
        {
            if (!(AssetImporter.GetAtPath(Path) is TextureImporter importer))
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

            var border = importer.spriteBorder;
            if (border.x != Border || border.y != Border || border.z != Border || border.w != Border)
            {
                importer.spriteBorder = new Vector4(Border, Border, Border, Border);
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
