#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.Trade;

namespace AnomalyPath.Core.Combat
{
    /// <summary>Актёр из данных: персонаж, торговец или враг.</summary>
    public sealed class ActorTemplate
    {
        /// <summary>Создаёт актёра.</summary>
        /// <param name="id">Идентификатор.</param>
        /// <param name="type">Тип: <c>npc</c>, <c>trader</c>, <c>enemy</c>.</param>
        /// <param name="nameKey">Ключ имени.</param>
        /// <param name="combat">Боевой блок, если есть.</param>
        /// <param name="descriptionKey">Ключ описания.</param>
        /// <param name="portrait">Id портрета в каталоге спрайтов.</param>
        /// <param name="portraitMode">Кадрирование портрета.</param>
        /// <param name="factionId">Фракция или <c>null</c>.</param>
        /// <param name="locationId">Локация или <c>null</c>.</param>
        /// <param name="roles">Роли актёра.</param>
        /// <param name="trader">Блок торговли у торговца.</param>
        /// <param name="rarity">Ступень редкости актёра.</param>
        /// <param name="lootTableId">Таблица лута; только у врага.</param>
        /// <param name="dialogId">Диалог персонажа; только у NPC и торговца.</param>
        /// <param name="hubPortrait">Id полноростовой картинки для экрана хаба или <c>null</c>.</param>
        /// <param name="costume">Id полноростового костюма для боевого экрана или <c>null</c>.</param>
        public ActorTemplate(
            string id,
            ActorType type,
            string nameKey,
            CombatBlockTemplate? combat,
            string? descriptionKey = null,
            string? portrait = null,
            PortraitMode portraitMode = PortraitMode.Full,
            string? factionId = null,
            string? locationId = null,
            IReadOnlyList<string>? roles = null,
            TraderBlockTemplate? trader = null,
            Rarity rarity = global::AnomalyPath.Core.Templates.Rarity.Common,
            string? lootTableId = null,
            string? dialogId = null,
            string? hubPortrait = null,
            string? costume = null)
        {
            Id = id;
            Type = type;
            NameKey = nameKey;
            Combat = combat;
            DescriptionKey = descriptionKey;
            Portrait = portrait;
            PortraitMode = portraitMode;
            FactionId = factionId;
            LocationId = locationId;
            Roles = roles ?? Array.Empty<string>();
            Trader = trader;
            Rarity = rarity;
            LootTableId = lootTableId;
            DialogId = dialogId;
            HubPortrait = hubPortrait;
            Costume = costume;
        }

        /// <summary>Идентификатор.</summary>
        public string Id { get; }

        /// <summary>Тип актёра.</summary>
        public ActorType Type { get; }

        /// <summary>Ключ имени.</summary>
        public string NameKey { get; }

        /// <summary>Боевой блок; у NPC и торговца может отсутствовать.</summary>
        public CombatBlockTemplate? Combat { get; }

        /// <summary>Ключ описания.</summary>
        public string? DescriptionKey { get; }

        /// <summary>Id портрета в каталоге спрайтов: <c>portraits/trd_hub_smuggler</c>.</summary>
        public string? Portrait { get; }

        /// <summary>Кадрирование портрета: в полный рост или по пояс.</summary>
        public PortraitMode PortraitMode { get; }

        /// <summary>
        /// Id полноростовой картинки для экрана хаба (<c>portraits/trd_hub_smuggler_full</c>)
        /// или <c>null</c> — тогда хаб берёт обычный портрет.
        /// </summary>
        public string? HubPortrait { get; }

        /// <summary>
        /// Id полноростового костюма для боевого экрана (<c>costumes/itm_armor_jacket</c>)
        /// или <c>null</c> — тогда боевой экран показывает нейтральный силуэт.
        /// </summary>
        public string? Costume { get; }

        /// <summary>Фракция или <c>null</c> у одиночки.</summary>
        public string? FactionId { get; }

        /// <summary>Локация; у торговца привязка задаётся списком <c>traders</c> хаба.</summary>
        public string? LocationId { get; }

        /// <summary>Роли актёра.</summary>
        public IReadOnlyList<string> Roles { get; }

        /// <summary>Блок торговли; только у <c>type = trader</c>.</summary>
        public TraderBlockTemplate? Trader { get; }

        /// <summary>Ступень редкости актёра; у врага множит шансы лута.</summary>
        public Rarity Rarity { get; }

        /// <summary>Таблица лута; допустима только у <c>type = enemy</c>.</summary>
        public string? LootTableId { get; }

        /// <summary>Диалог персонажа; только у <c>npc</c> и <c>trader</c>.</summary>
        public string? DialogId { get; }

