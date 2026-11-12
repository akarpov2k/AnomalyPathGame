#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Rules;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Часть класса <see cref="PlayScreen"/>: бесконечный боковой поход из префаба WalkLane.
    /// Дорога сама продолжается вправо; от неё отходят ответвления «вверх», в которые можно
    /// войти кнопкой. Зона заканчивается — игрок сам возвращается на новую дорогу.
    /// </summary>
    public sealed partial class PlayScreen
    {
        private WalkSettings? _walkSettings;
        private string? _walkLocationId;
        private bool _walkHasParts;
        private bool _walkBranchRaised;
        private WalkFigure? _walkNear;
        private WalkBranch? _walkNearBranch;
        private float _walkCamera;
        private float _walkForwardMax;
        private double _walkMinutesCarry;
        private bool _walkAtEnd;
        private bool _walkHeld;
        private bool _walkRunHeld;
        private bool _walkIndoor;
        private bool _walkReady;
        private bool _walkSwitching;
        private readonly List<WalkFigure> _walkSlots = new List<WalkFigure>(WalkMetrics.FigureCount);
        private readonly List<WalkFigure> _walkActive = new List<WalkFigure>(WalkMetrics.FigureCount);
        private readonly List<WalkBranch> _walkBranches = new List<WalkBranch>(WalkMetrics.BranchCount);
        private readonly float[] _walkSegmentPathX = new float[WalkMetrics.BackdropPartCount];

        /// <summary>Включает или выключает режим похода и перестраивает экран.</summary>
        /// <param name="enabled">Включить режим.</param>
        public void SetWalkMode(bool enabled)
        {
            walkMode = enabled;
            if (enabled)
            {
                LegacyModeForTests = false;
            }

            Show();
        }

        /// <summary>Меняет флаг режима без перерисовки — для тестов и переключения по данным.</summary>
        /// <param name="enabled">Включить режим.</param>
        public void SetWalkModeFlag(bool enabled) => walkMode = enabled;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            {
                walkMode = !walkMode;
                Show();
                return;
            }

            if (!walkMode || !_walkReady || _walkSwitching || walkLane == null ||
                !walkLane.gameObject.activeInHierarchy)
            {
                return;
            }

            if (_host.Session == null)
            {
                return;
            }

            if (WalkInteractPressed())
            {
                UpdateWalkNear();
                if (_walkNear != null)
                {
                    InteractWalk();
                    return;
                }
            }

            var settings = _walkSettings;
            var lane = settings != null ? (float)settings.LaneLength : walkLaneLength;
            var forwardKey = WalkForwardPressed();
            var backwardKey = WalkBackwardPressed();
            var runKey = Keyboard.current != null &&
                         (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            var forward = _walkHeld || _walkRunHeld || forwardKey;
            var backward = !forward && backwardKey;
            if (!forward && !backward)
            {
                walkLane.SetPlayerState(false, false, false);
                walkLane.SetPlayerY(walkGroundY);
                UpdateWalkNear();
                UpdateWalkBranches();
                return;
            }

            var running = _walkRunHeld || runKey;
            var runMultiplier = settings != null
                ? (float)settings.RunMultiplier
                : (float)WalkSettings.Default.RunMultiplier;
            var speed = (settings != null ? (float)settings.Speed : walkSpeed) * (running ? runMultiplier : 1f);
            var previous = _walkCamera;
            if (forward)
            {
                var limit = Mathf.Max(0f, lane - walkPlayerX);
                _walkCamera = Mathf.Min(_walkCamera + (speed * Time.deltaTime), limit);
                _walkForwardMax = Mathf.Max(_walkForwardMax, _walkCamera);
            }
            else
            {
                var factor = settings != null
                    ? (float)settings.BacktrackFactor
                    : (float)WalkSettings.Default.BacktrackFactor;
                var backLimit = settings != null
                    ? (float)settings.BackLimit(_walkForwardMax)
                    : Mathf.Max(0f, _walkForwardMax - (_walkForwardMax / factor));
                _walkCamera = Mathf.Max(_walkCamera - (speed * Time.deltaTime), backLimit);
            }

            walkLane.SetWorldOffset(_walkCamera);
            walkLane.SetPlayerState(true, running, backward);
            walkLane.SetPlayerY(walkGroundY);
            AdvanceWalkTime(previous, settings);
            UpdateWalkSky();
            UpdateWalkProgress();
            UpdateWalkNear();
            UpdateWalkBranches();
            if (forward && _walkCamera >= lane - walkPlayerX && !_walkAtEnd)
            {
                _walkAtEnd = true;
                BeginAutoWalk();
            }
        }

        private static bool WalkForwardPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.wKey.isPressed || keyboard.dKey.isPressed ||
                    keyboard.upArrowKey.isPressed || keyboard.rightArrowKey.isPressed);
        }

        private static bool WalkBackwardPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.sKey.isPressed || keyboard.aKey.isPressed ||
                    keyboard.downArrowKey.isPressed || keyboard.leftArrowKey.isPressed);
        }

        /// <summary>Взаимодействие с ближайшим объектом: Enter, E или F.</summary>
        private static bool WalkInteractPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.eKey.wasPressedThisFrame ||
                    keyboard.fKey.wasPressedThisFrame);
        }

        private bool EnsureWalkLane()
        {
            if (_walkReady)
            {
                return walkLane != null;
            }

            if (walkLane == null)
            {
                Debug.LogError("PlayScreen: WalkLane prefab is not assigned");
                walkMode = false;
                return false;
            }

            _walkSlots.Clear();
            foreach (var figure in walkLane.Figures)
            {
                if (figure != null)
                {
                    _walkSlots.Add(new WalkFigure(figure));
                    figure.Hide();
                }
            }

            SetButtonLabel(walkLane.Forward, UiText.Get(_host.Texts, UiTextKey.Game_Walk_Go));
            SetButtonLabel(walkLane.Run, UiText.Get(_host.Texts, UiTextKey.Game_Walk_Run));
            SetButtonLabel(walkLane.ReturnToHub, UiText.Get(_host.Texts, UiTextKey.Game_Walk_Home));
            AddWalkHold(walkLane.Forward);
            AddWalkRunHold(walkLane.Run);
            if (walkLane.ReturnToHub != null)
            {
                walkLane.ReturnToHub.onClick.RemoveAllListeners();
                walkLane.ReturnToHub.onClick.AddListener(ReturnToHub);
            }

            if (walkLane.Interact != null)
            {
                walkLane.Interact.onClick.RemoveAllListeners();
                walkLane.Interact.onClick.AddListener(InteractWalk);
            }

            if (walkLane.Up != null)
            {
                walkLane.Up.onClick.RemoveAllListeners();
                walkLane.Up.onClick.AddListener(EnterWalkBranch);
            }

            walkLane.SetUp(false, UiText.Get(_host.Texts, UiTextKey.Game_Walk_Up));
            walkLane.HideBranchesFrom(0);
            walkLane.SetVisible(false);
            _walkReady = true;
            return true;
        }

        private void BuildWalk(GameSession session, LocationInstance location, LocationTemplate template)
        {
            if (!EnsureWalkLane() || walkLane == null)
            {
                return;
            }

            _walkSettings = _host.Rules.Require().Walk;
            if (!string.Equals(_walkLocationId, location.InstanceId, StringComparison.Ordinal))
            {
                _walkLocationId = location.InstanceId;
                _walkCamera = 0f;
                _walkForwardMax = 0f;
                _walkMinutesCarry = 0;
                _walkAtEnd = false;
            }

            var lane = _walkSettings != null ? (float)_walkSettings.LaneLength : walkLaneLength;
            walkLane.Configure(lane, walkPlayerX, walkGroundY, template.Interior);
            _walkHeld = false;
            _walkRunHeld = false;
            _walkNear = null;
            _walkNearBranch = null;
            _walkAtEnd = false;
            if (walkLane.Interact != null)
            {
                walkLane.Interact.gameObject.SetActive(false);
            }

            walkLane.SetUp(false, UiText.Get(_host.Texts, UiTextKey.Game_Walk_Up));
            ApplyWalkBackdrop(template, location);
            _walkIndoor = template.Interior;
            UpdateWalkSky();
            BuildWalkFigures(session, location, template);
            BuildWalkBranches(session, lane);
            walkLane.SetWorldOffset(_walkCamera);
            walkLane.SetPlayerState(false, false, false);
            walkLane.SetPlayerY(walkGroundY);
            UpdateWalkProgress();
            UpdateWalkNeeds();
            walkLane.SetVisible(true);
        }

        private void HideWalk(bool reset = true)
        {
            if (!_walkReady || walkLane == null)
            {
                return;
            }

            walkLane.SetVisible(false);
            walkLane.SetUp(false, UiText.Get(_host.Texts, UiTextKey.Game_Walk_Up));
            walkLane.HideBranchesFrom(0);
            walkLane.SetPlayerState(false, false, false);
            _walkHeld = false;
            _walkRunHeld = false;
            _walkNear = null;
            _walkNearBranch = null;
            if (reset)
            {
                _walkLocationId = null;
                _walkCamera = 0f;
                _walkForwardMax = 0f;
                _walkMinutesCarry = 0;
                _walkAtEnd = false;
                _walkBranches.Clear();
            }
        }

        private void ShowWalkEnd()
        {
            var session = _host.Session;
            if (session == null)
            {
                return;
            }

            if (walkLane != null)
            {
                walkLane.SetVisible(false);
            }

            _scene.SetVisible(true);
            var world = session.State.World;
            _scene.SetTransitions(
                BuildTransitions(_host.Sessions.AvailableEdges(session), world, out var forwardIndex),
                forwardIndex);
            var canReturn = world.CurrentLocation.Subtype != LocationSubtype.Hub;
            _returnToHub.gameObject.SetActive(canReturn);
            SetEnabled(_returnToHub, true);
        }

        private void BuildWalkBranches(GameSession session, float lane)
        {
            _walkBranches.Clear();
            walkLane?.HideBranchesFrom(0);
            if (walkLane == null)
            {
                return;
            }

            var world = session.State.World;
            var candidates = new List<(WorldEdge Edge, string Title, string? SceneArt)>(WalkMetrics.BranchCount);
            foreach (var edge in _host.Sessions.AvailableEdges(session))
            {
                if (!IsForwardEdge(edge))
                {
                    continue;
                }

                if (!world.Map.TryGetLocation(edge.ToId, out var target) || target == null ||
                    target.Subtype == LocationSubtype.Path || target.Subtype == LocationSubtype.Hub)
                {
                    continue;
                }

                var template = _host.Locations.Get(target.TemplateId);
                candidates.Add((edge, _host.Texts.Get(template.TitleKey), template.SceneArt));
                if (candidates.Count >= WalkMetrics.BranchCount)
                {
                    break;
                }
            }

            var total = lane + WalkMetrics.WorldTail;
            var partWidth = total / WalkMetrics.BackdropPartCount;
            for (var index = 0; index < candidates.Count; index++)
            {
                // Переход стоит на вертикальной тропе своего сегмента: её долю ширины
                // задаёт walkPaths, а если тропы нет — центр сегмента. По высоте он поднят
                // над горизонтальной дорогой, когда вертикальная тропа задана. За конец
                // дороги ветка не уходит: дальше игрок до неё не дойдёт.
                var x = _walkHasParts
                    ? Mathf.Min(
                        lane,
                        (index * partWidth) + (partWidth * _walkSegmentPathX[index]))
                    : Mathf.Min(
                        lane - 520f,
                        (lane * WalkMetrics.BranchFirstFraction) + (index * WalkMetrics.BranchGap));
                var y = _walkBranchRaised ? walkGroundY + WalkMetrics.BranchRoadOffset : walkGroundY;
                _walkBranches.Add(new WalkBranch(candidates[index].Edge, x, candidates[index].Title));
                walkLane.ShowBranch(
                    index,
                    candidates[index].Title,
                    x,
                    y,
                    Icon(candidates[index].SceneArt),
                    null);
            }

            walkLane.HideBranchesFrom(candidates.Count);
        }

        private void UpdateWalkBranches()
        {
            if (walkLane == null)
            {
                return;
            }

            WalkBranch? best = null;
            var bestDistance = float.MaxValue;
            foreach (var branch in _walkBranches)
            {
                var screenX = branch.WorldX - _walkCamera;
                if (screenX < walkPlayerX - WalkMetrics.BranchRange)
                {
                    continue;
                }

                var distance = Mathf.Abs(screenX - walkPlayerX);
                if (distance > WalkMetrics.BranchRange || distance >= bestDistance)
                {
                    continue;
                }

                best = branch;
                bestDistance = distance;
            }

            if (best == _walkNearBranch)
            {
                return;
            }

            _walkNearBranch = best;
            walkLane.SetUp(best != null, UiText.Get(_host.Texts, UiTextKey.Game_Walk_Up));
        }

        private void EnterWalkBranch()
        {
            if (_walkNearBranch == null || _walkSwitching)
            {
                return;
            }

            var branch = _walkNearBranch;
            _walkNearBranch = null;
            walkLane?.SetUp(false, UiText.Get(_host.Texts, UiTextKey.Game_Walk_Up));
            BeginWalkSwitch(branch.Edge, up: true);
        }

        private void BeginAutoWalk()
        {
            var session = _host.Session;
            if (session == null)
            {
                return;
            }

            var edge = PickAutoEdge(session);
            if (edge == null)
            {
                ShowWalkEnd();
                return;
            }

            BeginWalkSwitch(edge, up: false);
        }

        private WorldEdge? PickAutoEdge(GameSession session)
        {
            var world = session.State.World;
            var current = world.CurrentLocation;
            WorldEdge? pathOut = null;
            WorldEdge? otherOut = null;
            WorldEdge? pathAny = null;
            WorldEdge? hub = null;
            WorldEdge? anywhere = null;
            foreach (var edge in _host.Sessions.AvailableEdges(session))
            {
                if (!world.Map.TryGetLocation(edge.ToId, out var target) || target == null)
                {
                    continue;
                }

                anywhere ??= edge;

                if (IsForwardEdge(edge))
                {
                    if (target.Subtype == LocationSubtype.Path)
                    {
                        pathOut ??= edge;
                    }
                    else if (target.Subtype != LocationSubtype.Hub)
                    {
                        otherOut ??= edge;
                    }
                    else
                    {
                        hub ??= edge;
                    }
                }
                else if (target.Subtype == LocationSubtype.Path)
                {
                    pathAny ??= edge;
                }
            }

            return current.Subtype == LocationSubtype.Path
                ? pathOut ?? anywhere
                : pathOut ?? otherOut ?? pathAny ?? hub ?? anywhere;
        }

        private void BeginWalkSwitch(WorldEdge edge, bool up)
        {
            if (_walkSwitching)
            {
                return;
            }

            StartCoroutine(SwitchWalk(edge, up));
        }

        private IEnumerator SwitchWalk(WorldEdge edge, bool up)
        {
            var resume = !up && _walkHeld;
            _walkSwitching = true;
            _walkHeld = false;
            _walkRunHeld = false;
            _walkNear = null;
            _walkNearBranch = null;
            if (walkLane?.Interact != null)
            {
                walkLane.Interact.gameObject.SetActive(false);
            }

            walkLane?.SetUp(false, UiText.Get(_host.Texts, UiTextKey.Game_Walk_Up));
            var group = walkLane?.Group;
            var duration = Mathf.Max(0.01f, WalkMetrics.TransitionSeconds);
            for (var time = 0f; time < duration; time += Time.deltaTime)
            {
                var t = Mathf.Clamp01(time / duration);
                walkLane?.SetPlayerY(walkGroundY + (up ? t * 280f : 0f));
                if (group != null)
                {
                    group.alpha = 1f - t;
                }

                yield return null;
            }

            var session = _host.Session;
            if (session == null)
            {
                if (group != null)
                {
                    group.alpha = 1f;
                }

                walkLane?.SetPlayerY(walkGroundY);
                _walkAtEnd = false;
                _walkSwitching = false;
                yield break;
            }

            var result = _host.Sessions.Travel(session, edge.EdgeId);
            if (!result.Succeeded)
            {
                if (group != null)
                {
                    group.alpha = 1f;
                }

                walkLane?.SetPlayerY(walkGroundY);
                walkLane?.SetVisible(true);
                _walkAtEnd = false;
                _walkSwitching = false;
                yield break;
            }

            _walkCamera = 0f;
            _walkForwardMax = 0f;
            _walkAtEnd = false;
            _walkBranches.Clear();
            Show();
            yield return null;
            _walkHeld = resume;
            for (var time = 0f; time < duration; time += Time.deltaTime)
            {
                var t = Mathf.Clamp01(time / duration);
                if (group != null)
                {
                    group.alpha = t;
                }

                yield return null;
            }

            if (group != null)
            {
                group.alpha = 1f;
            }

            _walkSwitching = false;
        }

        private static bool IsForwardEdge(WorldEdge edge) =>
            edge.EdgeId.EndsWith(".out", StringComparison.Ordinal);

        private void ApplyWalkBackdrop(LocationTemplate template, LocationInstance location)
        {
            if (walkLane == null)
            {
                return;
            }

            var art = BackdropSprite(template);
            if (template.WalkParts.Count == WalkMetrics.BackdropPartCount)
            {
                var sprites = new Sprite?[WalkMetrics.BackdropPartCount];
                var complete = true;
                for (var index = 0; index < sprites.Length; index++)
                {
                    sprites[index] = Icon(template.WalkParts[index]);
                    complete &= sprites[index] != null;
                }

                if (complete)
                {
                    var order = ShuffleBackdropParts(location.Seed);
                    var shuffled = new Sprite?[WalkMetrics.BackdropPartCount];
                    for (var index = 0; index < shuffled.Length; index++)
                    {
                        shuffled[index] = sprites[order[index]];
                        _walkSegmentPathX[index] = PathFraction(template, order[index]);
                    }

                    _walkHasParts = true;
                    _walkBranchRaised = !template.Interior && template.WalkPaths.Count > 0;
                    walkLane.SetRoad(null);
                    walkLane.SetBackdropParts(shuffled);
                    if (template.Interior)
                    {
                        // В помещении небо скрыто, а кадр стоит на полосе пола: по ней
                        // персонаж бежит, она едет вместе с миром. Без неё он «летит».
                        walkLane.SetRoad(FloorSprite(template));
                    }

                    return;
                }
            }

            _walkHasParts = false;
            _walkBranchRaised = false;
            ResetSegmentPathX();
            walkLane.SetBackdropParts(null);
            if (template.Interior)
            {
                // Кадров-сегментов нет: один кадр на всю высоту и полоса пола под ним.
                walkLane.SetFullBackdrop(art);
                walkLane.SetRoad(FloorSprite(template));
                return;
            }

            walkLane.SetRoad(FloorSprite(template));
            walkLane.SetBackdrop(art);
        }

        /// <summary>Текстура пола режима похода: своя у локации, иначе общая.</summary>
        /// <param name="template">Шаблон локации.</param>
        /// <returns>Спрайт пола или <c>null</c>, если текстуры нет.</returns>
        private Sprite? FloorSprite(LocationTemplate template) =>
            !string.IsNullOrWhiteSpace(template.WalkRoad) ? Icon(template.WalkRoad) : walkRoad;

        /// <summary>Картинка фона похода: своя у локации, иначе дорога или первый фон.</summary>
        /// <param name="template">Шаблон локации.</param>
        /// <returns>Спрайт или <c>null</c>, если картинки нет.</returns>
        private Sprite? BackdropSprite(LocationTemplate template)
        {
            Sprite? art = !string.IsNullOrWhiteSpace(template.WalkBackdrop)
                ? Icon(template.WalkBackdrop)
                : walkBackdrop;
            if (art == null)
            {
                art = Icon(template.RoadArt);
            }

            if (art == null && template.Backgrounds.Count > 0)
            {
                art = Icon(template.Backgrounds[0]);
            }

            return art;
        }

        private void ResetSegmentPathX()
        {
            for (var index = 0; index < _walkSegmentPathX.Length; index++)
            {
                _walkSegmentPathX[index] = 0.5f;
            }
        }

        /// <summary>
        /// Порядок трёх частей фона: детерминированная перестановка по seed локации,
        /// поэтому один и тот же заход всегда даёт одинаковую картину.
        /// </summary>
        /// <param name="seed">Seed экземпляра локации.</param>
        /// <returns>Индексы частей слева направо.</returns>
        private static int[] ShuffleBackdropParts(int seed)
        {
            var order = new[] { 0, 1, 2 };
            var random = new SeededWorldRandom(StableHash.Combine(seed, "walk_parts"));
            for (var index = order.Length - 1; index > 0; index--)
            {
                var swap = random.NextInt(0, index + 1);
                (order[index], order[swap]) = (order[swap], order[index]);
            }

            return order;
        }

        /// <summary>
        /// Доля ширины части фона, где нарисована вертикальная тропа: по ней ставится
        /// переход. Если данных нет — центр сегмента.
        /// </summary>
        /// <param name="template">Шаблон текущей локации.</param>
        /// <param name="part">Номер части в порядке данных.</param>
        /// <returns>Доля ширины от 0 до 1.</returns>
        private static float PathFraction(LocationTemplate template, int part)
        {
            var paths = template.WalkPaths;
            return part >= 0 && part < paths.Count ? (float)paths[part] : 0.5f;
        }

        private void BuildWalkFigures(GameSession session, LocationInstance location, LocationTemplate template)
        {
            foreach (var figure in _walkSlots)
            {
                figure.View.Hide();
                figure.Used = false;
            }

            _walkActive.Clear();
            var slot = 0;
            var totalSlots = location.Objects.Count + (location.StashPresent ? 1 : 0);
            foreach (var locationObject in location.Objects)
            {
                if (slot >= _walkSlots.Count)
                {
                    break;
                }

                if (locationObject is EnemyLocationObject enemy)
                {
                    if (!enemy.Looted && CanShow(session, locationObject))
                    {
                        AddWalkFigure(locationObject, template, slot);
                    }

                    slot++;
                    continue;
                }

                if (!CanShow(session, locationObject))
                {
                    slot++;
                    continue;
                }

                if (locationObject is LootLocationObject loot && (!loot.HasLoot || loot.Looted))
                {
                    slot++;
                    continue;
                }

                AddWalkFigure(locationObject, template, slot);
                slot++;
            }

            if (location.StashPresent && slot < _walkSlots.Count)
            {
                if (!location.StashOpened)
                {
                    AddWalkStash(slot);
                }

                slot++;
            }

            SpreadWalkFigures(totalSlots);
        }

        /// <summary>
        /// Раскладывает фигуры по всей дороге по их номеру места. Номер не зависит от того,
        /// сколько объектов ещё не обыскано, поэтому забранный предмет не сдвигает остальные.
        /// </summary>
        /// <param name="totalSlots">Сколько всего мест у локации: объекты и тайник.</param>
        private void SpreadWalkFigures(int totalSlots)
        {
            if (_walkActive.Count == 0)
            {
                return;
            }

            var count = Mathf.Max(1, totalSlots);
            var start = _walkSettings != null ? (float)_walkSettings.ObjectStart : walkObjectStartX;
            var lane = _walkSettings != null ? (float)_walkSettings.LaneLength : walkLaneLength;
            var end = Mathf.Max(start, lane - WalkMetrics.ObjectEndMargin);
            foreach (var figure in _walkActive)
            {
                var slot = Mathf.Clamp(figure.Slot, 0, count - 1);
                figure.WorldX = count == 1
                    ? start
                    : Mathf.Lerp(start, end, (float)slot / (count - 1));
                figure.View.SetPosition(new Vector2(figure.WorldX, walkGroundY));
            }
        }

        private void AddWalkFigure(LocationObject locationObject, LocationTemplate template, int slot)
        {
            if (slot < 0 || slot >= _walkSlots.Count)
            {
                return;
            }

            var figure = _walkSlots[slot];
            var title = string.Empty;
            var iconId = (string?)null;
            var action = string.Empty;
            var actorId = (string?)null;
            var dialogId = (string?)null;
            switch (locationObject)
            {
                case LootLocationObject loot:
                    title = _host.Texts.Get(loot.TitleKey);
                    iconId = LootIconId(loot);
                    action = UiText.Format(_host.Texts, UiTextKey.Game_Object_Action, title);
                    break;
                case AnomalyLocationObject anomaly:
                    title = _host.Texts.Get(anomaly.TitleKey);
                    iconId = AnomalyIconId(anomaly);
                    action = UiText.Format(_host.Texts, UiTextKey.Game_Anomaly_Search, title);
                    break;
                case NpcLocationObject npc:
                    if (!_host.Actors.Contains(npc.ActorId))
                    {
                        return;
                    }

                    var npcActor = _host.Actors.Get(npc.ActorId);
                    dialogId = npcActor.DialogId;
                    if (dialogId == null || !_host.Dialogs.CanStart(_host.Session!, dialogId))
                    {
                        return;
                    }

                    actorId = npc.ActorId;
                    title = _host.Texts.Get(npcActor.NameKey);
                    iconId = npcActor.Portrait;
                    action = UiText.Format(_host.Texts, UiTextKey.Game_Talk_Action, title);
                    break;
                case EnemyLocationObject enemy:
                    if (enemy.ActorId == null || !_host.Actors.Contains(enemy.ActorId))
                    {
                        return;
                    }

                    var enemyActor = _host.Actors.Get(enemy.ActorId);
                    actorId = enemy.ActorId;
                    title = _host.Texts.Get(enemyActor.NameKey);
                    iconId = enemyActor.Portrait;
                    action = UiText.Format(_host.Texts, UiTextKey.Game_Enemy_Action, title);
                    break;
                default:
                    return;
            }

            var height = 260f;
            if (_host.Spawns.TryGet(locationObject.SpawnId, out var entry) && entry != null &&
                entry.Height is > 0)
            {
                // Люди и противники выше предметов: 1,8 м × 350 = 630 px с обрезкой до 360,
                // мутанты 0,85–0,9 м дают 298–315 px (у игрока 380 px).
                var perMeter = locationObject is EnemyLocationObject || locationObject is NpcLocationObject
                    ? 350f
                    : 150f;
                height = Mathf.Clamp((float)entry.Height.Value * perMeter, 120f, 360f);
            }

            SetWalkFigure(figure, locationObject, actorId, dialogId, title, iconId, action, height, slot);
        }

        private void AddWalkStash(int slot)
        {
            if (slot < 0 || slot >= _walkSlots.Count)
            {
                return;
            }

            var figure = _walkSlots[slot];
            SetWalkFigure(
                figure,
                null,
                null,
                null,
                UiText.Get(_host.Texts, UiTextKey.Game_Stash),
                PlaySceneArt.StashIcon,
                UiText.Get(_host.Texts, UiTextKey.Game_Stash_Hint),
                210f,
                slot);
        }

        private void SetWalkFigure(
            WalkFigure figure,
            LocationObject? locationObject,
            string? actorId,
            string? dialogId,
            string title,
            string? iconId,
            string action,
            float height,
            int slot)
        {
            figure.View.Show(Icon(iconId), title, height);
            var start = _walkSettings != null ? (float)_walkSettings.ObjectStart : walkObjectStartX;
            var gap = _walkSettings != null ? (float)_walkSettings.ObjectGap : walkObjectGap;
            figure.WorldX = start + (slot * gap);
            figure.View.SetPosition(new Vector2(figure.WorldX, walkGroundY));
            figure.LocationObject = locationObject;
            figure.ActorId = actorId;
            figure.DialogId = dialogId;
            figure.Title = title;
            figure.Action = action;
            figure.Slot = slot;
            figure.Used = false;
            _walkActive.Add(figure);
        }

        private void UpdateWalkNear()
        {
            WalkFigure? best = null;
            var bestDistance = float.MaxValue;
            foreach (var figure in _walkActive)
            {
                if (figure.Used)
                {
                    continue;
                }

                var screenX = figure.WorldX - _walkCamera;
                var distance = Mathf.Abs(screenX - walkPlayerX);
                if (distance > walkInteractRange || distance >= bestDistance)
                {
                    continue;
                }

                best = figure;
                bestDistance = distance;
            }

            if (best == _walkNear)
            {
                return;
            }

            _walkNear = best;
            if (walkLane?.Interact == null)
            {
                return;
            }

            if (best == null)
            {
                walkLane.Interact.gameObject.SetActive(false);
                return;
            }

            walkLane.SetInteractLabel(best.Action);
            walkLane.Interact.gameObject.SetActive(true);
        }

        private void InteractWalk()
        {
            if (_walkNear == null)
            {
                return;
            }

            var figure = _walkNear;
            figure.Used = true;
            figure.View.SetUsed(true);
            _walkNear = null;
            if (walkLane?.Interact != null)
            {
                walkLane.Interact.gameObject.SetActive(false);
            }

            switch (figure.LocationObject)
            {
                case LootLocationObject loot:
                    OpenLootObject(loot);
                    return;
                case AnomalyLocationObject anomaly:
                    _host.ShowRadar(anomaly);
                    return;
                case NpcLocationObject npc:
                    if (figure.ActorId != null && figure.DialogId != null)
                    {
                        _host.ShowDialog(figure.ActorId, figure.DialogId);
                    }

                    return;
                case EnemyLocationObject enemy:
                    if (figure.ActorId != null)
                    {
                        EnterCombat(figure.ActorId);
                    }

                    return;
                case null:
                    OpenStash();
                    return;
            }
        }

        private void UpdateWalkProgress()
        {
            if (!_walkReady || walkLane == null)
            {
                return;
            }

            var lane = _walkSettings != null ? (float)_walkSettings.LaneLength : walkLaneLength;
            var progress = Mathf.Clamp01(_walkCamera / Mathf.Max(1f, lane - walkPlayerX));
            walkLane.SetProgress(
                progress,
                string.Format(
                    UiText.Get(_host.Texts, UiTextKey.Game_Walk_Path),
                    Mathf.RoundToInt(progress * 100f)));
        }

        private void AdvanceWalkTime(float previousCamera, WalkSettings? settings)
        {
            var session = _host.Session;
            if (session == null || settings == null || settings.MinutesPerMeter <= 0 || walkLane == null)
            {
                return;
            }

            var delta = _walkCamera - previousCamera;
            if (delta <= 0)
            {
                return;
            }

            var meters = (double)delta / settings.LaneLength * settings.LaneMeters;
            _walkMinutesCarry += meters * settings.MinutesPerMeter;
            var whole = (int)Math.Floor(_walkMinutesCarry);
            if (whole <= 0)
            {
                return;
            }

            _walkMinutesCarry -= whole;
            var damage = _host.Sessions.AdvanceWalk(session, whole);
            _clock.Show(session.State.World.Clock);
            UpdateWalkNeeds();
            if (damage != null)
            {
                GameFlow.ShowThirst(_host, damage, ShowToast);
            }
        }

        private void UpdateWalkNeeds()
        {
            if (walkLane == null)
            {
                return;
            }

            var player = _host.Session?.State.Player;
            if (player == null)
            {
                return;
            }

            var needs = player.Needs;
            walkLane.SetNeeds(string.Format(
                UiText.Get(_host.Texts, UiTextKey.Game_Walk_Needs),
                needs.Hunger,
                needs.FoodMax,
                needs.Thirst,
                needs.WaterMax));
        }

        private void UpdateWalkSky()
        {
            var session = _host.Session;
            if (walkLane == null || session == null)
            {
                return;
            }

            var rules = _host.Rules.Rules;
            if (rules == null)
            {
                return;
            }

            walkLane.SetSky(SkyCurve.Evaluate(session.State.World.Clock, rules.Sky), _walkIndoor);
        }

        private void AddWalkHold(Button? button)
        {
            if (button == null)
            {
                return;
            }

            var trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            AddWalkTrigger(trigger, EventTriggerType.PointerDown, () => _walkHeld = true);
            AddWalkTrigger(trigger, EventTriggerType.PointerUp, () => _walkHeld = false);
            AddWalkTrigger(trigger, EventTriggerType.PointerExit, () => _walkHeld = false);
        }

        private void AddWalkRunHold(Button? button)
        {
            if (button == null)
            {
                return;
            }

            var trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            AddWalkTrigger(trigger, EventTriggerType.PointerDown, () => _walkRunHeld = true);
            AddWalkTrigger(trigger, EventTriggerType.PointerUp, () => _walkRunHeld = false);
            AddWalkTrigger(trigger, EventTriggerType.PointerExit, () => _walkRunHeld = false);
        }

        private static void AddWalkTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private static void SetButtonLabel(Button? button, string text)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
                label.color = new Color(0.2f, 0.13f, 0.07f, 1f);
            }
        }

        /// <summary>Ответвление от дороги: ребро, позиция и название зоны.</summary>
        private sealed class WalkBranch
        {
            public WalkBranch(WorldEdge edge, float worldX, string title)
            {
                Edge = edge;
                WorldX = worldX;
                Title = title;
            }

            public WorldEdge Edge { get; }

            public float WorldX { get; }

            public string Title { get; }
        }

        /// <summary>Фигура на дороге: объект локации и его действие.</summary>
        private sealed class WalkFigure
        {
            public WalkFigure(WalkFigureView view) => View = view;

            public WalkFigureView View { get; }

            public LocationObject? LocationObject { get; set; }

            public string? ActorId { get; set; }

            public string? DialogId { get; set; }

            public string Title { get; set; } = string.Empty;

            public string Action { get; set; } = string.Empty;

            public float WorldX { get; set; }

            /// <summary>Номер места объекта на локации: по нему считается позиция на дороге.</summary>
            public int Slot { get; set; }

            public bool Used { get; set; }
        }
    }
}
