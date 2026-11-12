#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префабы модальных окон по прототипам `.prototypes/подтверждение`, `выход`,
    /// `отдых` и `окно диалога`: затемнение, окно, подписи и кнопки становятся объектами
    /// префаба, код только заполняет динамику. Префабы назначаются на <c>GameScreen</c>
    /// в сцене Main. Лута среди них нет: лут объекта и аномалии показывает `RewardScreen`.
    /// </summary>
    public static partial class DialogPrefabsBuilder
    {
        private const string Folder = "Assets/Prefabs/Dialogs";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ConfirmPath = Folder + "/ConfirmDialog.prefab";
        private const string ExitPath = Folder + "/ExitDialog.prefab";
        private const string RestPath = Folder + "/RestDialog.prefab";
        private const string OverlayPath = Folder + "/DialogOverlay.prefab";

        private static readonly Color Dark = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color Green = new Color(0.24f, 0.48f, 0.30f, 1f);
        private static readonly Color Window = new Color(0.05f, 0.065f, 0.06f, 0.97f);
        private static readonly Color Reply = new Color(0.08f, 0.10f, 0.09f, 0.95f);

        private static IGameTexts _texts = null!;

        /// <summary>Пункт меню: собрать префабы модальных окон и привязать их к сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префабы «Модальные окна»")]
        public static void BuildFromMenu() => Build();

        /// <summary>Точка входа batch-режима.</summary>
        public static void BuildFromBatch()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Собирает четыре префаба и назначает их на <c>GameScreen</c>.</summary>
        public static void Build()
        {
            _texts = LoadTexts();
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(Folder);
            var prefabs = new[]
            {
                Save(BuildConfirm(), ConfirmPath),
                Save(BuildExit(), ExitPath),
                Save(BuildRest(), RestPath),
                Save(BuildOverlay(), OverlayPath)
            };
            AssetDatabase.SaveAssets();
            AssignToScene(prefabs);
            Debug.Log("AnomalyPath: префабы модальных окон собраны");
        }

        private static GameObject BuildConfirm()
        {
            var root = CreateRoot("ConfirmDialog", Dark, out var backdrop);
            var dialog = root.AddComponent<ConfirmDialog>();
            var box = CreateColumn(root.transform, "Window", 700f);
            var boxBlocker = box.gameObject.AddComponent<Button>();
            boxBlocker.targetGraphic = box.GetComponent<Image>();
            var title = AddText(box, "Title", string.Empty, 40, UiFactory.TextColor, 70f);
            Localize(title, UiTextKey.Common_Confirm_Title);
            var message = AddText(box, "Message", string.Empty, 20, UiFactory.TextColor, 90f);
            var row = AddRow(box, "ConfirmButtons", 54f, 10f);
            var confirm = AddButton(row, "Confirm", Green, 22, UiTextKey.Common_Delete);
            var cancel = AddButton(row, "Cancel", UiFactory.ButtonColor, 22, UiTextKey.Common_Cancel);
            SetObject(dialog, "title", title);
            SetObject(dialog, "message", message);
            SetObject(dialog, "confirmLabel", confirm.GetComponentInChildren<Text>());
            SetObject(dialog, "confirmButton", confirm);
            SetObject(dialog, "cancelButton", cancel);
            SetObject(dialog, "backdropButton", backdrop);
            SetObject(dialog, "boxBlocker", boxBlocker);
            return root;
        }

        private static GameObject BuildExit()
        {
            var root = CreateRoot("ExitDialog", Dark, out var backdrop);
            var dialog = root.AddComponent<ExitDialog>();
            var box = CreateColumn(root.transform, "Window", 700f);
            var title = AddText(box, "Title", string.Empty, 40, UiFactory.TextColor, 70f);
            Localize(title, UiTextKey.Exit_Title);
            var body = AddText(box, "Body", string.Empty, 20, UiFactory.TextColor, 32f);
            Localize(body, UiTextKey.Exit_Body);
            var toMenu = AddButton(box, "ToMenu", UiFactory.ButtonColor, 22, UiTextKey.Exit_To_Menu);
            var toDesktop = AddButton(box, "ToDesktop", UiFactory.ButtonColor, 22, UiTextKey.Exit_To_Desktop);
            SetObject(dialog, "backdropButton", backdrop);
            SetObject(dialog, "toMenuButton", toMenu);
            SetObject(dialog, "toDesktopButton", toDesktop);
            return root;
        }

        private static GameObject BuildRest()
        {
            var root = CreateRoot("RestDialog", Dark, out var backdrop);
            var dialog = root.AddComponent<RestDialog>();
            var box = CreateColumn(root.transform, "Window", 780f);
            var title = AddText(box, "Title", string.Empty, 40, UiFactory.TextColor, 70f);
            Localize(title, UiTextKey.Rest_Title);
            var stamina = AddText(box, "RestStaminaText", string.Empty, 20, UiFactory.MutedTextColor, 32f);
            var slider = AddSlider(box);
            var hours = AddText(box, "RestHoursText", string.Empty, 20, UiFactory.TextColor, 32f);
            var recovery = AddText(box, "RestRecoveryText", string.Empty, 20, UiFactory.MutedTextColor, 32f);
            var risk = AddText(box, "RestRiskText", string.Empty, 20, UiFactory.MutedTextColor, 32f);
            var row = AddRow(box, "RestButtons", 54f, 10f);
            var confirm = AddButton(row, "Отдыхать", Green, 22, UiTextKey.Rest_Confirm);
            var cancel = AddButton(row, "Cancel", UiFactory.ButtonColor, 22, UiTextKey.Common_Cancel);
            SetObject(dialog, "staminaText", stamina);
            SetObject(dialog, "hoursSlider", slider);
            SetObject(dialog, "hoursText", hours);
            SetObject(dialog, "recoveryText", recovery);
            SetObject(dialog, "riskText", risk);
            SetObject(dialog, "confirmButton", confirm);
            SetObject(dialog, "cancelButton", cancel);
            SetObject(dialog, "backdropButton", backdrop);
            return root;
        }

        private static GameObject BuildOverlay()
        {
            var root = new GameObject("DialogOverlay", typeof(RectTransform));
            Stretch((RectTransform)root.transform);
            var blocker = CreateImage(root.transform, "Blocker", new Color(0f, 0f, 0f, 0f));
            Stretch(blocker.rectTransform);
            blocker.raycastTarget = true;
            var view = root.AddComponent<DialogOverlayView>();

            var menu = CreateWindow(root.transform, "DialogMenu", DialogMetrics.MenuButtonHeight);
            PlaceBottom(menu.rectTransform, 0f, DialogMetrics.WindowWidth,
                DialogMetrics.MenuHeightFor(3));
            var talkButton = AddMenuButton(menu.transform, "DialogTalk", "Поговорить", 0,
                UiTextKey.Dialog_Talk);
            var tradeButton = AddMenuButton(menu.transform, "DialogTrade", "Поторгуем?", 1,
                UiTextKey.Dialog_Trade);
            var farewellButton = AddMenuButton(menu.transform, "DialogFarewell", "Прощай", 2,
                UiTextKey.Dialog_Farewell);

            var talk = CreateWindow(root.transform, "DialogTalkWindow", DialogMetrics.TalkHeight);
            PlaceBottom(talk.rectTransform, 0f, DialogMetrics.WindowWidth, DialogMetrics.TalkHeight);
            var replyPanel = CreateImage(talk.transform, "DialogReplyPanel", Reply);
            PlaceTopLeft(replyPanel.rectTransform,
                DialogMetrics.WindowPadding, DialogMetrics.ReplyTop,
                DialogMetrics.WindowWidth - (2f * DialogMetrics.WindowPadding),
                DialogMetrics.ReplyHeight);
            var speaker = AddText(talk.transform, "DialogSpeaker", string.Empty,
                DialogMetrics.SpeakerFontSize, UiFactory.TextColor, DialogMetrics.SpeakerHeight);
            speaker.fontStyle = FontStyle.Bold;
            speaker.alignment = TextAnchor.MiddleLeft;
            PlaceTopLeft(speaker.rectTransform,
                DialogMetrics.WindowPadding + DialogMetrics.ReplyPadding,
                DialogMetrics.ReplyTop + DialogMetrics.ReplyPadding,
                DialogMetrics.WindowWidth - (2f * DialogMetrics.WindowPadding) - (2f * DialogMetrics.ReplyPadding),
                DialogMetrics.SpeakerHeight);
            var reply = AddText(talk.transform, "DialogReply", string.Empty,
                DialogMetrics.ReplyFontSize, UiFactory.TextColor, DialogMetrics.ReplyTextHeight);
            reply.alignment = TextAnchor.UpperLeft;
            reply.verticalOverflow = VerticalWrapMode.Truncate;
            PlaceTopLeft(reply.rectTransform,
                DialogMetrics.WindowPadding + DialogMetrics.ReplyPadding,
                DialogMetrics.ReplyTop + DialogMetrics.ReplyTextTop + DialogMetrics.SpeakerHeight,
                DialogMetrics.WindowWidth - (2f * DialogMetrics.WindowPadding) - (2f * DialogMetrics.ReplyPadding),
                DialogMetrics.ReplyTextHeight);
            var scroll = CreateScroll(talk.transform);
            var template = AddPhraseTemplate(scroll);

            SetObject(view, "blocker", blocker);
            SetObject(view, "menu", menu.rectTransform);
            SetObject(view, "talk", talk.rectTransform);
            SetObject(view, "talkButton", talkButton);
            SetObject(view, "tradeButton", tradeButton);
            SetObject(view, "farewellButton", farewellButton);
            SetObject(view, "speaker", speaker);
            SetObject(view, "reply", reply);
            SetObject(view, "phrases", scroll);
            SetObject(view, "phraseTemplate", template);
            return root;
        }
    }
}