        /// <summary>Проверяет, есть ли у актёра роль.</summary>
        /// <param name="role">Код роли.</param>
        /// <returns><c>true</c>, если роль назначена.</returns>
        public bool HasRole(string role)
        {
            foreach (var item in Roles)
            {
                if (string.Equals(item, role, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Читает актёра из записи данных.</summary>
        /// <param name="value">Загруженная запись.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Актёр.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static ActorTemplate FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            var typeCode = DataMap.RequireString(value, "type", sourcePath);
            if (!SchemaEnum.TryParse(typeCode, out ActorType type))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownActorType, typeCode);
            }

            var nameKey = DataMap.RequireString(value, "nameKey", sourcePath);
            var roles = DataMap.OptionalStringArray(value, "roles");
            var portraitMode = ReadPortraitMode(value, sourcePath);
            var combat = ReadCombat(value, type, id, roles, sourcePath);
            var trader = ReadTrader(value, type, roles, sourcePath);
            var lootTableId = DataMap.OptionalString(value, "lootTable");
            if (lootTableId != null && type != ActorType.Enemy)
            {
                throw new DataException(sourcePath, CoreErrors.LootTableOnNonEnemy, id);
            }

            var dialogId = DataMap.OptionalString(value, "dialogId");
            if (dialogId != null && type == ActorType.Enemy)
            {
                throw new DataException(sourcePath, CoreErrors.DialogOnEnemy, id);
            }

            return new ActorTemplate(
                id,
                type,
                nameKey,
                combat,
                DataMap.OptionalString(value, "descriptionKey"),
                DataMap.OptionalString(value, "portrait"),
                portraitMode,
                DataMap.OptionalString(value, "factionId"),
                DataMap.OptionalString(value, "locationId"),
                roles,
                trader,
                ReadRarity(value, sourcePath),
                lootTableId,
                dialogId,
                DataMap.OptionalString(value, "hubPortrait"),
                DataMap.OptionalString(value, "costume"));
        }

        private static Rarity ReadRarity(JsonValue value, string sourcePath)
        {
            var code = DataMap.OptionalString(value, "rarity");
            if (code == null)
            {
                return global::AnomalyPath.Core.Templates.Rarity.Common;
            }

            if (!SchemaEnum.TryParse(code, out Rarity rarity))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownRarity, code);
            }

            return rarity;
        }

