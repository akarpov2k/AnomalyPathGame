#nullable enable
using System.Collections;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Модальные окна собраны префабами: у панели есть компонент окна и разметка из ассета,
    /// а не созданные кодом объекты.
    /// </summary>
    public sealed class DialogPrefabsPlayModeTests : PlayModeTestBase
    {
        /// <summary>Подтверждение создаётся из префаба с окном и кнопками.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Confirm_IsPrefab_WithWindow()
        {
            yield return StartGame();
            var screen = FindScreen();
            var dialog = screen.CreateConfirmDialog(screen.transform);
            Assert.IsNotNull(dialog.GetComponent<ConfirmDialog>(), "У панели нет компонента ConfirmDialog");
            Assert.IsNotNull(dialog.transform.Find("Window"), "В префабе нет окна подтверждения");
            Assert.IsNotNull(dialog.transform.Find("Window/ConfirmButtons"), "В префабе нет строки кнопок");
            Object.Destroy(dialog.gameObject);
            yield return null;
        }

        /// <summary>Окно диалога создаётся из префаба с меню, окном разговора и прокруткой.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Overlay_IsPrefab_WithMenuAndTalk()
        {
            yield return StartGame();
            var screen = FindScreen();
            var view = screen.CreateDialogOverlay(
                screen.transform,
                "TestDialog",
                new[]
                {
                    new DialogOverlayView.MenuEntry(
                        DialogOverlayView.MenuTalk,
                        UiTextKey.Dialog_Talk,
                        () => { })
                },
                _ => { },
                blockClicks: false);
            Assert.IsNotNull(view.GetComponent<DialogOverlayView>(), "У панели нет DialogOverlayView");
            Assert.IsNotNull(view.transform.Find("DialogMenu"), "В префабе нет меню диалога");
            Assert.IsNotNull(view.transform.Find("DialogTalkWindow"), "В префабе нет окна разговора");
            view.ShowMenu(new[] { DialogOverlayView.MenuTalk });
            Assert.IsTrue(view.MenuShown, "Меню диалога не показалось");
            Object.Destroy(view.gameObject);
            yield return null;
        }

        /// <summary>Окно подтверждения и его кнопки рисуются префабными текстурами.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Confirm_UsesFrameAndPlateTextures()
        {
            yield return StartGame();
            var screen = FindScreen();
            var dialog = screen.CreateConfirmDialog(screen.transform);
            var window = dialog.transform.Find("Window")!.GetComponent<Image>();
            Assert.IsNotNull(window.sprite, "у окна нет рамки");
            Assert.AreEqual(Image.Type.Sliced, window.type, "рамка окна не в режиме 9-slice");
            Assert.AreEqual(new Vector4(20f, 20f, 20f, 20f), window.sprite.border, "border рамки окна не 20 px");
            var buttons = dialog.transform.Find("Window/ConfirmButtons")!.GetComponentsInChildren<Button>(true);
            Assert.IsNotEmpty(buttons, "в окне нет кнопок");
            var plate = buttons[0].GetComponent<Image>();
            Assert.IsNotNull(plate.sprite, "у кнопки окна нет пластины");
            Assert.AreEqual(Image.Type.Sliced, plate.type, "пластина кнопки не в режиме 9-slice");
            Assert.AreEqual(new Vector4(8f, 8f, 8f, 8f), plate.sprite.border, "border пластины кнопки не 8 px");
            Object.Destroy(dialog.gameObject);
            yield return null;
        }

        /// <summary>Отдых открывается префабным окном с ползунком.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rest_IsPrefab_WithSlider()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen.Session!.State.World;
            Click("LeaveHub");
            yield return null;
            Assert.IsTrue(world.TrySpendStamina(4), "выносливость потрачена");
            screen.ShowGame(false);
            yield return null;
            Click("Rest");
            yield return null;

            var dialog = FindAnywhere("RestDialog");
            Assert.IsNotNull(dialog, "Диалог отдыха не создан");
            Assert.IsNotNull(dialog!.GetComponent<RestDialog>(), "У панели нет компонента RestDialog");
            Assert.IsNotNull(dialog.transform.Find("Window/RestHoursSlider"), "В префабе нет ползунка");
            screen.HandleEscape();
            yield return null;
        }
    }
}
