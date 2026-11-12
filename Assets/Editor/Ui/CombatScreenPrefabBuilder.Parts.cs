#nullable enable
using System;
using System.IO;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Часть класса <see cref="CombatScreenPrefabBuilder"/>: общие помощники разметки —
    /// прямоугольники, картинки, подписи, кнопки, локальные подписи и сохранение префаба.
    /// </summary>
    public static partial class CombatScreenPrefabBuilder
    {
        /// <summary>Создаёт пустой прямоугольник внутри родителя.</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="name">Имя объекта.</param>
        /// <returns>Прямоугольник.</returns>
        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Создаёт картинку с цветом без перехвата кликов.</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="name">Имя объекта.</param>
        /// <param name="color">Цвет.</param>
        /// <returns>Компонент картинки.</returns>
        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Создаёт подпись.</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="name">Имя объекта.</param>
        /// <param name="text">Начальный текст.</param>
        /// <param name="size">Размер шрифта.</param>
        /// <param name="anchor">Выравнивание.</param>
        /// <param name="color">Цвет текста.</param>
        /// <param name="bold">Жирный шрифт.</param>
        /// <returns>Компонент текста.</returns>
        private static Text CreateText(
            Transform parent,
            string name,
            string text,
            int size,
            TextAnchor anchor,
            Color color,
            bool bold = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = bold ? UiFactory.FindBoldFont() : UiFactory.FindFont();
            label.fontSize = size;
            label.text = text;
            label.alignment = anchor;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        /// <summary>Создаёт обычную кнопку-плашку с подписью.</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="name">Имя объекта.</param>
        /// <param name="fontSize">Размер шрифта подписи.</param>
        /// <returns>Кнопка.</returns>
        private static Button CreateButton(Transform parent, string name, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = UiFactory.ButtonColor;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var label = CreateText(go.transform, "Label", string.Empty, fontSize, TextAnchor.MiddleCenter, UiFactory.TextColor);
            Stretch(label.rectTransform, 4f);
            return button;
        }

        /// <summary>Создаёт кнопку-действие с кадром и зелёным индикатором наведения.</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="name">Имя объекта.</param>
        /// <param name="outLabel">Подпись кнопки.</param>
        /// <returns>Кнопка.</returns>
        private static Button CreateActionButton(Transform parent, string name, out Text outLabel)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(ActionFramePath);
            var highlighted = AssetDatabase.LoadAssetAtPath<Sprite>(ActionFrameGreenPath);
            if (frame != null)
            {
                image.sprite = frame;
                image.color = Color.white;
            }
            else
            {
                image.color = UiFactory.ButtonColor;
            }

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (frame != null)
            {
                var art = go.AddComponent<ButtonArt>();
                art.Configure(image, frame, highlighted);
            }

            outLabel = CreateText(
                go.transform,
                "Label",
                string.Empty,
                ActionButtonMetrics.LabelFontSize,
                TextAnchor.MiddleCenter,
                ActionButtonPalette.Label,
                bold: true);
            ActionButtonMetrics.PlaceLabel(outLabel, ActionButtonMetrics.LabelFontSize);
            return button;
        }

        /// <summary>Добавляет подписи компонент локализации по ключу.</summary>
        /// <param name="label">Подпись.</param>
        /// <param name="key">Ключ текста.</param>
        /// <returns>Компонент локализованной подписи.</returns>
        private static LocalizedText AddLocalized(Text label, UiTextKey key)
        {
            var localized = label.gameObject.AddComponent<LocalizedText>();
            localized.Key = UiText.Code(key);
            return localized;
        }

        /// <summary>Растягивает прямоугольник по родителю с отступом.</summary>
        /// <param name="rect">Прямоугольник.</param>
        /// <param name="inset">Отступ со всех сторон.</param>
        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Ставит прямоугольник от левого верхнего угла родителя.</summary>
        /// <param name="rect">Прямоугольник.</param>
        /// <param name="position">Смещение от угла.</param>
        /// <param name="size">Размер.</param>
        private static void PlaceTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Ставит прямоугольник от правого верхнего угла родителя.</summary>
        /// <param name="rect">Прямоугольник.</param>
        /// <param name="position">Смещение от угла.</param>
        /// <param name="size">Размер.</param>
        private static void PlaceTopRight(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Ставит прямоугольник по центру родителя.</summary>
        /// <param name="rect">Прямоугольник.</param>
        /// <param name="position">Смещение от центра.</param>
        /// <param name="size">Размер.</param>
        private static void PlaceCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Ставит прямоугольник на долю ширины и высоты родителя.</summary>
        /// <param name="rect">Прямоугольник.</param>
        /// <param name="min">Доля от левого нижнего угла.</param>
        /// <param name="max">Доля от левого нижнего угла.</param>
        private static void PlaceFraction(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Ставит прямоугольник в точку с долями родителя и заданным размером.</summary>
        /// <param name="rect">Прямоугольник.</param>
        /// <param name="anchor">Доля от левого нижнего угла.</param>
        /// <param name="pivot">Точка привязки.</param>
        /// <param name="position">Смещение от точки привязки.</param>
        /// <param name="size">Размер.</param>
        private static void PlaceAt(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Ставит картинке спрайт по пути ассета, если он есть.</summary>
        /// <param name="image">Картинка.</param>
        /// <param name="path">Путь PNG-ассета.</param>
        private static void ApplySprite(Image image, string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }

        /// <summary>Делает из картинки полосу с горизонтальным заполнением от левого края.</summary>
        /// <param name="image">Картинка полосы.</param>
        private static void MakeFilled(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
        }

        /// <summary>Задаёт ссылку на объект в сериализованном поле компонента.</summary>
        /// <param name="target">Компонент.</param>
        /// <param name="field">Имя поля.</param>
        /// <param name="value">Ссылка на объект.</param>
        /// <exception cref="InvalidOperationException">Поля нет у компонента.</exception>
        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object? value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException($"Нет поля {field} у {target.GetType().Name}");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Задаёт число в сериализованном поле компонента.</summary>
        /// <param name="target">Компонент.</param>
        /// <param name="field">Имя поля.</param>
        /// <param name="value">Новое значение.</param>
        /// <exception cref="InvalidOperationException">Поля нет у компонента.</exception>
        private static void SetFloat(UnityEngine.Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException($"Нет поля {field} у {target.GetType().Name}");
            }

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Задаёт точку Vector2 в сериализованном поле компонента.</summary>
        /// <param name="target">Компонент.</param>
        /// <param name="field">Имя поля.</param>
        /// <param name="value">Новое значение.</param>
        /// <exception cref="InvalidOperationException">Поля нет у компонента.</exception>
        private static void SetVector2(UnityEngine.Object target, string field, Vector2 value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException($"Нет поля {field} у {target.GetType().Name}");
            }

            property.vector2Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Создаёт папку проекта, если её нет.</summary>
        /// <param name="path">Путь папки.</param>
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            var name = Path.GetFileName(path)!;
            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>Сохраняет префаб, выключая корень, и удаляет временный объект сцены.</summary>
        /// <param name="root">Корень префаба.</param>
        /// <param name="path">Путь ассета.</param>
        /// <returns>Сохранённый префаб.</returns>
        /// <exception cref="InvalidOperationException">Префаб не сохранился.</exception>
        private static GameObject SavePrefab(GameObject root, string path)
        {
            ButtonArtAssets.ApplyPlatesToFlatButtons(root);
            root.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Не удалось сохранить префаб {path}");
            }

            return prefab;
        }
    }
}
