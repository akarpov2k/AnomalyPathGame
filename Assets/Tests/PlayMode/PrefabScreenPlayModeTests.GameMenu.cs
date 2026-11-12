#nullable enable
using System.Collections;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Часть класса <see cref="PrefabScreenPlayModeTests"/>: меню игры (Esc и шестерёнка).</summary>
    public sealed partial class PrefabScreenPlayModeTests
    {
        /// <summary>
        /// Меню игры (Esc и шестерёнка) собрано префабом: задник, заголовок, сводка состояния
        /// и четыре кнопки — объекты префаба, подписи берутся из каталога, «Продолжить» возвращает в игру.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator GameMenu_Prefab_ButtonsAndLabelsFromCatalog()
        {
            yield return StartGame();
            FindScreen().ShowGameMenu();
            yield return null;

            var panel = GameObject.Find("GameMenu");
            Assert.IsNotNull(panel, "Меню игры не открылось");
            Assert.IsNotNull(panel!.GetComponent<GameMenuScreen>(), "Меню игры собрано не из префаба");
            Assert.IsNotNull(panel.transform.Find("ScreenBackdrop"), "У меню игры нет задника из префаба");
            Assert.IsNotNull(FindByPrefix(panel.transform, "GameMenuDeaths"), "В меню нет строки счётчика смертей");

            var screen = FindScreen();
            var title = FindByPrefix(panel.transform, "Title");
            Assert.IsNotNull(title, "В меню нет заголовка");
            Assert.AreEqual(
                UiText.Get(screen.Texts, UiTextKey.GameMenu_Title),
                title!.GetComponent<Text>().text,
                "Заголовок меню не взят из каталога текстов");

            foreach (var name in new[] { "Продолжить", "Сохранить игру", "Загрузить игру", "Выйти" })
            {
                var button = GameObject.Find(name);
                Assert.IsNotNull(button, $"В меню нет кнопки «{name}»");
                Assert.IsNotNull(button!.GetComponent<Button>(), $"«{name}» — не кнопка префаба");
            }

            var localized = panel.GetComponentsInChildren<LocalizedText>(true);
            Assert.Greater(localized.Length, 0, "В префабе меню игры нет локализованных подписей");
            foreach (var item in localized)
            {
                Assert.IsTrue(
                    item.Key.StartsWith("ui.", System.StringComparison.Ordinal),
                    $"Код «{item.Key}» у «{item.name}» не похож на ключ каталога");
                var label = item.GetComponent<Text>() ?? item.GetComponentInChildren<Text>(true);
                Assert.IsNotNull(label, $"У «{item.name}» нет подписи");
                Assert.AreEqual(
                    UiText.Get(screen.Texts, item.Key),
                    label!.text,
                    $"Подпись «{item.name}» не из каталога");
            }

            Click("Продолжить");
            yield return null;

            Assert.IsNotNull(GamePanel(), "«Продолжить» не вернула в игру");
            Assert.IsNull(GameObject.Find("GameMenu"), "Меню игры осталось поверх игры");
        }

        /// <summary>
        /// Расположение кнопок меню игры прежнее: колонка по центру экрана, кнопки одной ширины
        /// и высоты 54, сверху вниз «Продолжить», «Сохранить игру», «Загрузить игру»,
        /// «Выйти» с прежним зазором 10; плашка кнопок теперь пластина 9-slice.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator GameMenu_Prefab_KeepsLayout_OnPlates()
        {
            yield return StartGame();
            FindScreen().ShowGameMenu();
            yield return null;

            var panel = GameObject.Find("GameMenu");
            Assert.IsNotNull(panel, "Меню игры не открылось");

            var names = new[] { "Продолжить", "Сохранить игру", "Загрузить игру", "Выйти" };
            Canvas.ForceUpdateCanvases();
            var rects = new Rect[names.Length];
            var scale = 1f;
            for (var index = 0; index < names.Length; index++)
            {
                var button = GameObject.Find(names[index]);
                Assert.IsNotNull(button, $"В меню нет кнопки «{names[index]}»");
                var image = button!.GetComponent<Image>();
                Assert.IsNotNull(image.sprite, $"Кнопка «{names[index]}» без пластины");
                Assert.AreEqual(Image.Type.Sliced, image.type, $"Пластина «{names[index]}» не в 9-slice");
                var local = (RectTransform)button.transform;
                rects[index] = WorldRect(local);
                scale = local.rect.height > 0f ? rects[index].height / local.rect.height : 1f;
                Assert.AreEqual(
                    GameMenuMetrics.ButtonHeight,
                    local.rect.height,
                    0.01f,
                    $"Высота кнопки «{names[index]}» не прежняя");
            }

            for (var index = 0; index < rects.Length; index++)
            {
                Assert.AreEqual(rects[0].width, rects[index].width, 0.5f, "Кнопки меню разной ширины");
                Assert.AreEqual(rects[0].center.x, rects[index].center.x, 0.5f, "Кнопки меню не в одной колонке");
                if (index == 0)
                {
                    continue;
                }

                Assert.AreEqual(
                    GameMenuMetrics.ColumnSpacing * scale,
                    rects[index - 1].yMin - rects[index].yMax,
                    0.5f * scale,
                    $"Зазор между «{names[index - 1]}» и «{names[index]}» не прежний");
            }

            var panelRect = WorldRect((RectTransform)panel!.transform);
            Assert.AreEqual(
                panelRect.center.x,
                rects[0].center.x,
                panelRect.width * 0.01f,
                "Колонка кнопок меню не по центру экрана");
        }
    }
}
