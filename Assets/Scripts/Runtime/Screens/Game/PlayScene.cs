#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Сцена локации из префаба: текстура дороги на весь экран, полоса неба поверх неё
    /// и три объекта-перехода. Объект-переход — вырезка локации-цели; у цели без вырезки
    /// рисуется общий указатель. Объекты кликабельны и подсвечиваются при наведении.
    /// </summary>
    public sealed class PlayScene : MonoBehaviour
    {
        [Tooltip("Подложка неба на весь экран: закрывает прозрачные места текстуры дороги")]
        [SerializeField]
        private Image? skyBackdrop = null;

        [Tooltip("Полоса неба сверху")]
        [SerializeField]
        private Image? sky = null;

        [Tooltip("Текстура дороги текущей локации")]
        [SerializeField]
        private Image? road = null;

        [Tooltip("Подгонщик пропорций дороги")]
        [SerializeField]
        private AspectRatioFitter? roadFitter = null;

        [Tooltip("Место перехода-ветки 2: слева, выше центра")]
        [SerializeField]
        private RectTransform? leftRoot = null;

        [Tooltip("Место перехода «дальше»: по центру")]
        [SerializeField]
        private RectTransform? centerRoot = null;

        [Tooltip("Место перехода-ветки 1: справа, ниже центра")]
        [SerializeField]
        private RectTransform? rightRoot = null;

        private SlotView _left = null!;
        private SlotView _center = null!;
        private SlotView _right = null!;
        private bool _hasRoad;
        private bool _slotsReady;

        /// <summary>Корень сцены: лежит позади остальных элементов экрана.</summary>
        public RectTransform Root => (RectTransform)transform;

        private void Awake() => EnsureSlots();

        /// <summary>
        /// Готовит места переходов. В игре это делает <c>Awake</c>, но кадры сцены
        /// собираются в редакторе из префаба, где <c>Awake</c> не вызывается, —
        /// поэтому инициализация повторяется по требованию.
        /// </summary>
        private void EnsureSlots()
        {
            if (_slotsReady)
            {
                return;
            }

            _slotsReady = true;
            _left = new SlotView(leftRoot);
            _center = new SlotView(centerRoot);
            _right = new SlotView(rightRoot);
        }

        /// <summary>Показывает или скрывает сцену.</summary>
        /// <param name="visible">Показывать ли дорогу, небо и объекты.</param>
        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        /// <summary>Меняет текстуру дороги текущей локации.</summary>
        /// <param name="roadId">Id дороги в каталоге спрайтов; <c>null</c> — запасной цвет.</param>
        public void SetRoad(string? roadId)
        {
            _hasRoad = ApplyClosed(road, roadFitter, roadId, PlayPalette.RoadFallback);
            HideBandForRoad();
        }

        /// <summary>
        /// Меняет небо сцены по фазе суток: полосу сверху и подложку под дорогой.
        /// Если дорога есть, полоса прозрачная: верх экрана уже занят небом самой дороги.
        /// </summary>
        /// <param name="skyId">Id неба в каталоге спрайтов; <c>null</c> — запасной цвет.</param>
        public void SetSky(string? skyId)
        {
            Apply(sky, skyId, PlayPalette.SkyFallback);
            Apply(skyBackdrop, skyId, PlayPalette.SkyFallback);
            HideBandForRoad();
        }

        /// <summary>Показывает объекты-переходы в местах по правилу раскладки.</summary>
        /// <param name="transitions">Переходы в порядке показа.</param>
        /// <param name="forwardIndex">Номер перехода «дальше по дороге»; <c>-1</c> — такого нет.</param>
        /// <exception cref="ArgumentNullException">Список не задан.</exception>
        public void SetTransitions(IReadOnlyList<PlayTransition> transitions, int forwardIndex)
        {
            if (transitions == null)
            {
                throw new ArgumentNullException(nameof(transitions));
            }

            HideTransitions();
            if (transitions.Count == 0)
            {
                return;
            }

            var layout = PlaySceneSlots.Layout(transitions.Count, forwardIndex);
            var single = transitions.Count == 1;
            for (var index = 0; index < transitions.Count && index < layout.Count; index++)
            {
                var transition = transitions[index];
                Sprite? art = null;
                var hasArt = transition.ArtId != null && _spritesOf != null &&
                             _spritesOf.TryGet(transition.ArtId!, out art) && art != null;
                var view = Slot(layout[index]);
                view.Show(transition, hasArt ? art : null);
                if (single || index == forwardIndex)
                {
                    view.PlaceAt(CenterPlacement(), Root.rect.size);
                }
            }
        }

        /// <summary>
        /// Место перехода «дальше по дороге» и единственного перехода: низ по центру в точке схода дороги.
        /// Точка берётся из картинки дороги, поэтому совпадает при любом разрешении.
        /// </summary>
        /// <returns>Прямоугольник перехода в единицах сцены.</returns>
        private Rect CenterPlacement()
        {
            var size = Root.rect.size;
            var point = ToStagePoint(0.5, PlayMetrics.RoadHorizon);
            return PlaySceneSlots.CenterRect(
                point,
                size,
                PlayMetrics.CenterTransitionWidth,
                PlayMetrics.CenterTransitionHeight);
        }

        /// <summary>Скрывает все объекты-переходы: в бою их нет.</summary>
        public void HideTransitions()
        {
            EnsureSlots();
            _left.Hide();
            _center.Hide();
            _right.Hide();
        }

        /// <summary>Каталог спрайтов передаёт экран: дорога и вырезки берутся по id.</summary>
        /// <param name="sprites">Каталог спрайтов.</param>
        public void SetSprites(ISpriteCatalog sprites) => _spritesOf = sprites;

        /// <summary>Можно ли ставить фигуры на сцену: нужна картинка дороги, чтобы знать её пропорции.</summary>
        public bool CanStage => road != null && road.sprite != null;

        /// <summary>Высота картинки дороги в пикселях: на неё делится высота фигуры из сцены.</summary>
        public float RoadArtHeight => road != null && road.sprite != null ? road.sprite.rect.height : 0f;

        /// <summary>
        /// Высота фигуры в единицах канваса: доля от высоты картинки дороги, умноженная
        /// на высоту вписанной дороги. Поэтому размер не зависит от разрешения экрана.
        /// </summary>
        /// <param name="anchor">Точка земли сцены: масштаб в пикселях на метр.</param>
        /// <param name="meters">Высота фигуры в метрах из записи спавна.</param>
        /// <returns>Высота в единицах канваса; <c>0</c>, если дороги нет.</returns>
        public float StageHeight(AnomalyPath.Core.Stage.StageAnchor anchor, double meters)
        {
            if (!CanStage)
            {
                return 0f;
            }

            var fraction = anchor.HeightFraction(meters, RoadArtHeight);
            return (float)(fraction * road!.rectTransform.rect.height);
        }

        /// <summary>
        /// Переводит долю картинки дороги в точку канваса сцены: 0 по ширине — левый край,
        /// 0 по высоте — низ, 1 — правый и верхний. Учитывает то, как вписана дорога.
        /// </summary>
        /// <param name="x">Доля по ширине картинки, 0..1.</param>
        /// <param name="y">Доля по высоте картинки, 0..1 — низ фигуры.</param>
        /// <returns>Точка в единицах сцены, начало координат — её левый нижний угол.</returns>
        public Vector2 ToStagePoint(double x, double y)
        {
            if (!CanStage)
            {
                var area = Root.rect;
                return new Vector2((float)x * area.width, (float)y * area.height);
            }

            var roadRect = road!.rectTransform;
            var roadArea = roadRect.rect;
            var local = new Vector2((float)x * roadArea.width, (float)y * roadArea.height);
            var world = roadRect.TransformPoint(roadArea.min + local);
            var inScene = Root.InverseTransformPoint(world);
            return new Vector2(inScene.x, inScene.y) - Root.rect.min;
        }

        private ISpriteCatalog? _spritesOf;

        private SlotView Slot(PlaySceneSlot slot) => slot switch
        {
            PlaySceneSlot.RightLow => _right,
            PlaySceneSlot.LeftHigh => _left,
            _ => _center
        };

        private void Apply(Image? image, string? id, Color fallback)
        {
            if (image == null)
            {
                return;
            }

            if (id != null && _spritesOf != null && _spritesOf.TryGet(id, out var sprite) && sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                return;
            }

            image.sprite = null;
            image.color = fallback;
        }

        private bool ApplyClosed(Image? image, AspectRatioFitter? fitter, string? id, Color fallback)
        {
            if (image == null)
            {
                return false;
            }

            if (id != null && _spritesOf != null && _spritesOf.TryGet(id, out var sprite) && sprite != null)
            {
                image.gameObject.SetActive(true);
                image.sprite = sprite;
                image.color = Color.white;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                if (fitter != null)
                {
                    fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
                    fitter.enabled = true;
                }

                return true;
            }

            image.sprite = null;
            image.color = fallback;
            if (fitter != null)
            {
                fitter.enabled = false;
            }

            image.gameObject.SetActive(false);
            return false;
        }

        private void HideBandForRoad()
        {
            if (sky != null)
            {
                sky.color = _hasRoad ? new Color(1f, 1f, 1f, 0f) : Color.white;
            }
        }

        /// <summary>Одно место перехода: свечение, картинка с кнопкой и подсказка.</summary>
        private sealed class SlotView
        {
            private readonly GameObject _root;
            private readonly RectTransform _rect;
            private readonly Vector2 _defaultAnchorMin;
            private readonly Vector2 _defaultAnchorMax;
            private readonly Vector2 _defaultOffsetMin;
            private readonly Vector2 _defaultOffsetMax;
            private readonly Image? _art;
            private readonly Image? _glow;
            private readonly HoverHighlight? _highlight;
            private PlayTransition? _transition;

            public SlotView(RectTransform? root)
            {
                if (root == null)
                {
                    throw new ArgumentNullException(nameof(root));
                }

                _root = root.gameObject;
                _rect = root;
                _defaultAnchorMin = root.anchorMin;
                _defaultAnchorMax = root.anchorMax;
                _defaultOffsetMin = root.offsetMin;
                _defaultOffsetMax = root.offsetMax;
                _art = Find<Image>(root, "TransitionArt");
                _glow = Find<Image>(root, "TransitionGlow");
                var shadow = Find<Image>(root, "TransitionShadow");
                if (shadow != null)
                {
                    shadow.sprite = PlayArt.Glow();
                    shadow.color = new Color(0f, 0f, 0f, 0.45f);
                    shadow.preserveAspect = false;
                    shadow.raycastTarget = false;
                }

                if (_glow != null)
                {
                    _glow.sprite = PlayArt.Glow();
                    _glow.color = PlayPalette.TransitionGlow;
                    _glow.preserveAspect = true;
                    _glow.raycastTarget = false;
                }

                if (_art != null)
                {
                    _art.raycastTarget = true;
                    var button = _art.GetComponent<Button>();
                    if (button == null)
                    {
                        button = _art.gameObject.AddComponent<Button>();
                    }

                    button.targetGraphic = _art;
                    button.transition = Selectable.Transition.None;
                    button.onClick.AddListener(Click);

                    _highlight = _art.GetComponent<HoverHighlight>();
                    if (_highlight == null)
                    {
                        _highlight = _art.gameObject.AddComponent<HoverHighlight>();
                    }

                    if (_glow != null)
                    {
                        _highlight.Configure(
                            _art,
                            _glow.gameObject,
                            PlayPalette.TransitionNormal,
                            PlayPalette.TransitionHover,
                            PlayMetrics.TransitionHoverScale);
                    }
                }
            }

            /// <summary>Ставит место перехода прямоугольником сцены: низ по центру у горизонта дороги.</summary>
            /// <param name="rect">Прямоугольник в единицах сцены от левого нижнего угла.</param>
            /// <param name="sceneSize">Размер сцены в единицах канваса.</param>
            public void PlaceAt(Rect rect, Vector2 sceneSize)
            {
                if (sceneSize.x <= 0f || sceneSize.y <= 0f)
                {
                    return;
                }

                _rect.anchorMin = new Vector2(rect.x / sceneSize.x, rect.y / sceneSize.y);
                _rect.anchorMax = new Vector2(
                    (rect.x + rect.width) / sceneSize.x,
                    (rect.y + rect.height) / sceneSize.y);
                _rect.offsetMin = Vector2.zero;
                _rect.offsetMax = Vector2.zero;
            }

            /// <summary>Возвращает месту прежние якоря сцены: так стоят ветки и продолжение пути.</summary>
            private void RestoreRect()
            {
                _rect.anchorMin = _defaultAnchorMin;
                _rect.anchorMax = _defaultAnchorMax;
                _rect.offsetMin = _defaultOffsetMin;
                _rect.offsetMax = _defaultOffsetMax;
            }

            public void Show(PlayTransition transition, Sprite? art)
            {
                _transition = transition;
                if (_art == null)
                {
                    return;
                }

                RestoreRect();
                var hasArt = art != null;
                _art.sprite = hasArt ? art : PlayArt.Signpost();
                _highlight?.SetNormalColor(hasArt ? PlayPalette.TransitionNormal : PlayPalette.Marker);
                _highlight?.SetHighlighted(false);
                PlaceArt(_art.rectTransform, hasArt);
                _root.SetActive(true);
            }

            public void Hide()
            {
                _transition = null;
                _highlight?.SetHighlighted(false);
                _root.SetActive(false);
            }

            private void Click() => _transition?.Click();

            private static void PlaceArt(RectTransform rect, bool fullSize)
            {
                if (fullSize)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                }
                else
                {
                    rect.anchorMin = new Vector2(
                        PlayMetrics.TransitionMarkerLeft,
                        PlayMetrics.TransitionMarkerBottom);
                    rect.anchorMax = new Vector2(
                        PlayMetrics.TransitionMarkerRight,
                        PlayMetrics.TransitionMarkerTop);
                }

                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            private static T? Find<T>(Transform root, string name)
                where T : Component
            {
                var child = root.Find(name);
                return child == null ? null : child.GetComponent<T>();
            }
        }
    }
}
