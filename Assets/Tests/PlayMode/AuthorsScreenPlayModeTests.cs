#nullable enable
using System.Collections;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Проверяет экран авторов: префаб, заголовок игры сверху, идущие титры в окне
    /// по центру и выход в стартовое меню кнопкой и по Esc.
    /// </summary>
    public sealed class AuthorsScreenPlayModeTests : PlayModeTestBase
    {
        /// <summary>Пункт «Авторы» открывает экран с заголовком игры по центру и окном титров.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Authors_OpensWithGameTitleAndCredits()
        {
            yield return LoadMain();

            var mainMenu = GameObject.Find("MainMenu");
            Click("Авторы");
            yield return null;

            var panel = GameObject.Find("Authors");
            Assert.IsNotNull(panel, "Экран авторов не открылся");
            Assert.IsTrue(panel!.activeSelf, "Экран авторов не активен");
            Assert.IsNotNull(panel.GetComponent<AuthorsScreen>(), "Экран авторов собран не из префаба");
            Assert.IsFalse(mainMenu!.activeSelf, "Стартовое меню осталось активным");

            var texts = FindScreen().Texts;
            var title = panel.transform.Find("Content/AuthorsTitle");
            Assert.IsNotNull(title, "На экране авторов нет заголовка игры");
            Assert.AreEqual(
                UiText.Get(texts, UiTextKey.MainMenu_Game_Title),
                title!.GetComponent<Text>().text,
                "Заголовок игры взят не из каталога строк");
            var screenRect = WorldRect((RectTransform)panel.transform);
            var titleRect = WorldRect((RectTransform)title.transform);
            Assert.AreEqual(screenRect.center.x, titleRect.center.x, 0.5f, "Заголовок игры стоит не по центру экрана");
            Assert.Greater(titleRect.center.y, screenRect.center.y, "Заголовок игры должен стоять в верхней части экрана");

            var credits = panel.GetComponentInChildren<CreditsScroller>(true);
            Assert.IsNotNull(credits, "На экране авторов нет идущих титров");
            var label = credits!.Track!.GetComponentInChildren<Text>(true);
            Assert.IsNotNull(label, "У титров нет текста");
            Assert.AreEqual(
                UiText.Get(texts, UiTextKey.Authors_Credits),
                label!.text,
                "Титры взяты не из каталога строк");

            var window = panel.transform.Find("Content/Window");
            Assert.IsNotNull(window, "У экрана авторов нет окна титров");
            var windowRect = WorldRect((RectTransform)window!);
            Assert.AreEqual(screenRect.center.x, windowRect.center.x, 0.5f, "Окно титров стоит не по центру экрана");
            Assert.AreEqual(screenRect.center.y, windowRect.center.y, 0.5f, "Окно титров стоит не по центру экрана");

            Assert.IsNotNull(GameObject.Find("AuthorsBack"), "Нет кнопки выхода в стартовое меню");
        }

        /// <summary>Титры длиннее окна и сами едут вверх, а за верхом окна начинаются заново снизу.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Authors_Credits_RunUpAndStartOver()
        {
            yield return LoadMain();

            Click("Авторы");
            yield return null;

            var credits = GameObject.Find("Authors")!.GetComponentInChildren<CreditsScroller>(true);
            Assert.IsNotNull(credits, "На экране авторов нет идущих титров");
            var height = credits!.Track!.sizeDelta.y;
            var viewport = credits.Viewport!.rect.height;
            Assert.Greater(height, viewport, "Титры должны быть длиннее окна, иначе им некуда ехать");
            Assert.IsTrue(credits.IsRunning, "Титры не запустились при показе экрана");

            var start = credits.Offset;
            credits.Advance(1f);
            Assert.AreEqual(start + credits.Speed, credits.Offset, 0.01f, "Титры не поехали вверх");

            credits.Advance(1000f);
            Assert.LessOrEqual(credits.Offset, viewport + 0.01f, "Титры ушли выше окна");
            Assert.GreaterOrEqual(credits.Offset, -height - 0.01f, "Титры не начались заново снизу");
            Assert.AreEqual(viewport, credits.Viewport!.rect.height, 0.01f, "Окно титров поменяло высоту");
        }

        /// <summary>Кнопка выхода и Esc возвращают со экрана авторов в стартовое меню.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Authors_BackAndEscape_ReturnToMenu()
        {
            yield return LoadMain();

            Click("Авторы");
            yield return null;
            Click("AuthorsBack");
            yield return null;
            Assert.IsTrue(GameObject.Find("MainMenu")!.activeSelf, "Кнопка выхода не вернула в стартовое меню");
            Assert.IsNull(GameObject.Find("Authors"), "Экран авторов остался активным");

            Click("Авторы");
            yield return null;
            Assert.IsTrue(FindScreen().HandleEscape(), "Esc на экране авторов не обработан");
            yield return null;
            Assert.IsTrue(GameObject.Find("MainMenu")!.activeSelf, "Esc не вернул в стартовое меню");
            Assert.IsNull(GameObject.Find("Authors"), "Экран авторов остался активным после Esc");
        }

        /// <summary>Каждая подпись префаба авторов хранит код ключа и берёт текст из каталога строк.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Authors_Labels_TakeTextFromCatalog()
        {
            yield return LoadMain();
            Click("Авторы");
            yield return null;

            var panel = GameObject.Find("Authors");
            Assert.IsNotNull(panel, "Экран авторов не открылся");
            var texts = FindScreen().Texts;
            var localized = panel!.GetComponentsInChildren<LocalizedText>(true);
            Assert.Greater(localized.Length, 0, "В префабе авторов нет локализованных подписей");
            foreach (var item in localized)
            {
                Assert.IsTrue(
                    item.Key.StartsWith("ui.", System.StringComparison.Ordinal),
                    $"Код «{item.Key}» у «{item.name}» не похож на ключ каталога");
                Assert.IsTrue(
                    texts.Contains(item.Key),
                    $"Ключа «{item.Key}» у «{item.name}» нет в каталоге строк");
                var label = item.GetComponent<Text>();
                Assert.IsNotNull(label, $"У «{item.name}» нет подписи");
                Assert.AreEqual(
                    UiText.Get(texts, item.Key),
                    label!.text,
                    $"Подпись «{item.name}» не из каталога");
            }
        }
    }
}
