#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Dialog;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Диалоги: показ узла с учётом условий, выбор варианта и последствия.
    /// Выдача и сдача сюжетного квеста идут через <see cref="IStoryQuestService"/>.
    /// </summary>
    public sealed class DialogService : IDialogService
    {
        private readonly IDialogRepository _dialogs;
        private readonly IStoryQuestService _story;
        private readonly IItemRepository _items;
        private readonly IInventoryService _inventory;

        /// <summary>Создаёт сервис диалогов.</summary>
        /// <param name="dialogs">Репозиторий диалогов.</param>
        /// <param name="story">Сюжетные квесты: выдача и сдача из диалога.</param>
        /// <param name="items">Шаблоны предметов для последствий.</param>
        /// <param name="inventory">Инвентарь: выдача предметов и проверка места.</param>
        /// <exception cref="ArgumentNullException">Зависимость не задана.</exception>
        public DialogService(
            IDialogRepository dialogs,
            IStoryQuestService story,
            IItemRepository items,
            IInventoryService inventory)
        {
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            _story = story ?? throw new ArgumentNullException(nameof(story));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        /// <inheritdoc />
        public bool CanStart(GameSession session, string dialogId)
        {
            var player = session?.State.Player;
            if (session == null || player == null || !_dialogs.TryGet(dialogId, out var dialog))
            {
                return false;
            }

            var ready = ReadyCheck(session);
            return dialog.Requirements.IsMet(player, ready) &&
                   dialog.TryGetNode(dialog.StartNodeId, out var start) &&
                   start != null &&
                   start.Requirements.IsMet(player, ready);
        }

        /// <inheritdoc />
        public DialogView? Start(GameSession session, string dialogId, string? actorId = null)
        {
            if (!CanStart(session, dialogId) ||
                !_dialogs.TryGet(dialogId, out var dialog) ||
                !dialog.TryGetNode(dialog.StartNodeId, out var start) ||
                start == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(actorId))
            {
                _story.NotifyTalkedToActor(session!, actorId!);
            }

            return BuildView(session, dialog, start, actorId);        }

        /// <inheritdoc />
        public DialogView? Node(GameSession session, string dialogId, string nodeId)
        {
            if (session?.State.Player == null ||
                !_dialogs.TryGet(dialogId, out var dialog) ||
                !dialog.TryGetNode(nodeId, out var node) ||
                node == null)
            {
                return null;
            }

            return BuildView(session, dialog, node, null);
        }

        /// <inheritdoc />
        public DialogResult Choose(
            GameSession session,
            string dialogId,
            string nodeId,
            string optionId)
        {
            var player = session?.State.Player;
            if (player == null)
            {
                return DialogResult.Error(DialogResultStatus.DialogNotFound);
            }

            if (!_dialogs.TryGet(dialogId, out var dialog))
            {
                return DialogResult.Error(DialogResultStatus.DialogNotFound);
            }

            var ready = ReadyCheck(session!);
            if (!dialog.Requirements.IsMet(player, ready))
            {
                return DialogResult.Error(DialogResultStatus.Unavailable);
            }

            if (!dialog.TryGetNode(nodeId, out var node) || node == null)
            {
                return DialogResult.Error(DialogResultStatus.NodeNotFound);
            }

            if (!node.Requirements.IsMet(player, ready))
            {
                return DialogResult.Error(DialogResultStatus.Unavailable);
            }

            if (!node.TryGetOption(optionId, out var option) || option == null)
            {
                return DialogResult.Error(DialogResultStatus.OptionNotFound);
            }

            if (!option.Requirements.IsMet(player, ready))
            {
                return DialogResult.Error(DialogResultStatus.Unavailable);
            }

            string? questId = null;
            var money = 0;
            var loot = new List<ItemInstance>();
            foreach (var effect in option.Effects)
            {
                var failure = ApplyEffect(
                    session!,
                    player,
                    effect,
                    ref questId,
                    ref money,
                    loot);
                if (failure != null)
                {
                    return failure;
                }
            }

            if (option.End)
            {
                return DialogResult.Closed(questId, money, loot);
            }

            if (option.NextNodeId == null ||
                !dialog.TryGetNode(option.NextNodeId, out var next) ||
                next == null)
            {
                return DialogResult.Error(DialogResultStatus.NodeNotFound, questId);
            }

            var view = BuildView(session!, dialog, next, null);
            return view == null
                ? DialogResult.Error(DialogResultStatus.Unavailable, questId)
                : DialogResult.Next(view, questId, money, loot);
        }

        private DialogResult? ApplyEffect(
            GameSession session,
            PlayerState player,
            DialogEffect effect,
            ref string? questId,
            ref int money,
            List<ItemInstance> loot)
        {
            switch (effect.Type)
            {
                case DialogEffectType.Quest:
                {
                    var id = effect.QuestId!;
                    var result = effect.QuestAction == DialogQuestAction.TurnIn
                        ? _story.TurnIn(session, id)
                        : _story.Accept(session, id, effect.VariantId);
                    if (!result.Succeeded)
                    {
                        return DialogResult.Error(DialogResultStatus.QuestFailed, id);
                    }

                    questId = id;
                    money += result.Money;
                    foreach (var item in result.Loot)
                    {
                        loot.Add(item);
                    }

                    return null;
                }

                case DialogEffectType.Item:
                {
                    var itemId = effect.ItemId!;
                    if (!_items.TryGet(itemId, out var template) ||
                        !CanFit(player, template, effect.Count))
                    {
                        return DialogResult.Error(DialogResultStatus.NoRoom);
                    }

                    // Подарок по диалогу выдаётся сразу: это не награда за квест, окно ему не нужно.
                    _inventory.TryAdd(
                        player.Inventory,
                        itemId,
                        effect.Count,
                        ItemInstances.NewDurability(template),
                        null,
                        out _);
                    return null;
                }

                case DialogEffectType.Unlock:
                    player.AddUnlock(effect.UnlockId!);
                    return null;

                default:
                    return DialogResult.Error(DialogResultStatus.Unavailable);
            }
        }

        private bool CanFit(PlayerState player, ItemTemplate template, int count)
        {
            var probe = ItemInstances.CreateProbe("dialog-probe", template, count);
            return _inventory.CanFit(player.Inventory, new[] { probe });
        }

        private DialogView? BuildView(
            GameSession session,
            DialogTemplate dialog,
            DialogNode node,
            string? actorId)
        {
            var player = session.State.Player;
            if (player == null)
            {
                return null;
            }

            var ready = ReadyCheck(session);
            if (!node.Requirements.IsMet(player, ready))
            {
                return null;
            }

            var options = new List<DialogOptionView>();
            foreach (var option in node.Options)
            {
                var available = option.Requirements.IsMet(player, ready);
                if (!available && option.HiddenIfUnavailable)
                {
                    continue;
                }

                options.Add(new DialogOptionView(option.Id, option.TextKey, available));
            }

            return new DialogView(
                dialog.Id,
                node.Id,
                node.SpeakerActorId ?? actorId,
                node.TextKey,
                options);
        }

        private Func<string, bool> ReadyCheck(GameSession session) => questId =>
            _story.TryGetActive(session, questId, out var state) && _story.IsReady(session, state);
    }
}
