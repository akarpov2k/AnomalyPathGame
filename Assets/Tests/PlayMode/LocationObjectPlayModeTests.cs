#nullable enable
using System.Collections;
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Объекты локации: несколько кликабельных объектов, осмотр до опустошения,
    /// неактивный пустой объект, хаб без объектов и сохранение остатка.
    /// </summary>
    public sealed class LocationObjectPlayModeTests : PlayModeTestBase
    {
        // Id записей спавна — из данных: при загрузке объект восстанавливается по записи спавна.
        // Если пул локации уже дал объект с тем же id, тест кликает именно своё активное действие.
        private const string SpawnId = "spawn_road_crate";
        private const string TitleKey = "spawn.spawn_road_crate.title";
        private const string LootItemId = "ammo_545";
        private const string SecondSpawnId = "spawn_field_debris";

        /// <summary>
        /// Объект лута на размеченной локации стоит на сцене вырезкой: картинка из записи спавна,
        /// подписей нет, фигура кликабельна.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Object_OnExpedition_ShownAsSceneFigure()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen!.Session!.State.World;
            var location = new LocationInstance(
                "test_object_scene",
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.1);
            location.MarkSpawned();
            location.MarkStashRolled();
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            AddLootObject(location, LootItemId, 1);

            screen.ShowGame(false);
            yield return null;

            var action = FindObjectAction(SpawnId);
            Assert.IsNotNull(action, "Фигура объекта не появилась на сцене");

            var texts = action!.GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                Assert.IsFalse(text.gameObject.activeSelf, "у фигуры сцены нет подписей");
            }

            var icon = action.transform.Find("Icon");
            Assert.IsNotNull(icon, "у фигуры есть слой Icon");
            StringAssert.StartsWith(
                "obj_road_crate",
                icon!.GetComponent<Image>().sprite.name,
                "фигура рисует картинку из записи спавна");
            Assert.Greater(
                icon.GetComponent<RectTransform>().rect.height,
                0f,
                "размер фигуры считается от высоты в метрах");
        }

        /// <summary>
        /// Правый слот берёт зеркальную копию вырезки, левый — исходную: направление
        /// объекта смотрит внутрь кадра.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Object_BySlotSide_ShowsMirroredArt()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen!.Session!.State.World;
            screen.Sessions.LeaveHub(screen.Session!);
            var location = new LocationInstance(
                "locinst_mirror",
                "loc_route_highway",
                1,
                "bg_road_dawn",
                LocationSubtype.Path,
                1,
                0.2);
            location.MarkSpawned();
            location.MarkStashRolled();
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);

            location.AddObject(LootObject(SpawnId));
            location.AddObject(LootObject(SecondSpawnId));

            screen.ShowGame(false);
            yield return null;

            var left = FigureIcon(SpawnId);
            Assert.IsNotNull(left, "левый слот на сцене");
            Assert.IsNotNull(left!.sprite, "у левой фигуры есть картинка");
            Assert.AreEqual("obj_road_crate", left.sprite!.name, "левый слот рисует исходную вырезку");

            var right = FigureIcon(SecondSpawnId);
            Assert.IsNotNull(right, "правый слот на сцене");
            Assert.IsNotNull(right!.sprite, "у правой фигуры есть картинка");
            Assert.AreEqual("obj_field_debris_mirror", right.sprite!.name, "правый слот рисует зеркальную копию");
        }

        /// <summary>Два объекта локации показываются кликабельными фигурами на сцене.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Object_MultipleObjects_EachClickable()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen!.Session!.State.World;
            screen.Sessions.LeaveHub(screen.Session!);

            // Свежая локация без спавна и без тайника: на сцене ровно два места под объекты.
            var location = new LocationInstance(
                "locinst_two_objects",
                "loc_route_highway",
                1,
                "bg_road_dawn",
                LocationSubtype.Path,
                1,
                0.2);
            location.MarkSpawned();
            location.MarkStashRolled();
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);

            AddLootObject(location, LootItemId, 1);
            var second = new LootLocationObject(
                SecondSpawnId,
                "spawn.spawn_field_debris.title",
                "loot_human");
            second.SetLoot(new List<ItemInstance>
            {
                new ItemInstance(SecondSpawnId + "_instance", LootItemId, 1)
            });
            location.AddObject(second);

            screen.ShowGame(false);
            yield return null;

            foreach (var spawnId in new[] { SpawnId, SecondSpawnId })
            {
                var action = FindObjectAction(spawnId);
                Assert.IsNotNull(action, $"объект {spawnId} не попал в действия");
                Assert.IsTrue(action!.GetComponent<Button>().interactable, $"объект {spawnId} не кликабелен");
            }
        }

        /// <summary>У аномалии есть картинка зоны: берётся иконка из записи её спавна.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Anomaly_Figure_ShowsOwnSpawnIcon()
        {
            yield return StartGame();
            var screen = FindScreen();
            var location = IsolatedLocation(screen, "test_anomaly_icon");
            var anomaly = new AnomalyLocationObject(
                "spawn_anomaly_chem",
                "spawn.spawn_anomaly_chem.title",
                "anomaly_chem");
            location.MarkSpawned();
            location.AddObject(anomaly);

            screen.ShowGame(false);
            yield return null;

            var figure = FindObjectAction("spawn_anomaly_chem");
            Assert.IsNotNull(figure, "аномалия не попала на сцену");
            var icon = figure!.transform.Find("Icon");
            Assert.IsNotNull(icon, "у фигуры аномалии нет слоя текстуры");
            var image = icon!.GetComponent<Image>();
            Assert.IsNotNull(image, "у слоя текстуры нет Image");
            Assert.IsNotNull(image!.sprite, "у аномалии нет картинки");
            StringAssert.StartsWith("anomaly_", image.sprite!.name, "берётся картинка аномалии из данных спавна");
        }

        /// <summary>У записи спавна нет картинки: остаётся иконка артефакта её типа.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Anomaly_WithoutSpawnIcon_FallsBackToArtifactTexture()
        {
            yield return StartGame();
            var screen = FindScreen();
            var location = IsolatedLocation(screen, "test_anomaly_fallback");
            var anomaly = new AnomalyLocationObject(
                "spawn_test_anomaly",
                "spawn.spawn_anomaly_chem.title",
                "anomaly_chem");
            location.MarkSpawned();
            location.AddObject(anomaly);

            screen.ShowGame(false);
            yield return null;

            var figure = FindObjectAction("spawn_test_anomaly");
            Assert.IsNotNull(figure, "аномалия не попала на сцену");
            var icon = figure!.transform.Find("Icon");
            Assert.IsNotNull(icon, "у фигуры аномалии нет слоя текстуры");
            var image = icon!.GetComponent<Image>();
            Assert.IsNotNull(image, "у слоя текстуры нет Image");
            Assert.IsNotNull(image!.sprite, "у аномалии нет текстуры артефакта");
            StringAssert.Contains("art_", image.sprite!.name, "берётся иконка артефакта");
        }

        /// <summary>Осмотр открывает лут объекта, а после забора объект становится неактивным.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Object_Loot_Click_ShowsSpawnedLootAndDepletes()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen!.Session!.State.World;
            var location = new LocationInstance(
                "test_loot_click",
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.1);
            location.MarkSpawned();
            location.MarkStashRolled();
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            var lootObject = AddLootObject(location, LootItemId, 3);

            screen.ShowGame(false);
            yield return null;
            ClickObjectAction(SpawnId);

            var dialog = GameObject.Find("Rewards");
            Assert.IsNotNull(dialog, "Экран добычи не открылся");
            Assert.IsTrue(dialog!.activeSelf, "Экран добычи скрыт");
            Assert.IsNull(GameObject.Find("LootDialog"), "Диалога лута быть не должно");
            Assert.AreEqual(3, CountLootCells(), "В сетке не тот лут, что выпал объекту");

            Click("RewardTakeAll");
            yield return null;

            Assert.IsFalse(dialog.activeSelf, "После «Взять всё» экран добычи закрывается");
            Assert.IsTrue(lootObject.Looted, "Пустой объект отмечен забранным");
            Assert.IsFalse(lootObject.HasLoot, "Лута в объекте не осталось");
            var after = FindObjectAction(SpawnId);
            Assert.IsNotNull(after, "Пустой объект остаётся неактивным действием");
            Assert.IsFalse(after!.GetComponent<Button>().interactable, "Пустой объект неактивен");
        }

        /// <summary>Закрытие диалога с остатком оставляет объект доступным для повторного осмотра.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Object_CloseWithRemainder_StaysActive()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.Sessions.LeaveHub(screen.Session!);
            var world = screen!.Session!.State.World;

            // Свежая локация без спавна и тайника: тест не зависит от маршрута и seed.
            var location = new LocationInstance(
                "locinst_remainder",
                "loc_route_highway",
                1,
                "bg_road_dawn",
                LocationSubtype.Path,
                1,
                0.2);
            location.MarkSpawned();
            location.MarkStashRolled();
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            var lootObject = AddLootObject(location, LootItemId, 2);

            screen.ShowGame(false);
            yield return null;
            ClickObjectAction(SpawnId);
            yield return null;

            Click("RewardClose");
            yield return null;
            Click("Закрыть");
            yield return null;

            Assert.IsTrue(lootObject.HasLoot, "Лут остался в объекте");
            Assert.IsNotNull(FindObjectAction(SpawnId), "Объект с лутом остаётся доступным");
        }

        /// <summary>В хабе объекта локации нет, тайник там тоже не проверяется.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Object_Hub_NotShown()
        {
            yield return StartGame();
            var screen = FindScreen();
            var hub = screen!.Session!.State.World.CurrentLocation;

            screen.ShowGame(false);
            yield return null;

            Assert.AreEqual(0, hub.Objects.Count, "Хаб не получает объекты спавна");
            Assert.IsFalse(hub.StashPresent, "В хабе тайник не проверяется");
            var panel = GameObject.Find("BigHub");
            Assert.IsNotNull(panel, "Большой хаб не открылся");
            Assert.IsNull(FindByPrefix(panel!.transform, "Object_"), "В хабе нет действий объектов");
            Assert.IsNull(GameObject.Find("Stash"), "В хабе нет действия тайника");
        }

        /// <summary>Лут объекта и его состояние остаются после сохранения и загрузки.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Object_Looted_SurvivesSaveAndLoad()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.Sessions.LeaveHub(screen.Session!);
            var world = screen!.Session!.State.World;

            // Свежая локация без спавна и тайника: на сцене один объект и оба места свободны.
            var location = new LocationInstance(
                "locinst_save_loot",
                "loc_route_highway",
                1,
                "bg_road_dawn",
                LocationSubtype.Path,
                1,
                0.2);
            location.MarkSpawned();
            location.MarkStashRolled();
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            var lootObject = AddLootObject(location, LootItemId, 4);

            screen.ShowGame(false);
            yield return null;
            ClickObjectAction(SpawnId);
            yield return null;
            Click("RewardTakeAll");
            yield return null;
            Assert.IsTrue(lootObject.Looted, "Объект не отмечен забранным");

            var slot = FreeSlot(screen);
            screen.Saves.Delete(slot);
            screen.Sessions.Save(screen.Session, slot, title: "Объект");
            var loaded = screen.Sessions.Load(slot);
            screen.Session = loaded;
            screen.ShowGame(false);
            yield return null;

            var restored = loaded.State.World.Map.GetLocation(location.InstanceId);
            Assert.IsNotNull(restored, "Локация не восстановилась из сейва");
            var restoredLoot = false;
            foreach (var locationObject in restored!.Objects)
            {
                if (locationObject.SpawnId == SpawnId)
                {
                    restoredLoot = locationObject.Looted;
                }
            }

            Assert.IsTrue(restoredLoot, "Забранный объект должен остаться забранным");
            var after = FindObjectAction(SpawnId);
            Assert.IsNotNull(after, "Забранный объект остаётся неактивным действием");
            Assert.IsFalse(after!.GetComponent<Button>().interactable, "Забранный объект неактивен");

            screen.Saves.Delete(slot);
        }

        private static LocationInstance IsolatedLocation(GameScreen screen, string id)
        {
            var world = screen.Session!.State.World;
            var location = new LocationInstance(
                id,
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.1);
            location.MarkSpawned();
            location.MarkStashRolled();
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            return location;
        }

        private static void ClearObjects(LocationInstance location)
        {
            foreach (var locationObject in location.Objects)
            {
                locationObject.MarkLooted();
            }
        }

        /// <summary>Кладёт в локацию объект лута с заданным числом стопок одного предмета.</summary>
        /// <param name="location">Локация, куда добавить объект.</param>
        /// <param name="itemId">Id существующего предмета.</param>
        /// <param name="stacks">Сколько отдельных стопок положить в объект.</param>
        /// <returns>Добавленный объект.</returns>
        private static LootLocationObject AddLootObject(
            LocationInstance location,
            string itemId,
            int stacks)
        {
            var lootObject = new LootLocationObject(SpawnId, TitleKey, "loot_human");
            var loot = new List<ItemInstance>();
            for (var index = 0; index < stacks; index++)
            {
                loot.Add(new ItemInstance($"{SpawnId}_instance_{index}", itemId, 1));
            }

            lootObject.SetLoot(loot);
            location.MarkSpawned();
            location.AddObject(lootObject);
            return lootObject;
        }

        /// <summary>Ищет объект на сцене по id записи спавна; пустой объект возвращается неактивным.</summary>
        /// <param name="spawnId">Id записи спавна объекта.</param>
        /// <returns>Кнопка объекта или <c>null</c>.</returns>
        private static GameObject? FindObjectAction(string spawnId)
        {
            var root = GameObject.Find("InGame");
            if (root == null)
            {
                return null;
            }

            var actionName = "Object_" + spawnId;
            GameObject? disabled = null;
            foreach (var child in root.GetComponentsInChildren<Button>(true))
            {
                if (!string.Equals(child.name, actionName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (child.interactable)
                {
                    return child.gameObject;
                }

                disabled ??= child.gameObject;
            }

            return disabled;
        }

        /// <summary>Нажимает действие объекта по id спавна, не полагаясь на порядок объектов в сцене.</summary>
        /// <param name="spawnId">Id записи спавна объекта.</param>
        private static void ClickObjectAction(string spawnId)
        {
            var action = FindObjectAction(spawnId);
            Assert.IsNotNull(action, $"действие объекта {spawnId} не найдено");
            action!.GetComponent<Button>().onClick.Invoke();
        }

        private static int CountLootCells()
        {
            var panel = FindAnywhere("Rewards");
            if (panel == null)
            {
                return 0;
            }

            var total = 0;
            foreach (var child in panel.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith("RewardItem_", System.StringComparison.Ordinal))
                {
                    total++;
                }
            }

            return total;
        }

        /// <summary>Объект лута по существующей записи спавна: один предмет в остатке.</summary>
        /// <param name="spawnId">Id записи спавна.</param>
        /// <returns>Новый объект лута.</returns>
        private static LootLocationObject LootObject(string spawnId)
        {
            var lootObject = new LootLocationObject(spawnId, "spawn." + spawnId + ".title", "loot_human");
            lootObject.SetLoot(new List<ItemInstance>
            {
                new ItemInstance(spawnId + "_instance", LootItemId, 1)
            });
            return lootObject;
        }

        /// <summary>Слой картинки фигуры по id записи спавна.</summary>
        /// <param name="spawnId">Id записи спавна.</param>
        /// <returns>Картинка фигуры или <c>null</c>.</returns>
        private static Image? FigureIcon(string spawnId)
        {
            var action = FindObjectAction(spawnId);
            var icon = action == null ? null : action.transform.Find("Icon");
            return icon == null ? null : icon.GetComponent<Image>();
        }

        /// <summary>Ищет свободный слот сохранения вдали от пользовательских, чтобы не затирать их.</summary>
        /// <param name="screen">Игровой экран с сервисом сохранений.</param>
        /// <returns>Имя свободного слота.</returns>
        private static string FreeSlot(GameScreen screen)
        {
            for (var number = 40; number <= 60; number++)
            {
                var candidate = SaveSlots.Slot(number);
                if (!screen.Saves.Exists(candidate))
                {
                    return candidate;
                }
            }

            Assert.Fail("Нет свободного слота сохранения для теста");
            return string.Empty;
        }
    }
}
