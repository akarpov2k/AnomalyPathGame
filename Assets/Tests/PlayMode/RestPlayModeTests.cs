#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Отдых на игровом экране: кнопка, ползунок часов, восстановление и подсказки.</summary>
    public sealed class RestPlayModeTests : PlayModeTestBase
    {
        /// <summary>Вне хаба ползунок доходит до часов по нехватке выносливости и показывает риск нападения.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rest_OutsideHub_ShowsRiskAndRestoresSlower()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen!.Session!.State.World;

            Click("LeaveHub");
            yield return null;
            Assert.IsTrue(world.TrySpendStamina(4), "выносливость потрачена");
            screen.ShowGame(false);
            yield return null;

            Click("Rest");
            yield return null;

            var dialog = FindAnywhere("RestDialog");
            Assert.IsNotNull(dialog, "диалог отдыха не создан");
            Assert.IsTrue(dialog!.activeSelf, "диалог отдыха не открылся");
            var slider = FindAnywhere("RestHoursSlider")!.GetComponent<Slider>();
            var quote = screen.Sessions.GetRestQuote(screen.Session!);
            Assert.AreEqual(quote.MaxHours, slider.maxValue, 1e-3f, "предел часов — из предложения отдыха");
            var riskText = FindAnywhere("RestRiskText")!.GetComponent<Text>();
            StringAssert.Contains("%", riskText.text, "риск в диалоге");
        }

        /// <summary>Ноль часов не запускает отдых: кнопка подтверждения недоступна.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rest_ZeroHours_ConfirmDisabled()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen!.Session!.State.World;
            Click("LeaveHub");
            yield return null;
            Assert.IsTrue(world.TrySpendStamina(4), "выносливость потрачена");
            screen.ShowGame(false);
            yield return null;

            Click("Rest");
            yield return null;
            var slider = FindAnywhere("RestHoursSlider")!.GetComponent<Slider>();
            slider.value = 0f;
            yield return null;

            var confirm = FindAnywhere("Отдыхать")!.GetComponent<Button>();
            Assert.IsFalse(confirm.interactable, "при нуле часов отдых не запускается");
        }

        /// <summary>
        /// Ползунок часов ловит мышь: дорожка и ручка — цели рейкаста, иначе перетаскивание
        /// не начинается и ползунок выглядит нерабочим.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rest_HoursSlider_IsDraggable()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen!.Session!.State.World;
            Click("LeaveHub");
            yield return null;
            Assert.IsTrue(world.TrySpendStamina(4), "выносливость потрачена");
            screen.ShowGame(false);
            yield return null;

            Click("Rest");
            yield return null;

            var slider = FindAnywhere("RestHoursSlider")!.GetComponent<Slider>();
            Assert.IsNotNull(slider.handleRect, "У ползунка нет ручки");
            Assert.IsTrue(
                slider.handleRect!.GetComponent<Image>().raycastTarget,
                "Ручка ползунка должна ловить мышь");
            var background = slider.transform.Find("Background");
            Assert.IsNotNull(background, "У ползунка нет дорожки");
            Assert.IsTrue(
                background!.GetComponent<Image>().raycastTarget,
                "Дорожка ползунка должна ловить клик");
        }

        /// <summary>Esc закрывает диалог отдыха, не открывая меню игры.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rest_Escape_ClosesDialog()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen!.Session!.State.World;
            Click("LeaveHub");
            yield return null;
            Assert.IsTrue(world.TrySpendStamina(4), "выносливость потрачена");
            screen.ShowGame(false);
            yield return null;

            Click("Rest");
            yield return null;
            var dialog = FindAnywhere("RestDialog");
            Assert.IsNotNull(dialog, "диалог отдыха не создан");
            Assert.IsTrue(dialog!.activeSelf, "диалог отдыха не открылся");

            screen.HandleEscape();
            yield return null;

            Assert.IsFalse(dialog.activeSelf, "Esc закрыл диалог отдыха");
            Assert.IsNull(GameObject.Find("GameMenu"), "меню игры не открылось");
        }
    }
}
