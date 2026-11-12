#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.Stage;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>Часть класса <see cref="PlayScreen"/>: фигуры, иконки и объекты сцены локации.</summary>
    public sealed partial class PlayScreen
    {
        private void BuildFigures(GameSession session, LocationInstance location, LocationTemplate template)
        {
            var active = new List<LocationObject>(2);
            var inactive = new List<LocationObject>(2);
            if (location.Subtype != LocationSubtype.Hub)
            {
                foreach (var locationObject in location.Objects)
                {
                    if (locationObject is EnemyLocationObject || !CanShow(session, locationObject))
                    {
                        continue;
                    }

                    if (CanClick(locationObject))
                    {
                        active.Add(locationObject);
                    }
                    else
                    {
                        inactive.Add(locationObject);
                    }
                }
            }

            var ordered = new List<LocationObject>(active);
            KeepShown(ordered, _leftObject, active, inactive);
            KeepShown(ordered, _rightObject, active, inactive);
            foreach (var idle in inactive)
            {
                if (!ordered.Contains(idle))
                {
                    ordered.Add(idle);
                }
            }

            _leftObject = ordered.Count > 0 ? ordered[0] : null;
            _rightObject = location.StashPresent ? null : (ordered.Count > 1 ? ordered[1] : null);
            if (_leftObject != null)
            {
                ShowFigure(_objectLeft, session, _leftObject, template, StageSlot.Left);
            }

            if (_rightObject != null)
            {
                ShowFigure(_objectRight, session, _rightObject, template, StageSlot.Right);
            }
            else if (location.StashPresent)
            {
                ShowStash(location, template);
            }

            ShowEnemy(session, location, template);
        }

        /// <summary>Тайник на сцене: правый слот и высота тайника из блока локации.</summary>
        /// <param name="location">Текущая локация.</param>
        /// <param name="template">Шаблон локации: его сцена задаёт точку и высоту.</param>
        private void ShowStash(LocationInstance location, LocationTemplate template)
        {
            var title = UiText.Get(
                _host.Texts,
                location.StashOpened ? UiTextKey.Game_Stash_Opened : UiTextKey.Game_Stash);
            var hint = UiText.Get(
                _host.Texts,
                location.StashOpened ? UiTextKey.Game_Object_Hint : UiTextKey.Game_Stash_Hint);
            const string iconId = PlaySceneArt.StashIcon;
            var art = Icon(iconId);
            if (TryShowStage(
                    _objectRight,
                    template,
                    StageSlot.Right,
                    null,
                    art,
                    OpenStash,
                    !location.StashOpened,
                    "Stash",
                    iconId,
                    template.Stage?.StashHeight))
            {
                return;
            }

            _objectRight.Show(title, hint, OpenStash, !location.StashOpened, "Stash", art);
        }

        /// <summary>
        /// Пробует поставить фигуру на сцену: нужны блок локации, точка слота, картинка
        /// и высота в метрах. Нет хотя бы одного — фигура рисуется прежней плашкой.
        /// </summary>
        /// <param name="figure">Фигура префаба.</param>
        /// <param name="template">Шаблон локации.</param>
        /// <param name="slot">Слот сцены.</param>
        /// <param name="spawnId">Id записи спавна: из неё берётся высота; <c>null</c> — только явная высота.</param>
        /// <param name="art">Картинка фигуры; <c>null</c> — плашка.</param>
        /// <param name="click">Что выполнить по нажатию.</param>
        /// <param name="enabled">Кликабельна ли фигура.</param>
        /// <param name="objectName">Имя объекта в иерархии.</param>
        /// <param name="iconId">Id вырезки из данных: по нему правый слот берёт зеркальную копию.</param>
        /// <param name="height">Высота в метрах, если она не из записи спавна (тайник).</param>
        /// <returns><c>true</c>, если фигура поставлена на сцену.</returns>
        private bool TryShowStage(
            PlayObject figure,
            LocationTemplate template,
            StageSlot slot,
            string? spawnId,
            Sprite? art,
            Action click,
            bool enabled,
            string? objectName,
            string? iconId = null,
            double? height = null)
        {
            if (template.Stage == null || art == null || !_scene.CanStage)
            {
                return false;
            }

            var anchor = template.Stage.Anchor(slot);
            if (anchor == null)
            {
                return false;
            }

            if (slot == StageSlot.Right && iconId != null)
            {
                art = Icon(StageArt.FigureId(iconId, slot)) ?? art;
            }

            var meters = height;
            if (meters == null && spawnId != null &&
                _host.Spawns.TryGet(spawnId, out var entry) && entry != null)
            {
                meters = entry.Height;
            }

            if (meters is not > 0)
            {
                return false;
            }

            var sceneHeight = _scene.StageHeight(anchor, meters.Value);
            if (sceneHeight <= 0f)
            {
                return false;
            }

            var ground = _scene.ToStagePoint(anchor.X, anchor.Y);
            var tint = PlayPalette.SceneTint(template.Stage.Tint, template.Stage.Brightness);
            figure.ShowScene(art, ground, sceneHeight, tint, click, enabled, objectName);
            return true;
        }

        /// <summary>
        /// Оставляет ранее показанный объект на месте, если он ещё на локации и свободного
        /// места хватает: так забранный объект не подменяется старым пустым.
        /// </summary>
        /// <param name="ordered">Уже выбранные объекты.</param>
        /// <param name="shown">Объект, который был на этом месте.</param>
        /// <param name="active">Действующие объекты.</param>
        /// <param name="inactive">Неактивные объекты.</param>
        private static void KeepShown(
            List<LocationObject> ordered,
            LocationObject? shown,
            List<LocationObject> active,
            List<LocationObject> inactive)
        {
            if (shown == null || ordered.Contains(shown) || ordered.Count >= 2)
            {
                return;
            }

            if (active.Contains(shown) || inactive.Contains(shown))
            {
                ordered.Add(shown);
            }
        }

        /// <summary>
        /// Может ли объект появиться на сцене: NPC без доступного разговора не занимает
        /// место, у которого нет действия.
        /// </summary>
        /// <param name="session">Текущая сессия.</param>
        /// <param name="locationObject">Объект локации.</param>
        /// <returns><c>true</c>, если объект показывается.</returns>
        private bool CanShow(GameSession session, LocationObject locationObject)
        {
            if (locationObject is not NpcLocationObject npc || !_host.Actors.Contains(npc.ActorId))
            {
                return locationObject is not NpcLocationObject;
            }

            var actor = _host.Actors.Get(npc.ActorId);
            var dialogId = actor.DialogId;
            return dialogId != null && _host.Dialogs.CanStart(session, dialogId);
        }

        /// <summary>Можно ли нажать объект: пустой и забранный объект лута показывается неактивным.</summary>
        /// <param name="locationObject">Объект локации.</param>
        /// <returns><c>true</c>, если у объекта есть действие.</returns>
        private static bool CanClick(LocationObject locationObject) =>
            locationObject is not LootLocationObject loot || (loot.HasLoot && !loot.Looted);

        private void ShowFigure(
            PlayObject figure,
            GameSession session,
            LocationObject locationObject,
            LocationTemplate template,
            StageSlot slot)
        {
            switch (locationObject)
            {
                case LootLocationObject loot:
                {
                    var enabled = loot.HasLoot && !loot.Looted;
                    var iconId = LootIconId(loot);
                    Show(
                        figure,
                        template,
                        slot,
                        loot.SpawnId,
                        iconId,
                        Icon(iconId),
                        UiText.Get(_host.Texts, loot.TitleKey),
                        UiText.Get(_host.Texts, UiTextKey.Game_Object_Hint),
                        () => OpenLootObject(loot),
                        enabled);
                    break;
                }

                case AnomalyLocationObject anomaly:
                {
                    var iconId = AnomalyIconId(anomaly);
                    Show(
                        figure,
                        template,
                        slot,
                        anomaly.SpawnId,
                        iconId,
                        Icon(iconId),
                        UiText.Get(_host.Texts, anomaly.TitleKey),
                        UiText.Get(_host.Texts, UiTextKey.Game_Object_Hint),
                        () => _host.ShowRadar(anomaly),
                        true);
                    break;
                }

                case NpcLocationObject npc:
                {
                    if (!_host.Actors.Contains(npc.ActorId))
                    {
                        return;
                    }

                    var actor = _host.Actors.Get(npc.ActorId);
                    var dialogId = actor.DialogId;
                    if (dialogId == null || !_host.Dialogs.CanStart(session, dialogId))
                    {
                        return;
                    }

                    var actorId = npc.ActorId;
                    var portrait = actor.Portrait;
                    Show(
                        figure,
                        template,
                        slot,
                        npc.SpawnId,
                        portrait,
                        Icon(portrait),
                        _host.Texts.Get(actor.NameKey),
                        UiText.Format(_host.Texts, UiTextKey.Game_Talk_Action, _host.Texts.Get(actor.NameKey)),
                        () => _host.ShowDialog(actorId, dialogId),
                        true);
                    break;
                }
            }
        }

        /// <summary>
        /// Показывает объект: сцена, если локация размечена и у объекта есть высота с картинкой,
        /// иначе прежняя плашка с названием и подсказкой.
        /// </summary>
        private void Show(
            PlayObject figure,
            LocationTemplate template,
            StageSlot slot,
            string spawnId,
            string? iconId,
            Sprite? art,
            string title,
            string hint,
            Action click,
            bool enabled)
        {
            if (TryShowStage(figure, template, slot, spawnId, art, click, enabled, "Object_" + spawnId, iconId))
            {
                return;
            }

            figure.Show(title, hint, click, enabled, "Object_" + spawnId, art);
        }

        private void ShowEnemy(GameSession session, LocationInstance location, LocationTemplate template)
        {
            if (location.Enemy is not EnemyLocationObject enemy ||
                enemy.Looted ||
                !_host.Actors.Contains(enemy.ActorId))
            {
                return;
            }

            var actor = _host.Actors.Get(enemy.ActorId);
            var actorId = enemy.ActorId;
            Show(
                _enemy,
                template,
                StageSlot.Center,
                enemy.SpawnId,
                actor.Portrait,
                Icon(actor.Portrait),
                _host.Texts.Get(actor.NameKey),
                UiText.Format(_host.Texts, UiTextKey.Game_Enemy_Action, _host.Texts.Get(actor.NameKey)),
                () => EnterCombat(actorId),
                true);
        }

        /// <summary>Текстура из каталога спрайтов по id; <c>null</c>, если id пуст или картинки нет.</summary>
        /// <param name="id">Id картинки в каталоге.</param>
        /// <returns>Спрайт или <c>null</c>.</returns>
        private Sprite? Icon(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return _host.Sprites.TryGet(id!, out var sprite) ? sprite : null;
        }

        /// <summary>
        /// Id картинки объекта лута: он берётся из записи спавна, поэтому вид объекта
        /// (ящик, машина, шкаф) задаётся данными, а не кодом. Нет записи — остаётся
        /// эллипс-заглушка.
        /// </summary>
        /// <param name="loot">Объект лута с id записи спавна.</param>
        /// <returns>Id иконки или <c>null</c>.</returns>
        private string? LootIconId(LootLocationObject loot) =>
            _host.Spawns.TryGet(loot.SpawnId, out var entry) && entry != null ? entry.Icon : null;

        /// <summary>
        /// Id текстуры аномалии: картинка из записи её спавна (<c>icon</c>, диорама самой
        /// зоны); без поля — иконка артефакта её типа из данных предметов, как было до
        /// появления собственных картинок аномалий.
        /// </summary>
        /// <param name="anomaly">Объект-аномалия текущей локации.</param>
        /// <returns>Id иконки или <c>null</c>.</returns>
        private string? AnomalyIconId(AnomalyLocationObject anomaly)
        {
            if (_host.Spawns.TryGet(anomaly.SpawnId, out var entry) && entry != null &&
                !string.IsNullOrWhiteSpace(entry.Icon))
            {
                return entry.Icon;
            }

            if (!_host.Anomalies.TryGet(anomaly.AnomalyId, out var template) || template == null)
            {
                return null;
            }

            foreach (var item in _host.Items.All)
            {
                if (item.Category == ItemCategory.Artifact &&
                    string.Equals(item.ArtifactType, template.ArtifactType, StringComparison.Ordinal))
                {
                    return item.Icon;
                }
            }

            return null;
        }

        /// <summary>
        /// Открывает лут, выпавший объекту при спавне. Забранное убирается из объекта,
        /// поэтому при следующем осмотре остаётся только то, что не взяли.
        /// </summary>
        private void OpenLootObject(LootLocationObject lootObject)
        {
            var session = _host.Session;
            if (session != null)
            {
                _host.StoryQuests.NotifyObjectInspected(
                    session,
                    session.State.World.CurrentLocationId,
                    LocationObjectKind.Loot,
                    lootObject.SpawnId);
            }

            if (!lootObject.HasLoot)
            {
                Show();
                return;
            }

            _host.ShowLootFromObject(
                lootObject.Loot,
                item => lootObject.Take(item.InstanceId),
                Show);
        }

        private void HideFigures()
        {
            _objectLeft.Hide();
            _objectRight.Hide();
            _enemy.Hide();
        }

    }
}