        private static PortraitMode ReadPortraitMode(JsonValue value, string sourcePath)
        {
            var code = DataMap.OptionalString(value, "portraitMode");
            if (code == null)
            {
                return PortraitMode.Full;
            }

            if (!SchemaEnum.TryParse(code, out PortraitMode mode))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownPortraitMode, code);
            }

            return mode;
        }

        private static TraderBlockTemplate? ReadTrader(
            JsonValue value,
            ActorType type,
            IReadOnlyList<string> roles,
            string sourcePath)
        {
            if (type != ActorType.Trader)
            {
                if (value.TryGet("trader", out _))
                {
                    throw new DataException(sourcePath, CoreErrors.TraderBlockOnNonTrader);
                }

                if (ContainsRole(roles, ActorRoles.Trader))
                {
                    throw new DataException(sourcePath, CoreErrors.TraderRoleOnNonTrader);
                }

                return null;
            }

            var trader = TraderBlockTemplate.FromJson(value, sourcePath);
            if (!ContainsRole(roles, ActorRoles.Trader))
            {
                throw new DataException(sourcePath, CoreErrors.TraderWithoutRole);
            }

            return trader;
        }

        private static bool ContainsRole(IReadOnlyList<string> roles, string role)
        {
            foreach (var item in roles)
            {
                if (string.Equals(item, role, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static CombatBlockTemplate? ReadCombat(
            JsonValue value,
            ActorType type,
            string id,
            IReadOnlyList<string> roles,
            string sourcePath)
        {
            if (!value.TryGet("combat", out var block) || !block.IsObject)
            {
                if (type == ActorType.Enemy)
                {
                    throw new DataException(sourcePath, CoreErrors.ActorWithoutCombat, id);
                }

                return null;
            }

            var tier = DataMap.RequireInt(block, "tier", sourcePath);
            var level = DataMap.RequireInt(block, "level", sourcePath);
            var health = DataMap.RequireInt(block, "health", sourcePath);
            var ap = DataMap.RequireInt(block, "ap", sourcePath);
            if (health <= 0 || ap < 0)
            {
                throw new DataException(sourcePath, CoreErrors.MissingNumberField, health <= 0 ? "health" : "ap");
            }

            var protection = new Dictionary<string, int>(StringComparer.Ordinal);
            if (block.TryGet("protection", out var protectionBlock) && protectionBlock.IsObject)
            {
                foreach (var pair in protectionBlock.Entries)
                {
                    if (pair.Value.Kind != JsonKind.Number)
                    {
                        throw new DataException(sourcePath, CoreErrors.MissingNumberField, "protection");
                    }

                    protection[pair.Key] = (int)Math.Round(pair.Value.Number);
                }
            }

            var accuracy = DataMap.RequireDouble(block, "accuracy", sourcePath);
            var accuracyBonus = DataMap.OptionalDouble(block, "accuracyBonus") ?? 0;
            var xp = DataMap.OptionalInt(block, "xp", 0);
            if (type == ActorType.Enemy && xp <= 0)
            {
                throw new DataException(sourcePath, CoreErrors.MissingNumberField, "xp");
            }

            var attack = ReadAttack(block, type, id, sourcePath);
            var skills = DataMap.OptionalStringArray(block, "skills");
            var defaultSkillId = DataMap.OptionalString(block, "defaultSkillId");
            var isMutant = type == ActorType.Enemy && ContainsRole(roles, ActorRoles.Mutant);
            ValidateCombatSkills(type, id, isMutant, skills, defaultSkillId, attack, sourcePath);
            var medkitItemId = ReadMedkit(block, type, id, sourcePath);

            return new CombatBlockTemplate(
                tier,
                level,
                health,
                ap,
                protection,
                accuracy,
                accuracyBonus,
                xp,
                attack,
                skills,
                isMutant ? defaultSkillId : null,
                medkitItemId,
                medkitItemId == null ? 0 : DataMap.OptionalInt(block, "medkitCharges", 1));
        }

        /// <summary>Читает аптечку противника: только враг и только вместе с числом применений.</summary>
        private static string? ReadMedkit(JsonValue block, ActorType type, string id, string sourcePath)
        {
            var medkitItemId = DataMap.OptionalString(block, "medkitItemId");
            var hasCharges = block.TryGet("medkitCharges", out var chargesValue);
            if (medkitItemId == null)
            {
                if (hasCharges && (chargesValue.Kind != JsonKind.Number || chargesValue.Number != 0))
                {
                    throw new DataException(sourcePath, CoreErrors.ActorMedkitCharges, id);
                }

                return null;
            }

            if (type != ActorType.Enemy)
            {
                throw new DataException(sourcePath, CoreErrors.ActorMedkitNotEnemy, id);
            }

            var charges = DataMap.OptionalInt(block, "medkitCharges", 1);
            if (!hasCharges || charges < 1)
            {
                throw new DataException(sourcePath, CoreErrors.ActorMedkitCharges, id);
            }

            return medkitItemId;
        }

        /// <summary>Проверяет, чем актёр воюет: мутант — скиллами, остальные враги — оружием.</summary>
        private static void ValidateCombatSkills(
            ActorType type,
            string id,
            bool isMutant,
            IReadOnlyList<string> skills,
            string? defaultSkillId,
            ActorAttackTemplate? attack,
            string sourcePath)
        {
            if (!isMutant)
            {
                if (skills.Count > 0)
                {
                    throw new DataException(sourcePath, CoreErrors.ActorSkillsNotMutant, id);
                }

                if (defaultSkillId != null)
                {
                    throw new DataException(sourcePath, CoreErrors.DefaultSkillNotMutant, id);
                }

                if (type == ActorType.Enemy && (attack == null || attack.IsNatural))
                {
                    throw new DataException(sourcePath, CoreErrors.EnemyWithoutWeapon, id);
                }

                return;
            }

            if (attack != null && !attack.IsNatural)
            {
                throw new DataException(sourcePath, CoreErrors.MutantWithWeapon, id);
            }

            if (skills.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.ActorWithoutSkills, id);
            }

            if (defaultSkillId == null)
            {
                throw new DataException(sourcePath, CoreErrors.ActorWithoutDefaultSkill, id);
            }

            if (!ContainsSkill(skills, defaultSkillId))
            {
                throw new DataException(sourcePath, CoreErrors.DefaultSkillNotListed, defaultSkillId);
            }
        }

        private static bool ContainsSkill(IReadOnlyList<string> skills, string skillId)
        {
            for (var i = 0; i < skills.Count; i++)
            {
                if (string.Equals(skills[i], skillId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static ActorAttackTemplate? ReadAttack(JsonValue block, ActorType type, string id, string sourcePath)
        {
            var hasAttack = block.TryGet("attack", out var attackValue);
            if (type != ActorType.Enemy)
            {
                if (hasAttack)
                {
                    throw new DataException(sourcePath, CoreErrors.AttackOnNonEnemy, id);
                }

                return null;
            }

            if (!hasAttack || !attackValue.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.ActorWithoutAttack, id);
            }

            return ActorAttackTemplate.FromJson(attackValue, sourcePath);
        }
    }
}
