#nullable enable
using System;
using AnomalyPath.Core.Dialog;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Dialogs
{
    /// <summary>
    /// Диалог с NPC вне комнаты торговца: модальная накладка на игровой экран, как у торговца.
    /// Меню показывает одну полосу «Поговорить», разговор идёт по данным <c>dialogId</c>.
    /// Завершающая фраза закрывает окно и уводит на следующую локацию (что зовёт
    /// <see cref="_onEnded"/>), Esc возвращает на текущую локацию, где стоит NPC.
    /// </summary>
    public sealed class NpcDialogOverlay
    {
        /// <summary>Имя корневого объекта окна диалога в иерархии.</summary>
        public const string RootName = "NpcDialog";

        private readonly IUiHost _host;
        private readonly DialogOverlayView _view;
        private readonly Action? _onEnded;
        private string _actorId = string.Empty;
        private string _dialogId = string.Empty;
        private string _nodeId = string.Empty;

        /// <summary>Создаёт диалог скрытым.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="parent">Родитель, обычно канвас.</param>
        /// <param name="onEnded">Что выполнить после завершающей фразы; <c>null</c> — ничего.</param>
        public NpcDialogOverlay(IUiHost host, Transform parent, Action? onEnded = null)
        {
            _host = host;
            _onEnded = onEnded;
            _view = host.CreateDialogOverlay(
                parent,
                RootName,
                new[]
                {
                    new DialogOverlayView.MenuEntry(
                        DialogOverlayView.MenuTalk,
                        UiTextKey.Dialog_Talk,
                        OpenConversation)
                },
                Choose,
                blockClicks: true);
        }

        /// <summary>Представление окна диалога: нужно тестам.</summary>
        public DialogOverlayView View => _view;

        /// <summary>Показано ли окно диалога.</summary>
        public bool IsShown => _view.IsShown;

        /// <summary>Показывает меню диалога для NPC.</summary>
        /// <param name="actorId">С кем говорят.</param>
        /// <param name="dialogId">Идентификатор диалога.</param>
        public void Show(string actorId, string dialogId)
        {
            _actorId = actorId;
            _dialogId = dialogId;
            _nodeId = string.Empty;

            var session = _host.Session;
            if (session == null ||
                string.IsNullOrWhiteSpace(dialogId) ||
                !_host.Dialogs.CanStart(session, dialogId))
            {
                _view.Hide();
                ToastFailed();
                return;
            }

            _view.ShowMenu(new[] { "DialogTalk" });
        }

        /// <summary>Скрывает диалог, оставляя текущий экран на месте.</summary>
        public void Hide() => _view.Hide();

        /// <summary>Esc закрывает диалог и возвращает на текущую локацию.</summary>
        /// <returns><c>true</c>, если диалог был открыт.</returns>
        public bool HandleEscape()
        {
            if (!_view.IsShown)
            {
                return false;
            }

            _view.Hide();
            if (_host.Session != null)
            {
                _host.ShowGame(false);
            }

            return true;
        }

        private void OpenConversation()
        {
            var session = _host.Session;
            if (session == null || _dialogId.Length == 0)
            {
                return;
            }

            var view = _host.Dialogs.Start(session, _dialogId, _actorId);
            if (view == null)
            {
                ToastFailed();
                return;
            }

            Apply(view);
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
                End();
                return;
            }

            Apply(next);
        }

        private static bool HasRewards(DialogResult result) =>
            result.Loot.Count > 0 || result.Money > 0;

        /// <summary>Продолжает разговор после экрана наград: награда за квест забрана или потеряна.</summary>
        private void Continue(DialogView? next)
        {
            if (next == null)
            {
                End();
                return;
            }

            _host.ShowGame(false);
            Apply(next);
        }

        private void Apply(DialogView view)
        {
            _dialogId = view.DialogId;
            _nodeId = view.NodeId;
            _view.ShowTalk(
                SpeakerName(view.SpeakerActorId),
                _host.Texts.Get(view.TextKey),
                view.Options);
        }

        private void End()
        {
            _view.Hide();
            _onEnded?.Invoke();
        }

        private string SpeakerName(string? actorId)
        {
            var id = string.IsNullOrWhiteSpace(actorId) ? _actorId : actorId;
            if (string.IsNullOrWhiteSpace(id) || !_host.Actors.Contains(id!))
            {
                return string.Empty;
            }

            return _host.Texts.Get(_host.Actors.Get(id!).NameKey);
        }

        private void ToastFailed() =>
            _host.Toast(UiText.Get(_host.Texts, UiTextKey.Dialog_Failed));
    }
}
