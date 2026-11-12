#nullable enable
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>
    /// Спавн объектов локации: пул локации, шанс объекта, несколько объектов,
    /// не больше одного противника, детерминизм, лут при спавне и сериализация.
    /// </summary>
    public sealed partial class LocationObjectTests : TestBase
    {
        /// <inheritdoc />
        public override void Run()
        {
            PoolTests();
            ChanceTests();
            EnemyTests();
            StashSlotTests();
            LootTests();
            SaveTests();
        }

        private void PoolTests()
        {
            Check("объект: выбираются только записи пула локации", () =>
            {
                var scenario = Scenario(
                    "locinst_house",
                    "loc_house",
                    "building",
                    BuildingTags(),
                    "[\"spawn_house_cabinet\"]",
                    "[1,1]",
                    Entry("spawn_house_cabinet", "indoor,house", chance: 1),
                    Entry("spawn_car_wreck", "road,outdoor", chance: 1));

                var spawned = scenario.Service.Spawn(scenario.Location);

                Equal(1, spawned.Count, "объектов");
                Equal("spawn_house_cabinet", spawned[0].SpawnId, "шкаф, а не машина");
            });

            Check("объект: objectCount задаёт предел числа объектов", () =>
            {
                var scenario = Scenario(
                    "locinst_road",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_a\",\"spawn_b\",\"spawn_c\"]",
                    "[1,1]",
                    Entry("spawn_a", "road,outdoor", chance: 1),
                    Entry("spawn_b", "road,outdoor", chance: 1),
                    Entry("spawn_c", "road,outdoor", chance: 1));

                Equal(1, scenario.Service.Spawn(scenario.Location).Count, "не больше одного");
            });

            Check("объект: несколько объектов появляются из пула", () =>
            {
                var scenario = Scenario(
                    "locinst_many",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_a\",\"spawn_b\"]",
                    "[2,2]",
                    Entry("spawn_a", "road,outdoor", chance: 1),
                    Entry("spawn_b", "road,outdoor", chance: 1));

                Equal(2, scenario.Service.Spawn(scenario.Location).Count, "два объекта");
            });

            Check("объект: одинаковый seed и локация дают тот же набор", () =>
            {
                var first = Scenario(
                    "locinst_same",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_a\",\"spawn_b\",\"spawn_c\"]",
                    "[1,3]",
                    Entry("spawn_a", "road,outdoor", chance: 0.5),
                    Entry("spawn_b", "road,outdoor", chance: 0.5),
                    Entry("spawn_c", "road,outdoor", chance: 0.5));
                var second = Scenario(
                    "locinst_same",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_a\",\"spawn_b\",\"spawn_c\"]",
                    "[1,3]",
                    Entry("spawn_a", "road,outdoor", chance: 0.5),
                    Entry("spawn_b", "road,outdoor", chance: 0.5),
                    Entry("spawn_c", "road,outdoor", chance: 0.5));

                Equal(
                    Ids(first.Service.Spawn(first.Location)),
                    Ids(second.Service.Spawn(second.Location)),
                    "совпадает");
            });

            Check("объект: повторный спавн возвращает тот же набор", () =>
            {
                var scenario = Scenario(
                    "locinst_repeat",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_a\"]",
                    "[1,1]",
                    Entry("spawn_a", "road,outdoor", chance: 1));
                var first = scenario.Service.Spawn(scenario.Location);
                var second = scenario.Service.Spawn(scenario.Location);

                True(ReferenceEquals(first, second), "тот же список");
                Equal(1, second.Count, "не пересоздан");
            });

            Check("объект: на хабе спавна нет", () =>
            {
                var scenario = Scenario(
                    "locinst_hub",
                    "loc_hub",
                    "hub",
                    "[\"hub\",\"outdoor\"]",
                    "[]",
                    "[0,0]",
                    Entry("spawn_a", "road,outdoor", chance: 1));
                var location = new LocationInstance(
                    "locinst_hub",
                    scenario.Location.TemplateId,
                    1,
                    "bg",
                    LocationSubtype.Hub,
                    0,
                    0);

                Equal(0, scenario.Service.Spawn(location).Count, "хаб");
                True(location.Spawned, "бросок зафиксирован");
            });
        }

        private void ChanceTests()
        {
            Check("объект: шанс 0 не даёт объекта", () =>
            {
                var scenario = Scenario(
                    "locinst_zero",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_a\"]",
                    "[1,1]",
                    Entry("spawn_a", "road,outdoor", chance: 0));

                Equal(0, scenario.Service.Spawn(scenario.Location).Count, "не выпал");
            });

            Check("объект: шанс 1 даёт объект", () =>
            {
                var scenario = Scenario(
                    "locinst_one",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_a\"]",
                    "[1,1]",
                    Entry("spawn_a", "road,outdoor", chance: 1));

                Equal(1, scenario.Service.Spawn(scenario.Location).Count, "выпал");
            });
        }

        private void EnemyTests()
        {
            Check("объект: противников на локации не больше трёх", () =>
            {
                var scenario = Scenario(
                    "locinst_enemy",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_enemy_a\",\"spawn_enemy_b\",\"spawn_loot\"]",
                    "[3,3]",
                    Enemy("spawn_enemy_a", "enemy_bandit", chance: 1),
                    Enemy("spawn_enemy_b", "enemy_merc", chance: 1),
                    Entry("spawn_loot", "road,outdoor", chance: 1));

                var spawned = scenario.Service.Spawn(scenario.Location);
                var enemies = 0;
                foreach (var locationObject in spawned)
                {
                    if (locationObject.Kind == LocationObjectKind.Enemy)
                    {
                        enemies++;
                    }
                }

                True(enemies >= 1 && enemies <= 3, "от одного до трёх противников");
            });

            Check("объект: противник создаёт EnemyLocationObject с актёром", () =>
            {
                var scenario = Scenario(
                    "locinst_actor",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_enemy_a\"]",
                    "[1,1]",
                    Enemy("spawn_enemy_a", "enemy_bandit", chance: 1));

                var spawned = scenario.Service.Spawn(scenario.Location);

                True(spawned[0] is EnemyLocationObject, "вид объекта");
                Equal("enemy_bandit", ((EnemyLocationObject)spawned[0]).ActorId, "актёр");
            });

            Check("объект: побеждённый противник не помечает следующего", () =>
            {
                var location = new LocationInstance(
                    "locinst_multi_enemy",
                    "loc_road",
                    1,
                    "bg",
                    LocationSubtype.Path,
                    1,
                    0.1);
                location.AddObject(new EnemyLocationObject("spawn_enemy_a", "title", "enemy_bandit"));
                location.AddObject(new EnemyLocationObject("spawn_enemy_b", "title", "enemy_bandit"));

                location.MarkEnemyDefeated("enemy_bandit");
                True(location.FindEnemy("enemy_bandit") != null, "второй противник ещё жив");
                location.MarkEnemyDefeated("enemy_bandit");
                True(location.FindEnemy("enemy_bandit") == null, "все противники побеждены");
            });
        }

        /// <summary>
        /// Тайник занимает одно из двух мест сцены, поэтому объектов пула при нём
        /// появляется на один меньше; без тайника оба места отдаются объектам.
        /// </summary>
        private void StashSlotTests()
        {
            Check("объект: тайник занимает одно из двух мест сцены", () =>
            {
                var scenario = Scenario(
                    "locinst_stash_slot",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_a\",\"spawn_b\"]",
                    "[2,2]",
                    Entry("spawn_a", "road,outdoor", chance: 1),
                    Entry("spawn_b", "road,outdoor", chance: 1));
                scenario.Location.MarkStashPresent();

                var spawned = scenario.Service.Spawn(scenario.Location);

                Equal(1, spawned.Count, "объект пула и тайник — два места");
            });

            Check("объект: без тайника оба объекта пула помещаются", () =>
            {
                var scenario = Scenario(
                    "locinst_stash_free",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_a\",\"spawn_b\"]",
                    "[2,2]",
                    Entry("spawn_a", "road,outdoor", chance: 1),
                    Entry("spawn_b", "road,outdoor", chance: 1));

                var spawned = scenario.Service.Spawn(scenario.Location);

                Equal(2, spawned.Count, "два объекта пула");
            });
        }

        private void LootTests()
        {
            Check("объект: лут бросается при спавне и живёт в объекте", () =>
            {
                var loot = new FakeLoot
                {
                    Items = new[] { new ItemInstance("loot_1", "ammo_9x18", 7) }
                };
                var scenario = Scenario(
                    "locinst_loot",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_a\"]",
                    "[1,1]",
                    loot,
                    Entry("spawn_a", "road,outdoor", chance: 1));

                var spawned = (LootLocationObject)scenario.Service.Spawn(scenario.Location)[0];

                Equal(1, loot.RollCalls, "один бросок");
                Equal(1, spawned.Loot.Count, "предмет в объекте");
                True(spawned.HasLoot, "лут есть");
            });

            Check("объект: повторный спавн не перебрасывает лут", () =>
            {
                var loot = new FakeLoot
                {
                    Items = new[] { new ItemInstance("loot_1", "ammo_9x18", 7) }
                };
                var scenario = Scenario(
                    "locinst_loot2",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_a\"]",
                    "[1,1]",
                    loot,
                    Entry("spawn_a", "road,outdoor", chance: 1));

                scenario.Service.Spawn(scenario.Location);
                scenario.Service.Spawn(scenario.Location);

                Equal(1, loot.RollCalls, "бросок один");
            });

            Check("объект: забранный предмет уходит, объект пустеет", () =>
            {
                var loot = new FakeLoot
                {
                    Items = new[] { new ItemInstance("loot_1", "ammo_9x18", 7) }
                };
                var scenario = Scenario(
                    "locinst_take",
                    "loc_road",
                    "path",
                    RoadTags(),
                    "[\"spawn_a\"]",
                    "[1,1]",
                    loot,
                    Entry("spawn_a", "road,outdoor", chance: 1));
                var spawned = (LootLocationObject)scenario.Service.Spawn(scenario.Location)[0];

                True(spawned.Take("loot_1"), "забран");
                False(spawned.HasLoot, "лута не осталось");
                True(spawned.Looted, "объект пуст");
                False(spawned.Take("loot_1"), "повторно не забрать");
            });
        }

        private void SaveTests()
        {
            Check("объект: список объектов и остаток лута переживают сериализацию", () =>
            {
                var save = new LocationSaveData(
                    "locinst_1",
                    "loc_road",
                    1,
                    "bg",
                    LocationSubtype.Path,
                    1,
                    0.2,
                    visited: true,
                    cleared: false,
                    objectsSpawned: true,
                    objects: new[]
                    {
                        new LocationObjectSaveData("spawn_a", false, new[]
                        {
                            new ItemSaveData("loot_1", "ammo_9x18", 7, null, null, null, null, null)
                        }),
                        new LocationObjectSaveData("spawn_enemy", true)
                    });
                var restored = LocationSaveData.FromJson(save.ToJson(), "test");

                True(restored.ObjectsSpawned, "спавн выполнен");
                Equal(2, restored.Objects.Count, "объектов");
                Equal("spawn_a", restored.Objects[0].SpawnId, "первый объект");
                Equal(1, restored.Objects[0].Items.Count, "остаток лута");
                True(restored.Objects[1].Looted, "противник побеждён");
            });

            Check("объект: без спавна список объектов пуст", () =>
            {
                var save = new LocationSaveData(
                    "locinst_2",
                    "loc_hub",
                    1,
                    "bg",
                    LocationSubtype.Hub,
                    0,
                    0,
                    visited: false,
                    cleared: false);
                var restored = LocationSaveData.FromJson(save.ToJson(), "test");

                False(restored.ObjectsSpawned, "спавна не было");
                Equal(0, restored.Objects.Count, "объектов нет");
            });
        }

    }
}
