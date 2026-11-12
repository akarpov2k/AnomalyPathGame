#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Session;
using AnomalyPath.Runtime.Screens.Combat;
using AnomalyPath.Runtime.Ui.Art;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Снимает кадры боевого экрана из собранного префаба: ход игрока с открытой выпадашкой
    /// скиллов и открытым журналом действий. Бой берётся настоящий:
    /// новая игра, первый противник из данных, оружие игрока и боевые действия — через сервисы.
    /// </summary>
    public static partial class CombatScreenScreenshot
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/CombatScreen.prefab";
        private const string OutputFolder = "Logs/Screenshots";
        private const string DataRoot = "Assets/Data";
        private const string CatalogPath = "Assets/Art/SpriteCatalog.asset";
        private const string BattleBackdropPath = "Assets/Art/Backdrops/combat_bg_route_forest.png";
        private const int Width = 1920;
        private const int Height = 1080;
        private const int Seed = 42;
        private const int LogActions = 2;

        private enum MenuKind
        {
            None,
            Skills
        }

        /// <summary>Пункт меню: снять кадры боевого экрана.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншот боевого экрана")]
        public static void CaptureFromMenu() => CaptureAll();

        /// <summary>Точка входа batch-режима.</summary>
        public static void CaptureFromBatch()
        {
            try
            {
                CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Снимает кадры в <c>Logs/Screenshots/combat-*.png</c>.</summary>
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputFolder);
            var result = Preflight.Run(DataRoot);
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            var services = result.Services;
            var texts = services.Resolve<IGameTexts>();
            var items = services.Resolve<IItemRepository>();
            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>(CatalogPath);
            var skills = services.Resolve<ISkillRepository>();
            var settings = services.Resolve<ICombatSettings>();
            var progression = services.Resolve<IProgressionService>();
            var combatService = services.Resolve<ICombatService>();
            var ai = services.Resolve<ICombatAi>();

            var playerState = NewPlayer(services);
            var combat = StartCombat(services, combatService, playerState);
            var menu = new MenuContext(combatService, skills, settings);

            Capture("combat-skills.png", panel =>
            {
                Fill(
                    panel,
                    menu,
                    progression,
                    combat,
                    playerState,
                    texts,
                    items,
                    catalog,
                    MenuKind.Skills,
                    Array.Empty<string>());
                HoverEntry(panel, "reload");
            });

            Capture("combat-shot.png", panel =>
            {
                Fill(
                    panel,
                    menu,
                    progression,
                    combat,
                    playerState,
                    texts,
                    items,
                    catalog,
                    MenuKind.None,
                    Array.Empty<string>());
                ShowShot(panel);
            });

            Capture("combat-armor-plate.png", panel =>
            {
                Fill(
                    panel,
                    menu,
                    progression,
                    combat,
                    playerState,
                    texts,
                    items,
                    catalog,
                    MenuKind.None,
                    Array.Empty<string>());
                ApplyPlayerFigure(panel, catalog, "costumes/combat_itm_armor_plate", "icons/wpn_ak74");
            });

            Capture("combat-armor-exoskeleton.png", panel =>
            {
                Fill(
                    panel,
                    menu,
                    progression,
                    combat,
                    playerState,
                    texts,
                    items,
                    catalog,
                    MenuKind.None,
                    Array.Empty<string>());
                ApplyPlayerFigure(panel, catalog, "costumes/combat_itm_armor_exoskeleton", "icons/wpn_ak74");
            });

            Capture("combat-weapon-ks23.png", panel =>
            {
                Fill(
                    panel,
                    menu,
                    progression,
                    combat,
                    playerState,
                    texts,
                    items,
                    catalog,
                    MenuKind.None,
                    Array.Empty<string>());
                ApplyPlayerFigure(panel, catalog, "costumes/combat_itm_armor_jacket", "icons/wpn_ks23");
            });

            Capture("combat-weapon-sv98.png", panel =>
            {
                Fill(
                    panel,
                    menu,
                    progression,
                    combat,
                    playerState,
                    texts,
                    items,
                    catalog,
                    MenuKind.None,
                    Array.Empty<string>());
                ApplyPlayerFigure(panel, catalog, "costumes/combat_itm_armor_jacket", "icons/wpn_sv98");
            });

            var log = PlayTurns(combatService, ai, combat, texts);
            Capture("combat-journal.png", panel => Fill(
                panel,
                menu,
                progression,
                combat,
                playerState,
                texts,
                items,
                catalog,
                MenuKind.None,
                log));

            var mutant = StartCombat(services, combatService, playerState, "mutant_wolf");
            Capture("combat-mutant.png", panel => Fill(
                panel,
                menu,
                progression,
                mutant,
                playerState,
                texts,
                items,
                catalog,
                MenuKind.None,
                Array.Empty<string>()));

            Capture("combat-floating-hit.png", panel =>
            {
                Fill(
                    panel,
                    menu,
                    progression,
                    combat,
                    playerState,
                    texts,
                    items,
                    catalog,
                    MenuKind.None,
                    Array.Empty<string>());
                ShowFloatingLabel(panel, "EnemyFigure", "-23", CombatPalette.DamageText);
            });

            Capture("combat-floating-miss.png", panel =>
            {
                Fill(
                    panel,
                    menu,
                    progression,
                    combat,
                    playerState,
                    texts,
                    items,
                    catalog,
                    MenuKind.None,
                    Array.Empty<string>());
                ShowFloatingLabel(
                    panel,
                    "PlayerFigure",
                    UiText.Get(texts, UiTextKey.Combat_Floating_Miss),
                    CombatPalette.MissText);
            });

            Capture("combat-skill-chance.png", panel =>
            {
                Fill(
                    panel,
                    menu,
                    progression,
                    combat,
                    playerState,
                    texts,
                    items,
                    catalog,
                    MenuKind.Skills,
                    Array.Empty<string>());
                ShowSkillChance(panel, combatService, combat, texts);
            });

            Debug.Log($"AnomalyPath: кадры боевого экрана в {OutputFolder}");
        }

        private static PlayerState NewPlayer(ServiceContainer services)
        {
            var newGame = services.Resolve<INewGameService>();
            var state = newGame.Create(Seed, FirstLoadoutId(services), new NewGameSettings(0, 1));
            return state.Player ?? throw new InvalidOperationException("Новая игра не создала игрока");
        }

        private static string FirstLoadoutId(ServiceContainer services)
        {
            foreach (var loadout in services.Resolve<ILoadoutRepository>().All)
            {
                return loadout.Id;
            }

            throw new InvalidOperationException("В данных нет наборов снаряжения");
        }

        private static CombatState StartCombat(
            ServiceContainer services,
            ICombatService combat,
            PlayerState playerState,
            string? enemyId = null)
        {
            var rules = services.Resolve<IGameRulesRepository>().Require();
            var actors = services.Resolve<IActorRepository>();
            var player = actors.Get(rules.PlayerActorId);
            var enemy = enemyId == null ? FirstEnemy(services, player.Id) : actors.Get(enemyId);
            var equipment = playerState.Equipment ??
                            throw new InvalidOperationException("У игрока нет экипировки");
            return combat.Start(player, equipment, enemy);
        }

        private static ActorTemplate FirstEnemy(ServiceContainer services, string playerActorId)
        {
            foreach (var actor in services.Resolve<IActorRepository>().All)
            {
                if (actor.Combat != null && !string.Equals(actor.Id, playerActorId, StringComparison.Ordinal))
                {
                    return actor;
                }
            }

            throw new InvalidOperationException("В данных нет противника");
        }

        private static IReadOnlyList<string> PlayTurns(
            ICombatService combat,
            ICombatAi ai,
            CombatState state,
            IGameTexts texts)
        {
            var lines = new List<string>();
            var weapon = combat.PlayerWeapon(state);
            if (weapon == null)
            {
                return lines;
            }

            for (var shot = 0; shot < LogActions; shot++)
            {
                if (!combat.TryAttack(state, weapon, out var hit))
                {
                    break;
                }

                lines.Add(hit.Hit
                    ? UiText.Format(texts, UiTextKey.Combat_Log_Hit, hit.Damage)
                    : UiText.Get(texts, UiTextKey.Combat_Log_Miss));
            }

            combat.EndTurn(state);
            while (ai.TryExecuteNext(state, out var action))
            {
                lines.Add(EnemyLine(texts, action));
            }

            combat.EndTurn(state);
            return lines;
        }

        private static string EnemyLine(IGameTexts texts, EnemyTurnAction action)
        {
            switch (action.Step.Kind)
            {
                case EnemyTurnActionKind.Reload:
                    return UiText.Get(texts, UiTextKey.Combat_Log_Enemy_Reload);
                case EnemyTurnActionKind.Medkit:
                    return UiText.Format(texts, UiTextKey.Combat_Log_Enemy_Medkit, action.Healed);
                default:
                    return action.Hit
                        ? UiText.Format(texts, UiTextKey.Combat_Log_Enemy_Hit, action.Damage)
                        : UiText.Get(texts, UiTextKey.Combat_Log_Enemy_Miss);
            }
        }

        private static void Fill(
            GameObject panel,
            MenuContext menu,
            IProgressionService progression,
            CombatState combat,
            PlayerState player,
            IGameTexts texts,
            IItemRepository items,
            SpriteCatalog? catalog,
            MenuKind kind,
            IReadOnlyList<string> log)
        {
            ApplyLabels(panel, texts);
            FillCombatants(panel, combat, texts, items, catalog);
            FillFigures(panel, combat, items, catalog);
            FillProgress(panel, progression, player, texts);
            FillQuickSlots(panel, combat, items, catalog, texts);
            FillMenu(panel, menu, combat, texts, kind);
            FillJournal(panel, log);
            if (kind != MenuKind.None)
            {
                ShowTooltip(panel, texts, menu.Settings);
            }
        }

        /// <summary>Показывает подсказку с текстом первой строки открытой выпадашки.</summary>
        /// <param name="panel">Экземпляр префаба.</param>
        /// <param name="texts">Тексты игры.</param>
        /// <param name="settings">Цены действий.</param>
        private static void ShowTooltip(
            GameObject panel,
            IGameTexts texts,
            ICombatSettings settings)
        {
            var tooltip = FindDeep(panel.transform, "CombatTooltip");
            var label = FindPath(panel.transform, "CombatTooltip/Text");
            if (tooltip == null || label == null)
            {
                return;
            }

            var menu = FindDeep(panel.transform, "CombatSkillMenu");
            var hovered = string.Empty;
            if (menu != null)
            {
                foreach (Transform child in menu)
                {
                    if (!child.gameObject.activeSelf)
                    {
                        continue;
                    }

                    var childLabel = child.GetComponentInChildren<Text>();
                    if (childLabel != null)
                    {
                        hovered = childLabel.text;
                        break;
                    }
                }
            }

            if (hovered.Length == 0)
            {
                hovered = UiText.Format(
                    texts,
                    UiTextKey.Combat_Skill_Button_No_Ammo,
                    UiText.Get(texts, UiTextKey.Combat_Reload),
                    settings.ReloadApCost);
            }

            tooltip.gameObject.SetActive(true);
            label.GetComponent<Text>().text = hovered;
        }

        private static void FillCombatants(
            GameObject panel,
            CombatState combat,
            IGameTexts texts,
            IItemRepository items,
            SpriteCatalog? catalog)
        {
            var player = combat.Player;
            var enemy = combat.Enemy;
            SetText(panel, "PlayerPlate/Name", texts.Get(player.Template.NameKey));
            SetText(panel, "PlayerPlate/HealthBar/Health", UiText.Format(
                texts,
                UiTextKey.Combat_Hp,
                Math.Round(player.Health),
                player.MaxHealth));
            SetFill(panel, "PlayerPlate/HealthBar/Fill", Fraction(player.Health, player.MaxHealth));
            SetText(panel, "EnemyPlate/Name", texts.Get(enemy.Template.NameKey));
            SetText(panel, "EnemyPlate/HealthBar/Health", UiText.Format(
                texts,
                UiTextKey.Combat_Hp,
                Math.Round(enemy.Health),
                enemy.MaxHealth));
            SetFill(panel, "EnemyPlate/HealthBar/Fill", Fraction(enemy.Health, enemy.MaxHealth));
            FillAp(panel, player.Ap, player.MaxAp);
            FillWeapon(panel, "PlayerWeapon", items, catalog, player.Equipment?.Get(combat.PlayerWeaponSlot), texts);
            FillWeapon(panel, "EnemyWeapon", items, catalog, combat.EnemyWeapon, texts);
        }

        private static void FillAp(GameObject panel, int ap, int maxAp)
        {
            var row = FindDeep(panel.transform, "ApRow");
            var template = FindDeep(panel.transform, "ApTemplate");
            if (row == null || template == null)
            {
                return;
            }

            var image = template.GetComponent<Image>();
            template.gameObject.SetActive(false);
            for (var index = 0; index < maxAp; index++)
            {
                var circle = UnityEngine.Object.Instantiate(image, row);
                circle.gameObject.name = "Ap" + index;
                circle.gameObject.SetActive(true);
                circle.color = index < ap ? CombatPalette.ApOn : CombatPalette.ApOff;
            }
        }

        private static void FillWeapon(
            GameObject panel,
            string plate,
            IItemRepository items,
            SpriteCatalog? catalog,
            ItemInstance? weapon,
            IGameTexts texts)
        {
            var iconTransform = FindPath(panel.transform, plate + "/Icon");
            var ammo = FindPath(panel.transform, plate + "/Ammo");
            if (weapon == null)
            {
                if (iconTransform != null)
                {
                    iconTransform.GetComponent<Image>().enabled = false;
                }

                if (ammo != null)
                {
                    ammo.GetComponent<Text>().text = UiText.Get(texts, UiTextKey.Combat_No_Weapon);
                }

                return;
            }

            var template = items.Get(weapon.ItemId);
            if (iconTransform != null && catalog != null && catalog.TryGet(template.Icon, out var sprite) &&
                sprite != null)
            {
                var image = iconTransform.GetComponent<Image>();
                image.sprite = sprite;
                image.enabled = true;
            }

            if (ammo != null)
            {
                ammo.GetComponent<Text>().text = UiText.Format(
                    texts,
                    UiTextKey.Combat_Ammo,
                    weapon.MagazineRounds,
                    template.Weapon?.Magazine ?? 0);
            }
        }

    }
}
