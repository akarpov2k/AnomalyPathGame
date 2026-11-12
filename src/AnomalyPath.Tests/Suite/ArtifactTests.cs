#nullable enable
using System;
using System.IO;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Artifacts;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Json;

namespace AnomalyPath.Tests
{
    /// <summary>Артефакты: модификаторы, экземплярная редкость, доп. свойства и затухание.</summary>
    public sealed class ArtifactTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов артефактов.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public ArtifactTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var artifacts = services.Resolve<IArtifactService>();

            Check("артефакты: базовые свойства — из данных без множителей", () =>
            {
                var modifiers = artifacts.GetModifiers(Artifact("art_fire_ember", "art_1"));
                Equal(4, modifiers.HealthMax, "здоровье");
                Near(0.04, modifiers.Accuracy, "меткость");
                Equal(3, modifiers.ProtectionOf("dmg_fire"), "защита от огня");
                Equal(-2, modifiers.ProtectionOf("dmg_rupture"), "минус к разрыву");
                Equal(0, modifiers.ProtectionOf("dmg_bullet"), "пули");
            });

            Check("артефакты: легендарная ступень усиливает плюсы и ослабляет минусы", () =>
            {
                var modifiers = artifacts.GetModifiers(Artifact("art_fire_ember", "art_1", "art:legendary"));
                Equal(6, modifiers.HealthMax, "4 × 1.6");
                Near(0.064, modifiers.Accuracy, "0.04 × 1.6");
                Equal(5, modifiers.ProtectionOf("dmg_fire"), "3 × 1.6 = 4.8 → 5");
                Equal(-1, modifiers.ProtectionOf("dmg_rupture"), "-2 × 0.4 = -0.8 → -1");
            });

            Check("артефакты: доп. свойства легендарки берутся из пула артефакта", () =>
            {
                var modifiers = artifacts.GetModifiers(Artifact("art_fire_ember", "art_1", "art:legendary:0,1"));
                Near(0.08, modifiers.Accuracy, "0.064 + 0.032 × 0.5");
                Equal(8, modifiers.HealthMax, "6.4 + 3.2 × 0.5 = 8");
            });

            Check("артефакты: номер доп. свойства вне пула игнорируется", () =>
            {
                var modifiers = artifacts.GetModifiers(Artifact("art_fire_ember", "art_1", "art:legendary:99"));
                Equal(6, modifiers.HealthMax, "только база");
            });

            Check("артефакты: чужой или битый вариант не применяется", () =>
            {
                var modifiers = artifacts.GetModifiers(Artifact("art_fire_ember", "art_1", "weapon_variant"));
                Equal(4, modifiers.HealthMax, "базовые числа");
                Equal(3, modifiers.ProtectionOf("dmg_fire"), "базовая защита");
            });

            Check("артефакты: одинаковые модификаторы складываются с затуханием", () =>
            {
                var equipment = new Equipment("player", 0);
                equipment.SetArtifact(0, Artifact("art_fire_ember", "art_1"));
                equipment.SetArtifact(1, Artifact("art_fire_ember", "art_2"));
                var modifiers = artifacts.GetEquippedModifiers(equipment);
                Equal(6, modifiers.HealthMax, "4 + 4 × 0.5");
                Near(0.06, modifiers.Accuracy, "0.04 + 0.02");
                Equal(5, modifiers.ProtectionOf("dmg_fire"), "3 + 1.5 = 4.5 → 5");
                Equal(-3, modifiers.ProtectionOf("dmg_rupture"), "-2 - 1");
            });

            Check("артефакты: сломанный артефакт не даёт модификаторов", () =>
            {
                var equipment = new Equipment("player", 0);
                equipment.SetArtifact(0, new ItemInstance("broken", "art_fire_ember", 1, 0, 10));
                var modifiers = artifacts.GetEquippedModifiers(equipment);
                Equal(0, modifiers.HealthMax, "здоровье");
                Equal(0, modifiers.ProtectionOf("dmg_fire"), "защита");
            });

            Check("артефакты: выносливость растёт, минус к здоровью остаётся", () =>
            {
                var modifiers = artifacts.GetModifiers(Artifact("art_electric_spark", "spark_1"));
                Equal(3, modifiers.Stamina, "выносливость");
                Equal(-2, modifiers.HealthMax, "минус к здоровью");
                Equal(3, modifiers.ProtectionOf("dmg_electric"), "защита от электричества");
            });

            Check("артефакты: ношение не тратит экземпляр", () =>
            {
                var armor = new Equipment("player", 0);
                armor.Set(EquipmentSlot.Belt, new ItemInstance("belt", "itm_belt_artifact", 1));
                var artifact = Artifact("art_fire_ember", "art_1");
                True(services.Resolve<IEquipmentService>().TryEquipArtifact(armor, artifact, out _), "надет");
                Equal(1, artifact.Stack, "стопка");
                True(artifact.Charges == null, "зарядов нет");
                True(artifact.Durability == null, "прочности нет");
            });

            Check("артефакты: бросок редкости детерминирован", () =>
            {
                var first = artifacts.RollVariantKey("art_fire_ember", 12345);
                var second = artifacts.RollVariantKey("art_fire_ember", 12345);
                True(first != null, "код варианта");
                Equal(first, second, "тот же seed — тот же вариант");
                True(first!.StartsWith("art:", StringComparison.Ordinal), "префикс артефакта");
            });

            Check("артефакты: у не-артефакта варианта нет", () =>
            {
                True(artifacts.RollVariantKey("itm_armor_jacket", 1) == null, "броня");
                True(artifacts.RollVariantKey("unknown_item", 1) == null, "неизвестный предмет");
            });

            Check("артефакты: код варианта разбирается и собирается", () =>
            {
                var key = ArtifactVariantCodec.Format(AnomalyPath.Core.Templates.Rarity.Epic, new[] { 0, 2 });
                Equal("art:epic:0,2", key, "код");
                True(ArtifactVariantCodec.TryParse(key, out var variant), "разбор");
                Equal(AnomalyPath.Core.Templates.Rarity.Epic, variant!.Rarity, "ступень");
                Equal(2, variant.ExtraIndexes.Count, "доп. свойства");
                False(ArtifactVariantCodec.TryParse("art:nope", out _), "неизвестная ступень");
                False(ArtifactVariantCodec.TryParse("", out _), "пусто");
            });

            Check("артефакты: таблица редкости в данных полная", () =>
            {
                var path = Path.Combine(_dataRoot, "artifacts.json");
                var settings = ArtifactSettings.FromJson(JsonParser.Parse(File.ReadAllText(path), path), path);
                var total = 0.0;
                foreach (var rule in settings.Rarities)
                {
                    total += rule.Chance;
                }

                Equal(6, settings.Rarities.Count, "шесть ступеней");
                Near(1, total, "сумма шансов");
                Near(0.5, settings.Falloff, "затухание");
            });
        }

        private static ItemInstance Artifact(string itemId, string instanceId, string? variant = null) =>
            new ItemInstance(instanceId, itemId, 1, variantKey: variant);

        private void Near(double expected, double actual, string what) =>
            True(
                Math.Abs(expected - actual) <= 1e-9,
                what + ": ожидалось " + expected + ", получено " + actual);
    }
}
