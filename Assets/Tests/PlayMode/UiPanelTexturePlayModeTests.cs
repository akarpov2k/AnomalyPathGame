#nullable enable
using System.Collections;
using AnomalyPath.Runtime.Screens.Quests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Текстуры панелей вместо плоских заливок: строки настроек, панели журнала, окна замка
    /// и талантов закрыты 9-slice-пластиной, зона «Попытки» замка подкрашена зелёным, а ручка
    /// прокрутки нейтральная. Картинка задания на доске показывается без искажения.
    /// </summary>
    public sealed class UiPanelTexturePlayModeTests : PlayModeTestBase
    {
        private const string PlateSpriteName = "ui_plate_normal";

        /// <summary>Строки настроек и список разрешений стоят на текстурной пластине.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Settings_Rows_UsePlateTexture()
        {
            yield return LoadMain();

            Click("Настройки");
            yield return null;

            foreach (var name in new[] { "ResolutionRow", "VolumeRow", "SensitivityRow", "KeysRow" })
            {
                AssertPlate(name, GameObject.Find(name));
            }

            AssertPlate("ResolutionOptions", FindAnywhere("ResolutionOptions"));
        }

        /// <summary>Зелёная зона «Попытки» замка стала текстурной панелью с лёгким зелёным тинтом.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator LockMinigame_History_UsesGreenPlate()
        {
            yield return StartGame();

            FindScreen().ShowLockMinigame();
            yield return null;

            var panel = GameObject.Find("LockMinigame");
            Assert.IsNotNull(panel, "Окно замка не открылось");
            var history = panel!.transform.Find("Content/Window/History");
            Assert.IsNotNull(history, "В окне замка нет зоны «Попытки»");
            var image = history!.GetComponent<Image>();
            Assert.IsNotNull(image, "У зоны «Попытки» нет изображения");
            Assert.AreEqual(PlateSpriteName, image!.sprite?.name, "Зона «Попытки» должна быть текстурной");
            Assert.AreEqual(Image.Type.Sliced, image.type, "Панель должна растягиваться 9-slice");
            Assert.Greater(image.color.g, image.color.r, "У зоны «Попытки» должен остаться зелёный оттенок");
        }

        /// <summary>Ручка прокрутки настроек нейтральная, без зелёной заливки-заглушки.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Settings_ScrollbarHandle_IsNeutral()
        {
            yield return LoadMain();

            Click("Настройки");
            yield return null;

            var panel = GameObject.Find("Settings");
            Assert.IsNotNull(panel, "Экран настроек не открылся");
            var handle = panel!.transform.Find("Content/ContentColumn/Rows/Scrollbar/Sliding Area/Handle");
            Assert.IsNotNull(handle, "У списка настроек нет ручки прокрутки");
            var color = handle!.GetComponent<Image>()!.color;
            var spread = Mathf.Max(color.r, Mathf.Max(color.g, color.b)) -
                         Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            Assert.Less(spread, 0.08f, "Ручка прокрутки должна быть нейтральной, без зелёного оттенка");
        }

        /// <summary>Карточка задания и её картинка: текстурный фон, готовый арт без растяжения.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator QuestBoard_CardArt_KeepsAspectAndUsesQuestSprite()
        {
            yield return StartGame();

            var screen = FindScreen();
            screen.ShowQuests(screen.Session!.State.World.CurrentLocation.InstanceId);
            yield return null;

            QuestBoardCard? shown = null;
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (!transform.name.StartsWith("Board_", System.StringComparison.Ordinal))
                {
                    continue;
                }

                var candidate = transform.GetComponent<QuestBoardCard>();
                if (candidate?.Art != null && candidate.Art.enabled && candidate.Art.sprite != null)
                {
                    shown = candidate;
                    break;
                }
            }

            Assert.IsNotNull(shown, "На доске нет карточки с картинкой задания");
            Assert.IsNotNull(shown!.Background, "У карточки нет фона");
            Assert.AreEqual(
                PlateSpriteName,
                shown.Background!.sprite?.name,
                "Карточка должна стоять на текстурной пластине");
            Assert.IsTrue(shown.Art!.preserveAspect, "Картинка задания не должна растягиваться");
            CollectionAssert.Contains(
                new[] { "clearAnomaly", "clearLocation", "default", "findItems", "gatherArtifacts", "killMutants" },
                shown.Art.sprite!.name,
                "Карточка должна показывать готовый арт задания");
        }

        /// <summary>Проверяет, что у панели текстура пластины и режим 9-slice.</summary>
        /// <param name="name">Имя объекта для сообщения.</param>
        /// <param name="go">Объект панели.</param>
        private static void AssertPlate(string name, GameObject? go)
        {
            Assert.IsNotNull(go, $"Нет объекта «{name}»");
            var image = go!.GetComponent<Image>();
            Assert.IsNotNull(image, $"У «{name}» нет изображения");
            Assert.AreEqual(PlateSpriteName, image!.sprite?.name, $"У «{name}» не текстурная панель");
            Assert.AreEqual(Image.Type.Sliced, image.type, $"Панель «{name}» должна растягиваться 9-slice");
        }
    }
}
