#nullable enable
using System;
using AnomalyPath.Editor.Art;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Готовые фоны экранов: id в каталоге спрайтов, простановка id заднику префаба и спрайт для
    /// кадров. Каталог собирает <see cref="SpriteCatalogBuilder"/> по папке <c>Assets/Art/Backdrops</c>,
    /// поэтому в префабе хранится только id, а картинку в игре подставляет хост.
    /// </summary>
    internal static class ScreenBackdropAssets
    {
        /// <summary>Поле задника с id спрайта в каталоге.</summary>
        private const string SpriteIdField = "spriteId";

        /// <summary>Поле задника с прямым спрайтом из инспектора.</summary>
        private const string SpriteField = "sprite";

        /// <summary>Проставляет заднику экрана id фона из каталога спрайтов.</summary>
        /// <param name="backdrop">Задник, собранный в префабе.</param>
        /// <param name="spriteId">Id фона; константы — <see cref="ScreenBackdropIds"/>.</param>
        /// <exception cref="ArgumentNullException">Задник не задан.</exception>
        /// <exception cref="ArgumentException">Id пуст.</exception>
        /// <exception cref="InvalidOperationException">У задника нет поля <c>spriteId</c>.</exception>
        public static void Apply(ScreenBackdrop backdrop, string spriteId)
        {
            if (backdrop == null)
            {
                throw new ArgumentNullException(nameof(backdrop));
            }

            if (string.IsNullOrEmpty(spriteId))
            {
                throw new ArgumentException("Пустой id фона экрана", nameof(spriteId));
            }

            var serialized = new SerializedObject(backdrop);
            var property = serialized.FindProperty(SpriteIdField);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Нет поля {SpriteIdField} у {nameof(ScreenBackdrop)}: задник сменил сериализованные поля");
            }

            property.stringValue = spriteId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Ставит кадру задник окна: прямой спрайт из префаба, затем спрайт по id каталога,
        /// а без картинки — плоская тема экрана. В игре картинку из каталога подставляет хост.
        /// </summary>
        /// <param name="panel">Собранный экран из префаба.</param>
        /// <param name="fallback">Цвет фона без картинки; <c>null</c> — цвет фона интерфейса.</param>
        public static void ApplyForScreenshot(GameObject panel, Color? fallback = null)
        {
            if (panel == null)
            {
                return;
            }

            var flat = fallback ?? UiFactory.BackgroundColor;
            foreach (var backdrop in panel.GetComponentsInChildren<ScreenBackdrop>(true))
            {
                var image = backdrop.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                var serialized = new SerializedObject(backdrop);
                var authored = serialized.FindProperty(SpriteField)?.objectReferenceValue as Sprite;
                var id = serialized.FindProperty(SpriteIdField)?.stringValue;
                var sprite = authored != null
                    ? authored
                    : string.IsNullOrEmpty(id) ? null : Load(id!);
                image.sprite = sprite;
                image.color = sprite != null ? Color.white : flat;
                image.preserveAspect = false;
            }
        }

        /// <summary>Загружает спрайт фона по id; импорт PNG приводится к режиму спрайта.</summary>
        /// <param name="spriteId">Id фона в каталоге спрайтов.</param>
        /// <returns>Спрайт или <c>null</c>, если файла нет.</returns>
        public static Sprite? Load(string spriteId)
        {
            var path = PathOf(spriteId);
            EnsureSpriteImporter(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Строит путь PNG по id: <c>backdrops/ui_bg_base</c> → <c>Assets/Art/Backdrops/ui_bg_base.png</c>.</summary>
        /// <param name="spriteId">Id фона в каталоге спрайтов.</param>
        /// <returns>Путь к файлу в проекте.</returns>
        private static string PathOf(string spriteId)
        {
            var index = spriteId.LastIndexOf('/');
            var name = index >= 0 ? spriteId.Substring(index + 1) : spriteId;
            return SpriteCatalogBuilder.BackdropsFolder + "/" + name + ".png";
        }

        /// <summary>Переводит PNG в режим Sprite, чтобы кадр и каталог видели картинку.</summary>
        /// <param name="path">Путь к PNG в проекте.</param>
        private static void EnsureSpriteImporter(string path)
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

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
