#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Dialog;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Game.Hub
{
    /// <summary>
    /// Диалог торговца в комнате: меню разделов и разговор по данным актёра.
    /// Меню открывается при входе в комнату; «Поговорить» начинает диалог из
    /// <c>dialogId</c>, «Поторгуем?» открывает окно торговли с возвратом сюда,
    /// «Прощай» выходит в большой хаб, как дверь.
    /// </summary>
    public sealed class TraderDialogOverlay
    {
        /// <summary>Имя корневого объекта окна диалога в иерархии.</summary>
        public const string RootName = "TraderDialog";

        private readonly IUiHost _host;
        private readonly DialogOverlayView _view;
        private string? _actorId;
        private string _dialogId = string.Empty;
        private string _nodeId = string.Empty;

        /// <summary>Создаёт диалог скрытым.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="parent">Родитель, обычно контент комнаты торговца.</param>
        public TraderDialogOverlay(IUiHost host, Transform parent)
        {
            _host = host;
            _view = host.CreateDialogOverlay(
                parent,
                RootName,
                new[]
                {
                    new DialogOverlayView.MenuEntry(
                        DialogOverlayView.MenuTalk,
                        UiTextKey.Dialog_Talk,
                        OpenConversation),
                    new DialogOverlayView.MenuEntry(
                        DialogOverlayView.MenuTrade,
                        UiTextKey.Dialog_Trade,
                        OpenTrade),
                    new DialogOverlayView.MenuEntry(
                        DialogOverlayView.MenuFarewell,
                        UiTextKey.Dialog_Farewell,
                        LeaveRoom)
                },
                Choose,
                blockClicks: false);
        }

        /// <summary>Представление окна диалога: нужно тестам и кадрам.</summary>
        public DialogOverlayView View => _view;

        /// <summary>Показано ли окно диалога.</summary>
        public bool IsShown => _view.IsShown;

        /// <summary>Показывает меню разделов для торговца.</summary>
        /// <param name="actorId">Торговец или <c>null</c>, чтобы скрыть окно.</param>
        public void ShowMenu(string? actorId)
        {
            _actorId = actorId;
            _dialogId = string.Empty;
            _nodeId = string.Empty;

            var session = _host.Session;
            if (actorId == null || session == null || !_host.Actors.Contains(actorId))
            {
                _view.Hide();
                return;
            }

            var actor = _host.Actors.Get(actorId);
            var canTalk = actor.DialogId != null && _host.Dialogs.CanStart(session, actor.DialogId);
            var canTrade = _host.Trade.IsAvailable(session, actorId);
            var visible = new List<string>(3);
            if (canTalk)
            {
                visible.Add("DialogTalk");
            }

            if (canTrade)
            {
                visible.Add("DialogTrade");
            }

            visible.Add("DialogFarewell");
            _view.ShowMenu(visible);
        }

        /// <summary>Скрывает диалог, оставляя комнату торговца на экране.</summary>
        public void Hide() => _view.Hide();

        /// <summary>Возвращает из окна разговора в меню разделов.</summary>
        /// <returns><c>true</c>, если был открыт разговор и возврат выполнен.</returns>
        public bool Back()
        {
            if (!_view.TalkShown)
            {
                return false;
            }

            ShowMenu(_actorId);
            return true;
        }

        private void OpenConversation()
        {
            var session = _host.Session;
            if (_actorId == null || session == null || !_host.Actors.Contains(_actorId))
            {
                return;
            }

            var dialogId = _host.Actors.Get(_actorId).DialogId;
            if (dialogId == null)
            {
                return;
            }

            var view = _host.Dialogs.Start(session, dialogId, _actorId);
            if (view == null)
            {
                ToastFailed();
                return;
            }

            _dialogId = view.DialogId;
            _nodeId = view.NodeId;
            _view.ShowTalk(
                SpeakerName(view.SpeakerActorId),
                _host.Texts.Get(view.TextKey),
                view.Options);
        }

        private void Choose(string optionId)
        {
            var session = _host.Session;
            if (session == null || _dialogId.Length == 0)
            {
                return;
            }

            var result = _host.Dialogs.Choose(session, _dialogId, _nodeId, optionId);
            if (!result.Succeeded)
            {
                ToastFailed();
                return;
            }

            var next = result.Ended ? null : result.View;
            if (HasRewards(result))
            {
                _view.Hide();
                _host.ShowRewards(result.Loot, result.Money, RewardSource.Quest, () => Continue(next));
                return;
            }

            if (result.QuestId != null)
            {
                _host.Toast(UiText.Get(
                    _host.Texts,
                    result.Ended ? UiTextKey.Dialog_Quest_Completed : UiTextKey.Dialog_Quest_Taken));
            }

            if (next == null)
            {
                ShowMenu(_actorId);
                return;
            }

            ApplyNode(next);
        }

        private static bool HasRewards(DialogResult result) =>
            result.Loot.Count > 0 || result.Money > 0;

        /// <summary>Возвращает в комнату после экрана наград и продолжает разговор, если он не закрыт.</summary>
        private void Continue(DialogView? next)
        {
            if (_actorId == null)
            {
                _host.ShowGame(false);
                return;
            }

            _host.ShowTraderRoom(_actorId);
            if (next != null)
            {
                ApplyNode(next);
            }
        }

        private void ApplyNode(DialogView view)
        {
            _dialogId = view.DialogId;
            _nodeId = view.NodeId;
            _view.ShowTalk(
                SpeakerName(view.SpeakerActorId),
                _host.Texts.Get(view.TextKey),
                view.Options);
        }

        private void OpenTrade()
        {
            if (_actorId == null)
            {
                return;
            }

            _host.ShowTrade(_actorId, returnToDialog: true);
        }

        private void LeaveRoom()
        {
            _view.Hide();
            _host.ShowGame(false);
        }

        private string SpeakerName(string? actorId)
        {
            var id = actorId ?? _actorId;
            if (id == null || !_host.Actors.Contains(id))
            {
                return string.Empty;
            }

            return _host.Texts.Get(_host.Actors.Get(id).NameKey);
        }

        private void ToastFailed() =>
            _host.Toast(UiText.Get(_host.Texts, UiTextKey.Dialog_Failed));
    }
}
