#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Dialog;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Tests
{
    /// <summary>Категория «диалоги»: разбор, показ узлов, последствия и условия.</summary>
    public sealed class DialogTests : DataTestBase
    {
        private const string Dialog =
            "{\"schemaVersion\":1,\"kind\":\"dialogs\",\"id\":\"dlg_test\"," +
            "\"startNodeId\":\"n1\",\"nodes\":[" +
            "{\"id\":\"n1\",\"speakerActorId\":\"npc_giver\",\"textKey\":\"t1\",\"options\":[" +
            "{\"id\":\"o_quest\",\"textKey\":\"a\",\"effects\":[{\"type\":\"quest\"," +
            "\"action\":\"give\",\"questId\":\"qst_dlg\"}],\"nextNodeId\":\"n2\"}," +
            "{\"id\":\"o_hidden\",\"textKey\":\"b\",\"requirements\":{\"questStates\":[" +
            "{\"questId\":\"qst_dlg\",\"state\":\"active\"}]},\"hiddenIfUnavailable\":true," +
            "\"nextNodeId\":\"n2\"}," +
            "{\"id\":\"o_unavail\",\"textKey\":\"c\",\"requirements\":{\"questStates\":[" +
            "{\"questId\":\"qst_missing\",\"state\":\"completed\"}]},\"nextNodeId\":\"n2\"}]}," +
            "{\"id\":\"n2\",\"textKey\":\"t2\",\"options\":[" +
            "{\"id\":\"o_item\",\"textKey\":\"d\",\"effects\":[{\"type\":\"item\"," +
            "\"itemId\":\"ammo_545\",\"count\":3}],\"nextNodeId\":\"n3\"}]}," +
            "{\"id\":\"n3\",\"textKey\":\"t3\",\"options\":[" +
            "{\"id\":\"o_turnin\",\"textKey\":\"f\",\"effects\":[{\"type\":\"quest\"," +
            "\"action\":\"turnIn\",\"questId\":\"qst_dlg\"}],\"end\":true}," +
            "{\"id\":\"o_unlock\",\"textKey\":\"e\",\"effects\":[{\"type\":\"unlock\"," +
            "\"unlockId\":\"unlock_test\"}],\"end\":true}," +
            "{\"id\":\"o_close\",\"textKey\":\"g\",\"end\":true}]}]}";

        private const string LockedDialog =
            "{\"schemaVersion\":1,\"kind\":\"dialogs\",\"id\":\"dlg_locked\"," +
            "\"requirements\":{\"questStates\":[{\"questId\":\"qst_missing\"," +
            "\"state\":\"completed\"}]},\"startNodeId\":\"n1\",\"nodes\":[" +
            "{\"id\":\"n1\",\"textKey\":\"t\",\"options\":[{\"id\":\"o\",\"textKey\":\"o\"," +
            "\"end\":true}]}]}";

        private const string StoryQuest =
            "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_dlg\",\"questKind\":\"story\"," +
            "\"storyLine\":\"main\",\"titleKey\":\"t\",\"descriptionKey\":\"d\"," +
            "\"giverActorId\":\"npc_giver\",\"progressScope\":\"cumulative\"," +
            "\"objectives\":[{\"type\":\"openStashes\",\"count\":1}]," +
            "\"rewards\":{\"money\":50,\"items\":[{\"itemId\":\"ammo_545\",\"count\":3}]}}";

        private const string TalkQuest =
            "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_talk\",\"questKind\":\"story\"," +
            "\"storyLine\":\"talk\",\"titleKey\":\"t\",\"descriptionKey\":\"d\"," +
            "\"giverActorId\":\"npc_giver\",\"progressScope\":\"cumulative\"," +
            "\"objectives\":[{\"type\":\"talkToActor\",\"actorId\":\"npc_giver\"}]," +
            "\"rewards\":{\"money\":1}}";

        /// <summary>Создаёт категорию тестов диалогов.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public DialogTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var items = services.Resolve<IItemRepository>();
            var inventory = services.Resolve<IInventoryService>();
            var newGame = services.Resolve<INewGameService>();

            WithFixture(
                new Dictionary<string, string>
                {
                    ["catalog.json"] = Catalog(
                        "dialogs/dlg_test.json",
                        "dialogs/dlg_locked.json",
                        "quests/qst_dlg.json",
                        "quests/qst_talk.json"),
                    ["dialogs/dlg_test.json"] = Dialog,
                    ["dialogs/dlg_locked.json"] = LockedDialog,
                    ["quests/qst_dlg.json"] = StoryQuest,
                    ["quests/qst_talk.json"] = TalkQuest
                },
                root =>
                {
                    var data = new DataLoader(new FileSystemDataSource(root)).Load();
                    var storyRepository = StoryQuestRepository.FromLoadedData(data);
                    var dialogs = DialogRepository.FromLoadedData(data);
                    var story = new StoryQuestService(storyRepository, items, inventory);
                    var service = new DialogService(dialogs, story, items, inventory);
                    var session = new GameSession(newGame.Create(42, "loadout_rifleman", new NewGameSettings(0, 1)));

                    StartAndHideTests(service, session);
                    QuestGiveTests(service, story, session);
                    ItemAndUnlockTests(service, session);
                    TurnInTests(service, story, session);
                    ErrorTests(service, session);
                    TalkTests(service, story, session);
                });
        }

        private void StartAndHideTests(IDialogService service, GameSession session)
        {
            Check("диалог: начало скрывает недоступный вариант", () =>
            {
                True(service.CanStart(session, "dlg_test"), "диалог доступен");
                var view = service.Start(session, "dlg_test", "npc_giver");
                True(view != null, "узел получен");
                Equal("n1", view!.NodeId, "начальный узел");
                Equal("npc_giver", view.SpeakerActorId, "говорящий");

                var ids = OptionIds(view);
                True(ids.Contains("o_quest"), "доступный вариант есть");
                False(ids.Contains("o_hidden"), "скрытый вариант убран");
                True(ids.Contains("o_unavail"), "недоступный виден");
                False(Available(view, "o_unavail"), "недоступный помечен");
            });

            Check("диалог: закрытый условиями диалог не начинается", () =>
            {
                False(service.CanStart(session, "dlg_locked"), "условия не выполнены");
                True(service.Start(session, "dlg_locked") == null, "узла нет");
            });
        }

        private void QuestGiveTests(IDialogService service, IStoryQuestService story, GameSession session)
        {
            Check("диалог: выдача квеста и раскрытие варианта", () =>
            {
                var result = service.Choose(session, "dlg_test", "n1", "o_quest");

                True(result.Succeeded, "шаг выполнен");
                Equal("qst_dlg", result.QuestId, "квест выдан");
                Equal("n2", result.View!.NodeId, "следующий узел");
                True(story.TryGetActive(session, "qst_dlg", out _), "квест активен");

                var again = service.Node(session, "dlg_test", "n1")!;
                True(OptionIds(again).Contains("o_hidden"), "вариант раскрыт");
            });
        }

        private void ItemAndUnlockTests(IDialogService service, GameSession session)
        {
            Check("диалог: предмет и открытие выдаются", () =>
            {
                var player = session.State.Player!;
                var before = CountInventory(player, "ammo_545");
                var item = service.Choose(session, "dlg_test", "n2", "o_item");

                True(item.Succeeded, "предмет выдан: " + item.Status);
                Equal(3, CountInventory(player, "ammo_545") - before, "количество");
                Equal("n3", item.View!.NodeId, "следующий узел");

                var unlock = service.Choose(session, "dlg_test", "n3", "o_unlock");
                True(unlock.Succeeded, "шаг выполнен");
                True(unlock.Ended, "диалог закрыт");
                True(player.HasUnlock("unlock_test"), "открытие получено");
            });
        }

        private void TurnInTests(IDialogService service, IStoryQuestService story, GameSession session)
        {
            Check("диалог: сдача квеста с наградой", () =>
            {
                var player = session.State.Player!;
                story.Accept(session, "qst_dlg");
                story.NotifyStashOpened(session, "loc_a");
                var money = player.Money;
                var ammo = CountInventory(player, "ammo_545");

                var result = service.Choose(session, "dlg_test", "n3", "o_turnin");

                True(result.Succeeded, "квест сдан");
                Equal(50, player.Money - money, "награда");
                Equal(50, result.Money, "награда в результате");
                Equal(ammo, CountInventory(player, "ammo_545"), "предметы награды не попали в инвентарь");
                Equal(1, result.Loot.Count, "предмет награды ждёт экрана наград");
                Equal("ammo_545", result.Loot[0].ItemId, "вид предмета награды");
                True(player.HasCompletedQuest("qst_dlg"), "в завершённых");
            });
        }

        private void ErrorTests(IDialogService service, GameSession session)
        {
            Check("диалог: недоступный вариант и неизвестные id", () =>
            {
                Equal(
                    DialogResultStatus.Unavailable,
                    service.Choose(session, "dlg_test", "n1", "o_unavail").Status,
                    "недоступный вариант");
                Equal(
                    DialogResultStatus.OptionNotFound,
                    service.Choose(session, "dlg_test", "n1", "o_missing").Status,
                    "неизвестный вариант");
                Equal(
                    DialogResultStatus.NodeNotFound,
                    service.Choose(session, "dlg_test", "n_missing", "o_quest").Status,
                    "неизвестный узел");
                True(service.Node(session, "dlg_test", "n_missing") == null, "узла нет");
                False(service.CanStart(session, "dlg_missing"), "диалога нет");
            });
        }

        private void TalkTests(IDialogService service, IStoryQuestService story, GameSession session)
        {
            Check("диалог: разговор засчитывает цель talkToActor", () =>
            {
                story.Accept(session, "qst_talk");
                story.TryGetActive(session, "qst_talk", out var active);
                False(story.IsReady(session, active), "до разговора не готов");

                service.Start(session, "dlg_test", "npc_giver");

                True(story.IsReady(session, active), "после разговора готов");
            });
        }

        private static List<string> OptionIds(DialogView view)
        {
            var result = new List<string>();
            foreach (var option in view.Options)
            {
                result.Add(option.OptionId);
            }

            return result;
        }

        private static bool Available(DialogView view, string optionId)
        {
            foreach (var option in view.Options)
            {
                if (string.Equals(option.OptionId, optionId, System.StringComparison.Ordinal))
                {
                    return option.Available;
                }
            }

            return false;
        }

        private static int CountInventory(AnomalyPath.Core.Player.PlayerState player, string itemId)
        {
            var total = 0;
            foreach (var item in player.Inventory.Items)
            {
                if (string.Equals(item.ItemId, itemId, System.StringComparison.Ordinal))
                {
                    total += item.Stack;
                }
            }

            return total;
        }
    }
}
