#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Доска объявлений: слоты, таймеры, взятие и отказ, цели, прогресс, сдача и сейв.</summary>
    public sealed partial class QuestTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов квестов.</summary>
        /// <param name="dataRoot">Корень данных.</param>
        public QuestTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var quests = services.Resolve<IQuestService>();
            var repository = services.Resolve<IQuestRepository>();
            var inventory = services.Resolve<IInventoryService>();

            Check("квесты: шаблоны загружены и разложены по сложности", () =>
            {
                Equal(6, repository.All.Count, "квестов");
                var byDifficulty = new Dictionary<QuestDifficulty, int>();
                foreach (var quest in repository.All)
                {
                    Equal(QuestKind.Repeatable, quest.QuestKind, "вид квеста");
                    byDifficulty.TryGetValue(quest.Difficulty, out var count);
                    byDifficulty[quest.Difficulty] = count + 1;
                }

                Equal(2, byDifficulty[QuestDifficulty.Easy], "простых шаблонов");
                Equal(2, byDifficulty[QuestDifficulty.Medium], "средних шаблонов");
                Equal(2, byDifficulty[QuestDifficulty.Hard], "сложных шаблонов");
                True(quests.TryGetTemplate("qst_clear_house", out var template), "шаблон для экрана");
                Equal(QuestType.ClearLocation, template.Type, "тип шаблона");
            });

            Check("квесты: на доске шесть слотов с таймерами 24/48/72", () =>
            {
                var session = NewSession(services);
                var board = quests.GetBoard(session, BoardId(session));
                Equal(6, board.Count, "слотов");
                var total = 0;
                foreach (var slot in board)
                {
                    Equal(0.0, slot.AvailableAt, "слот открыт");
                    True(slot.Offer != null, "в слоте есть предложение");
                    total++;
                    if (slot.Difficulty == QuestDifficulty.Easy)
                    {
                        Equal(24, slot.RefreshHours, "таймер простого");
                    }
                    else if (slot.Difficulty == QuestDifficulty.Medium)
                    {
                        Equal(48, slot.RefreshHours, "таймер среднего");
                    }
                    else
                    {
                        Equal(72, slot.RefreshHours, "таймер сложного");
                    }
                }

                Equal(6, total, "предложений");
            });

            Check("квесты: повторное открытие доски не меняет предложения", () =>
            {
                var session = NewSession(services);
                var board = quests.GetBoard(session, BoardId(session));
                var again = quests.GetBoard(session, BoardId(session));
                Equal(board.Count, again.Count, "слотов");
                for (var index = 0; index < board.Count; index++)
                {
                    True(board[index].Offer != null, "предложение");
                    True(again[index].Offer != null, "предложение повторно");
                    Equal(board[index].Offer!.QuestId, again[index].Offer!.QuestId, "набор стабилен");
                }
            });

            Check("квесты: доска доступна только в хабе", () =>
            {
                var session = NewSession(services);
                var map = session.State.World.Map;
                var hubId = session.State.World.CurrentLocationId;
                var edge = map.EdgesFrom(hubId)[0];
                session.State.World.MoveTo(edge.ToId);
                Equal(0, quests.GetBoard(session, hubId).Count, "доска недоступна на пути");
            });

            Check("квесты: взятие запускает таймер слота", () =>
            {
                var session = NewSession(services);
                var boardId = BoardId(session);
                var slot = RequireSlot(quests, session, QuestDifficulty.Easy);
                var other = RequireSlot(quests, session, QuestDifficulty.Easy, slot.Index);
                var state = Accept(quests, session, slot);
                Equal(boardId, state.BoardLocationId, "доска взятия");
                Equal(slot.Index, state.Slot, "слот взятия");
                Equal(1, quests.GetActive(session).Count, "активных");
                True(quests.Decline(session, boardId, other.Index).Succeeded, "отказ от соседнего слота");
                var after = quests.GetBoard(session, boardId)[slot.Index];
                True(after.Offer == null, "слот освобождён");
                True(after.IsLocked(session.State.World.Clock.AbsoluteHours), "слот на таймере");
                Equal(24.0, after.HoursLeft(session.State.World.Clock.AbsoluteHours), "таймер 24 часа");
                session.State.World.Clock.Advance(23);
                True(quests.GetBoard(session, boardId)[slot.Index].IsLocked(
                    session.State.World.Clock.AbsoluteHours), "до срока закрыт");
                session.State.World.Clock.Advance(1);
                var refreshedBoard = quests.GetBoard(session, boardId);
                True(!refreshedBoard[slot.Index].IsLocked(session.State.World.Clock.AbsoluteHours), "слот открылся");
                var easyOffers = 0;
                foreach (var refreshed in refreshedBoard)
                {
                    if (refreshed.Difficulty == QuestDifficulty.Easy && refreshed.Offer != null)
                    {
                        easyOffers++;
                    }
                }

                True(easyOffers > 0, "новое предложение из пула");
            });

            Check("квесты: отказ запускает таймер и отменяет предложение", () =>
            {
                var session = NewSession(services);
                var boardId = BoardId(session);
                var slot = RequireSlot(quests, session, QuestDifficulty.Medium);
                True(quests.Decline(session, boardId, slot.Index).Succeeded, "отказ");
                Equal(QuestStatus.Cancelled, slot.Offer!.State.Status, "предложение отменено");
                var after = quests.GetBoard(session, boardId)[slot.Index];
                True(after.Offer == null, "слот пуст");
                Equal(48.0, after.HoursLeft(session.State.World.Clock.AbsoluteHours), "таймер 48 часов");
                session.State.World.Clock.Advance(48);
                True(quests.GetBoard(session, boardId)[slot.Index].Offer != null, "новое предложение");
            });

            Check("квесты: зачистка добавляет локацию в маршрут и сдаётся на доске", () =>
            {
                var session = NewSession(services);
                var boardId = BoardId(session);
                var slot = RequireSlot(quests, session, QuestType.ClearLocation);
                var state = Accept(quests, session, slot);
                Equal(slot.Offer!.Template.Id, state.QuestId, "шаблон");
                Equal(slot.Offer.State.LocationTemplateId, state.LocationTemplateId, "шаблон цели");
                True(state.LocationInstanceId != null, "экземпляр цели");
                var map = session.State.World.Map;
                True(map.TryGetLocation(state.LocationInstanceId!, out var target), "локация на карте");
                Equal(state.LocationTemplateId, target!.TemplateId, "шаблон экземпляра");
                True(HasPath(session, state.LocationInstanceId!), "маршрут от хаба");

                quests.NotifyLocationCleared(session, state.LocationInstanceId!);
                True(quests.GetProgress(session, state).Ready, "цель выполнена");
                var money = session.State.Player!.Money;
                True(quests.TurnIn(session, boardId, state.QuestId).Succeeded, "сдача");
                Equal(money + slot.Offer.Template.Rewards.Money, session.State.Player.Money, "награда");
                Equal(QuestStatus.Completed, state.Status, "статус");
                True(session.State.Player.HasCompletedQuest(state.QuestId), "квест засчитан");
            });

            Check("квесты: сдать можно только на доске, где взят", () =>
            {
                var session = NewSession(services);
                var hubId = BoardId(session);
                var slot = RequireSlot(quests, session, QuestType.ClearLocation);
                var state = Accept(quests, session, slot);
                quests.NotifyLocationCleared(session, state.LocationInstanceId!);

                var map = session.State.World.Map;
                var secondHub = new LocationInstance(
                    "locinst_second_hub",
                    "loc_hub_settlement",
                    1,
                    "bg_hub_settlement",
                    LocationSubtype.Hub,
                    0,
                    0);
                map.AddLocation(secondHub);
                session.State.World.MoveTo(secondHub.InstanceId);
                Equal(
                    QuestResultStatus.WrongBoard,
                    quests.TurnIn(session, secondHub.InstanceId, state.QuestId).Status,
                    "чужая доска");
                session.State.World.MoveTo(hubId);
                True(quests.TurnIn(session, hubId, state.QuestId).Succeeded, "своя доска");
            });

            Check("квесты: убийство считает только целевых мутантов", () =>
            {
                var session = NewSession(services);
                var slot = RequireSlot(quests, session, QuestType.KillMutants);
                var state = Accept(quests, session, slot);
                Equal(2, state.ActorIds.Count, "размер пула");
                Equal(3, state.RequiredCount, "нужно убить");
                quests.NotifyActorKilled(session, "enemy_bandit");
                Equal(0, state.KillCount, "чужой не считается");
                for (var index = 0; index < state.RequiredCount; index++)
                {
                    quests.NotifyActorKilled(session, state.ActorIds[0]);
                }

                True(quests.GetProgress(session, state).Ready, "целей хватает");
                True(quests.TurnIn(session, BoardId(session), state.QuestId).Succeeded, "сдача");
            });

            Check("квесты: артефакты сдаются из инвентаря", () =>
            {
                var session = NewSession(services);
                var slot = RequireSlot(quests, session, QuestType.GatherArtifacts);
                var state = Accept(quests, session, slot);
                Equal(2, state.RequiredCount, "нужно артефактов");
                foreach (var target in state.Items)
                {
                    inventory.TryAdd(session.State.Player!.Inventory, target.ItemId, 1, out _);
                }

                var progress = quests.GetProgress(session, state);
                True(progress.Ready, "артефактов хватает");
                Equal(2, progress.Current, "собрано");
                var money = session.State.Player!.Money;
                True(quests.TurnIn(session, BoardId(session), state.QuestId).Succeeded, "сдача");
                Equal(money + 900, session.State.Player.Money, "награда сложного");
                foreach (var target in state.Items)
                {
                    Equal(0, CountItem(session.State.Player, target.ItemId), "артефакт изъят");
                }
            });

            Check("квесты: оружие и броня принимаются в любом состоянии", () =>
            {
                var session = NewSession(services);
                var slot = RequireSlot(quests, session, QuestType.FindItems);
                var state = Accept(quests, session, slot);
                Equal(1, state.Items.Count, "видов предметов");
                var itemId = state.Items[0].ItemId;
                inventory.TryAdd(
                    session.State.Player!.Inventory,
                    itemId,
                    state.Items[0].Count,
                    0,
                    "broken",
                    out _);
                for (var index = 0; index < state.RequiredCount; index++)
                {
                    quests.NotifyActorKilled(session, state.ActorIds[0]);
                }

                True(quests.GetProgress(session, state).Ready, "битый предмет принят");
                var money = session.State.Player.Money;
                True(quests.TurnIn(session, BoardId(session), state.QuestId).Succeeded, "сдача");
                Equal(money + slot.Offer!.Template.Rewards.Money, session.State.Player.Money, "награда");
                Equal(0, CountItem(session.State.Player, itemId), "предмет изъят");
            });

            Check("награда квеста: предметы готовятся как лут и остаются вне инвентаря", () =>
            {
                var items = services.Resolve<IItemRepository>();
                var rewards = new QuestRewardTemplate(
                    0,
                    new[]
                    {
                        new QuestItemTarget("wpn_pm", 1),
                        new QuestItemTarget("ammo_545", 30)
                    },
                    Array.Empty<string>());

                var loot = QuestRewardLoot.Build("quest-reward:test", rewards, items);

                Equal(2, loot.Count, "предметов награды");
                Equal("wpn_pm", loot[0].ItemId, "первый предмет");
                Equal(30, loot[1].Stack, "патронов в награде");
                Equal(100, loot[0].Durability ?? -1, "прочность новой вещи — максимум шаблона");
                True(
                    !string.Equals(loot[0].InstanceId, loot[1].InstanceId, StringComparison.Ordinal),
                    "id экземпляров различаются");
                Equal(0, QuestRewardLoot.Build("empty", QuestRewardTemplate.Empty, items).Count, "пустая награда");
            });

            Check("квесты: сдача отдаёт деньги в результате, а награду — экрану наград", () =>
            {
                var session = NewSession(services);
                var slot = RequireSlot(quests, session, QuestType.ClearLocation);
                var state = Accept(quests, session, slot);
                quests.NotifyLocationCleared(session, state.LocationInstanceId!);

                var result = quests.TurnIn(session, BoardId(session), state.QuestId);

                True(result.Succeeded, "сдача");
                Equal(slot.Offer!.Template.Rewards.Money, result.Money, "деньги в результате");
                Equal(0, result.Loot.Count, "у простого квеста нет предметов награды");
            });

            Check("квесты: предел активных", () =>
            {
                var session = NewSession(services);
                for (var index = 0; index < 3; index++)
                {
                    var slot = FirstOffer(quests, session);
                    True(Accept(quests, session, slot) != null, "взятие " + index);
                }

                var fourth = FirstOffer(quests, session);
                Equal(
                    QuestResultStatus.ActiveLimit,
                    quests.Accept(session, BoardId(session), fourth.Index).Status,
                    "четвёртый не берётся");
            });

            Check("квесты: активный квест можно отменить", () =>
            {
                var session = NewSession(services);
                var slot = FirstOffer(quests, session);
                var state = Accept(quests, session, slot);
                True(quests.CancelQuest(session, state.QuestId).Succeeded, "отмена");
                Equal(0, quests.GetActive(session).Count, "активных после отмены");
                Equal(QuestStatus.Cancelled, state.Status, "статус");
            });

            Check("квесты: истёкший срок проваливает квест «на время»", () =>
            {
                var session = NewSession(services);
                var slot = RequireConditionSlot(quests, session, QuestFailCondition.Time);
                var state = Accept(quests, session, slot);
                var limit = slot.Offer!.Template.TimeLimitHours;
                True(limit > 0, "срок задан в данных");

                session.State.World.Clock.Advance(limit - 1);
                Equal(0, quests.Expire(session), "до срока не проваливается");
                Equal(QuestStatus.Active, state.Status, "квест активен");

                session.State.World.Clock.Advance(1);
                Equal(1, quests.Expire(session), "провален по сроку");
                Equal(QuestStatus.Failed, state.Status, "статус провала");
                True(state.IsOnCooldown(session.State.World.Clock.AbsoluteHours), "откат идёт");
                Equal(0, quests.GetActive(session).Count, "активных нет");
            });

            Check("квесты: остаток срока считается от момента взятия", () =>
            {
                var session = NewSession(services);
                var slot = RequireConditionSlot(quests, session, QuestFailCondition.Time);
                var state = Accept(quests, session, slot);
                var limit = slot.Offer!.Template.TimeLimitHours;
                var accepted = session.State.World.Clock.AbsoluteHours;

                True(Math.Abs(state.HoursLeft(accepted, limit) - limit) < 1e-6, "в момент взятия — весь срок");
                session.State.World.Clock.Advance(3);
                True(
                    Math.Abs(state.HoursLeft(session.State.World.Clock.AbsoluteHours, limit) - (limit - 3)) < 1e-6,
                    "через три часа — на три меньше");
                True(Math.Abs(state.HoursLeft(accepted + limit + 10, limit)) < 1e-6, "просроченный срок — ноль");
                True(Math.Abs(state.HoursLeft(accepted, 0)) < 1e-6, "без срока — ноль");
            });

            Check("квесты: смерть проваливает квест «за жизнь»", () =>
            {
                var session = NewSession(services);
                var slot = RequireConditionSlot(quests, session, QuestFailCondition.Death);
                var state = Accept(quests, session, slot);

                quests.NotifyPlayerDeath(session);

                Equal(QuestStatus.Failed, state.Status, "статус провала");
                True(state.IsOnCooldown(session.State.World.Clock.AbsoluteHours), "откат идёт");
                Equal(0, quests.GetActive(session).Count, "активных нет");
            });

            Check("квесты: смерть не проваливает квест «на время»", () =>
            {
                var session = NewSession(services);
                var slot = RequireConditionSlot(quests, session, QuestFailCondition.Time);
                var state = Accept(quests, session, slot);

                quests.NotifyPlayerDeath(session);

                Equal(QuestStatus.Active, state.Status, "квест остался активным");
                Equal(1, quests.GetActive(session).Count, "активных");
            });

            Check("квесты: предложения, слоты и цель переживают сейв", () =>
            {
                WithSaveRoot(root =>
                {
                    var session = NewSession(services);
                    var boardId = BoardId(session);
                    var slot = RequireSlot(quests, session, QuestDifficulty.Hard);
                    var state = Accept(quests, session, slot);
                    var service = new SaveService(
                        new FileSaveStorage(root),
                        services.Resolve<IItemRepository>());
                    service.Save(
                        session.State,
                        "slot_1",
                        0,
                        new DateTimeOffset(2026, 10, 24, 10, 0, 0, TimeSpan.Zero));

                    var loaded = service.Load("slot_1");
                    True(loaded.TryGetActiveQuest(state.QuestId, out var restored), "активный квест в сейве");
                    Equal(boardId, restored!.BoardLocationId, "доска в сейве");
                    Equal(slot.Index, restored.Slot, "слот в сейве");
                    True(loaded.World.Map.TryGetLocation(restored.LocationInstanceId ?? boardId, out _), "локация в сейве");
                    True(loaded.TryGetQuestBoard(boardId, out var boardState), "доска в сейве");
                    Equal(6, boardState!.Slots.Count, "слотов в сейве");
                    Equal(72.0, boardState.GetSlot(slot.Index).AvailableAt - state.AcceptedAt, "таймер в сейве");
                    var loadedSession = new GameSession(loaded);
                    Equal(6, quests.GetBoard(loadedSession, boardId).Count, "доска после загрузки");
                });
            });
        }

    }
}
