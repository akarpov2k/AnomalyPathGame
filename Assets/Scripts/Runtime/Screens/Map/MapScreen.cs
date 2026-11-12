#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Map
{
    /// <summary>
    /// Карта мира: картинка зоны во весь экран, на ней кликабельные вырезки хабов.
    /// Клик по объекту выбирает хаб и открывает плашку с двумя способами перехода —
    /// пешком и через проводника; крестик справа сверху возвращает в игру.
    /// Вырезки строятся по точкам <c>worldMap</c> из данных локаций, подписей у них нет.
    /// </summary>
    public sealed class MapScreen : PrefabScreenBase<IMapScreenHost>
    {
        [Tooltip("Слой кликабельных вырезок поверх картинки")]
        [SerializeField]
        private RectTransform? spots = null;

        [Tooltip("Неактивный образец вырезки хаба")]
        [SerializeField]
        private Button? spotTemplate = null;

        [Tooltip("Крестик справа сверху: возврат в игру")]
        [SerializeField]
        private Button? close = null;

        [Tooltip("Плашка выбора способа перехода; скрыта, пока хаб не выбран")]
        [SerializeField]
        private GameObject? travelPanel = null;

        [Tooltip("Зазор между выбранной вырезкой и парой кнопок")]
        [SerializeField]
        private float spotPanelGap = 16f;

        [Tooltip("Отступ пары кнопок от края картинки")]
        [SerializeField]
        private float spotPanelMargin = 24f;

        [Tooltip("Кнопка пешего перехода")]
        [SerializeField]
        private Button? walk = null;

        [Tooltip("Цена и риск пешего перехода на кнопке")]
        [SerializeField]
        private Text? walkPrice = null;

        [Tooltip("Кнопка перехода через проводника")]
        [SerializeField]
        private Button? guide = null;

        [Tooltip("Цена перехода через проводника на кнопке")]
        [SerializeField]
        private Text? guidePrice = null;

        [Tooltip("Сообщение экрана: почему переход недоступен или куда привели")]
        [SerializeField]
        private Text? toast = null;

        private readonly List<GameObject> _spotObjects = new List<GameObject>();
        private readonly Dictionary<string, MapSpot> _spotZones = new Dictionary<string, MapSpot>();
        private string? _selected;

        /// <summary>Связывает кнопки префаба с действиями.</summary>
        protected override void OnBound()
        {
            if (close != null)
            {
                close.onClick.AddListener(BackToGame);
            }

            if (walk != null)
            {
                walk.onClick.AddListener(() => Travel(HubTravelMode.OnFoot));
            }

            if (guide != null)
            {
                guide.onClick.AddListener(() => Travel(HubTravelMode.Guide));
            }

            ShowTravelPanel(false);
            SetToast(string.Empty);
        }

        /// <summary>Показывает карту и строит вырезки по текущему миру.</summary>
        public void Show()
        {
            _selected = null;
            BuildSpots();
            RefreshPanel();
            SetToast(string.Empty);
            Host.Router.Show(Panel);

            var session = Host.Session;
            if (session == null)
            {
                SetToast(UiText.Get(Host.Texts, UiTextKey.Common_No_Game), true);
                return;
            }

            if (session.State.World.CurrentLocation.Subtype != LocationSubtype.Hub)
            {
                SetToast(UiText.Get(Host.Texts, UiTextKey.Map_NotInHub), true);
            }
        }

        /// <summary>Esc возвращает в игру.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            BackToGame();
            return true;
        }

        private void BackToGame() => Host.ShowGame(announce: false);

        /// <summary>
        /// Строит вырезки хабов: один объект на шаблон локации, у которого в данных задана
        /// точка карты. Хаб без точки на карте остаётся только на экране локации.
        /// </summary>
        private void BuildSpots()
        {
            ClearSpots();
            var session = Host.Session;
            if (session == null || spots == null || spotTemplate == null)
            {
                return;
            }

            var used = new HashSet<string>();
            foreach (var hub in session.State.World.Map.Locations)
            {
                if (hub.Subtype != LocationSubtype.Hub)
                {
                    continue;
                }

                var spot = Host.Locations.Get(hub.TemplateId).WorldMapSpot;
                if (spot == null || !used.Add(hub.TemplateId))
                {
                    continue;
                }

                var instanceId = hub.InstanceId;
                var button = Instantiate(spotTemplate, spots);
                var rect = (RectTransform)button.transform;
                rect.anchorMin = new Vector2((float)spot.Left, (float)spot.Bottom);
                rect.anchorMax = new Vector2((float)spot.Right, (float)spot.Top);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                button.name = "Spot_" + hub.TemplateId;
                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => Select(instanceId));

                _spotObjects.Add(button.gameObject);
                var zone = button.GetComponent<MapSpot>();
                if (zone != null)
                {
                    _spotZones[instanceId] = zone;
                }
            }
        }

        private void ClearSpots()
        {
            for (var index = _spotObjects.Count - 1; index >= 0; index--)
            {
                var spot = _spotObjects[index];
                if (spot == null)
                {
                    continue;
                }

                spot.SetActive(false);
                Destroy(spot);
            }

            _spotObjects.Clear();
            _spotZones.Clear();
        }

        /// <summary>Выбирает хаб на карте; текущий хаб перехода не открывает.</summary>
        /// <param name="instanceId">Экземпляр хабa в мире.</param>
        private void Select(string instanceId)
        {
            var session = Host.Session;
            if (session == null)
            {
                SetToast(UiText.Get(Host.Texts, UiTextKey.Common_No_Game), true);
                return;
            }

            if (instanceId == session.State.World.CurrentLocationId)
            {
                _selected = null;
                RefreshPanel();
                RefreshSpots();
                SetToast(UiText.Get(Host.Texts, UiTextKey.Map_Same), true);
                return;
            }

            if (session.State.World.CurrentLocation.Subtype != LocationSubtype.Hub)
            {
                _selected = null;
                RefreshPanel();
                RefreshSpots();
                SetToast(UiText.Get(Host.Texts, UiTextKey.Map_NotInHub), true);
                return;
            }

            _selected = instanceId;
            SetToast(string.Empty);
            RefreshPanel();
            RefreshSpots();
        }

        private void RefreshSpots()
        {
            foreach (var pair in _spotZones)
            {
                pair.Value.SetSelected(pair.Key == _selected);
            }
        }

        /// <summary>Показывает плашку перехода и заполняет обе цены выбранного хаба.</summary>
        private void RefreshPanel()
        {
            var session = Host.Session;
            var target = session == null || _selected == null ? null : FindHub(session, _selected);
            if (session == null || target == null ||
                session.State.World.CurrentLocation.Subtype != LocationSubtype.Hub)
            {
                ShowTravelPanel(false);
                return;
            }

            var onFoot = Host.Sessions.GetHubTravelQuote(session, target.InstanceId, HubTravelMode.OnFoot);
            var guideQuote = Host.Sessions.GetHubTravelQuote(session, target.InstanceId, HubTravelMode.Guide);
            var inDevelopment = onFoot.Status == HubTravelStatus.InDevelopment ||
                                guideQuote.Status == HubTravelStatus.InDevelopment;

            if (walkPrice != null)
            {
                walkPrice.text = UiText.Format(
                    Host.Texts,
                    UiTextKey.Map_Walk_Price,
                    onFoot.Cost.Minutes,
                    onFoot.Cost.Stamina,
                    (int)Math.Round(onFoot.EncounterChance * 100));
                walkPrice.color = onFoot.Available || inDevelopment ? UiFactory.TextColor : PlayPalette.Error;
            }

            if (guidePrice != null)
            {
                guidePrice.text = UiText.Format(
                    Host.Texts,
                    UiTextKey.Map_Guide_Price,
                    guideQuote.Cost.Minutes,
                    guideQuote.Cost.Money);
                guidePrice.color = guideQuote.Available || inDevelopment ? UiFactory.TextColor : PlayPalette.Error;
            }

            SetInteractable(walk, onFoot.Available || inDevelopment);
            SetInteractable(guide, guideQuote.Available || inDevelopment);
            ShowTravelPanel(true);
            PlacePanel(Host.Locations.Get(target.TemplateId).WorldMapSpot);
        }

        /// <summary>
        /// Ставит пару кнопок рядом с выбранной вырезкой: справа от неё, по центру по высоте,
        /// а если справа не хватает места — слева. Кнопки остаются внутри картинки.
        /// </summary>
        /// <param name="spot">Точка выбранного хаба на карте; <c>null</c> — оставить как есть.</param>
        private void PlacePanel(WorldMapSpot? spot)
        {
            if (travelPanel == null || spot == null || spots == null)
            {
                return;
            }

            var panel = (RectTransform)travelPanel.transform;
            var stage = spots.parent as RectTransform;
            if (stage == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            var area = stage.rect.size;
            var size = panel.rect.size;
            var middle = (float)((spot.Bottom + spot.Top) * 0.5);

            var left = (float)spot.Right * area.x + spotPanelGap;
            if (left + size.x > area.x - spotPanelMargin)
            {
                left = (float)spot.Left * area.x - spotPanelGap - size.x;
            }

            var top = Mathf.Clamp(
                (middle * area.y) - (size.y * 0.5f),
                spotPanelMargin,
                Mathf.Max(spotPanelMargin, area.y - size.y - spotPanelMargin));

            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.zero;
            panel.pivot = Vector2.zero;
            panel.anchoredPosition = new Vector2(left, top);
        }

        private static void SetInteractable(Button? button, bool enabled)
        {
            if (button != null)
            {
                button.interactable = enabled;
            }
        }

        private void ShowTravelPanel(bool visible)
        {
            if (travelPanel != null)
            {
                travelPanel.SetActive(visible);
            }
        }

        /// <summary>Выполняет переход выбранным способом и переключает экран по результату.</summary>
        /// <param name="mode">Способ перехода.</param>
        private void Travel(HubTravelMode mode)
        {
            var session = Host.Session;
            if (session == null)
            {
                SetToast(UiText.Get(Host.Texts, UiTextKey.Common_No_Game), true);
                return;
            }

            if (_selected == null)
            {
                return;
            }

            var hubId = _selected;
            var target = FindHub(session, hubId);
            HubTravelResult result;
            try
            {
                result = Host.Sessions.TravelToHub(session, hubId, mode);
            }
            catch (Exception error)
            {
                SetToast(UiText.Format(Host.Texts, UiTextKey.Game_Travel_Error, error.Message), true);
                return;
            }

            if (result.Status == HubTravelStatus.InDevelopment)
            {
                Host.ShowDevelopmentStub();
                return;
            }

            if (result.Died)
            {
                SetToast(UiText.Get(Host.Texts, UiTextKey.Game_Thirst_Death), true);
                Host.ShowGame(announce: false);
                return;
            }

            if (result.Status == HubTravelStatus.Encounter)
            {
                var enemyName = result.EncounterActorId != null && Host.Actors.Contains(result.EncounterActorId)
                    ? Host.Texts.Get(Host.Actors.Get(result.EncounterActorId).NameKey)
                    : UiText.Get(Host.Texts, UiTextKey.Common_Dash);
                SetToast(UiText.Format(Host.Texts, UiTextKey.Map_Encounter, enemyName), true);
                Host.ShowCombat();
                return;
            }

            if (result.Status == HubTravelStatus.Success)
            {
                if (result.Damage is { } hit && !hit.Died)
                {
                    SetToast(UiText.Format(Host.Texts, UiTextKey.Game_Thirst_Hit, hit.Damage), true);
                }
                else
                {
                    var arrived = target == null ? string.Empty : HubTitle(target);
                    SetToast(UiText.Format(Host.Texts, UiTextKey.Map_Arrived, arrived));
                }

                Host.ShowGame(announce: false);
                return;
            }

            SetToast(Reason(result.Status), true);
            RefreshPanel();
        }

        private string Reason(HubTravelStatus status) => status switch
        {
            HubTravelStatus.NotFromHub => UiText.Get(Host.Texts, UiTextKey.Map_NotInHub),
            HubTravelStatus.SameHub => UiText.Get(Host.Texts, UiTextKey.Map_Same),
            HubTravelStatus.NotEnoughStamina => UiText.Get(Host.Texts, UiTextKey.Map_NotEnoughStamina),
            HubTravelStatus.NotEnoughMoney => UiText.Get(Host.Texts, UiTextKey.Map_NotEnoughMoney),
            HubTravelStatus.TargetNotFound => UiText.Get(Host.Texts, UiTextKey.Map_NoTargets),
            HubTravelStatus.TargetNotHub => UiText.Get(Host.Texts, UiTextKey.Map_NoTargets),
            _ => UiText.Get(Host.Texts, UiTextKey.Map_Unavailable)
        };

        private static LocationInstance? FindHub(GameSession session, string hubId)
        {
            foreach (var location in session.State.World.Map.Locations)
            {
                if (location.InstanceId == hubId && location.Subtype == LocationSubtype.Hub)
                {
                    return location;
                }
            }

            return null;
        }

        private string HubTitle(LocationInstance hub)
        {
            var template = Host.Locations.Get(hub.TemplateId);
            return Host.Texts.Get(template.TitleKey);
        }

        /// <summary>Показывает сообщение экрана карты: так заглушка говорит о неготовом контенте.</summary>
        /// <param name="text">Текст сообщения.</param>
        public void ShowToast(string text) => SetToast(text);

        private void SetToast(string text, bool error = false)
        {
            if (toast == null)
            {
                return;
            }

            toast.text = text;
            toast.color = error ? PlayPalette.Error : UiFactory.ToastColor;
            toast.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }
}
