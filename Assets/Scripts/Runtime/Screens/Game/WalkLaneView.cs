#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.World;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Слой режима похода в префабе: фон-копии, мир, игрок, фигуры, полоска пути,
    /// еда и вода, кнопки «Идти», «В хаб» и взаимодействия. Код только заполняет данные.
    /// </summary>
    public sealed class WalkLaneView : MonoBehaviour
    {
        [Tooltip("Слой целиком для затемнения перехода")]
        [SerializeField]
        private CanvasGroup? group = null;

        [Tooltip("Мир: дорога и фигуры, едет влево")]
        [SerializeField]
        private RectTransform? world = null;

        [Tooltip("Запасной фон, когда картинки нет")]
        [SerializeField]
        private Image? fallback = null;

        [Tooltip("Копии фона: чётные зеркалятся")]
        [SerializeField]
        private Image[] backdrops = new Image[WalkMetrics.BackdropCount];

        [Tooltip("Три переставляемые части фона похода")]
        [SerializeField]
        private Image[] backdropParts = new Image[WalkMetrics.BackdropPartCount];

        [Tooltip("Полоса земли, когда нет дороги")]
        [SerializeField]
        private RectTransform? ground = null;

        [Tooltip("Нарисованная дорога внизу")]
        [SerializeField]
        private Image? road = null;

        [Tooltip("Фигура игрока")]
        [SerializeField]
        private RectTransform? player = null;

        [Tooltip("Покадровая анимация игрока")]
        [SerializeField]
        private WalkPlayerView? playerView = null;

        [Tooltip("Небо: солнце, луна и оттенок сцены")]
        [SerializeField]
        private WalkSkyView? skyView = null;

        [Tooltip("Кнопка «Идти»")]
        [SerializeField]
        private Button? forward = null;

        [Tooltip("Кнопка бега: ускорение и двойной расход")]
        [SerializeField]
        private Button? run = null;

        [Tooltip("Кнопка «В хаб»")]
        [SerializeField]
        private Button? returnToHub = null;

        [Tooltip("Кнопка взаимодействия")]
        [SerializeField]
        private Button? interact = null;

        [Tooltip("Кнопка «Вверх» у ответвления")]
        [SerializeField]
        private Button? up = null;

        [Tooltip("Подпись кнопки «Вверх»")]
        [SerializeField]
        private Text? upLabel = null;

        [Tooltip("Указатели ответвлений")]
        [SerializeField]
        private WalkBranchView[] branches = new WalkBranchView[WalkMetrics.BranchCount];

        [Tooltip("Подпись кнопки взаимодействия")]
        [SerializeField]
        private Text? interactLabel = null;

        [Tooltip("Заполнение полоски пути")]
        [SerializeField]
        private Image? progressFill = null;

        [Tooltip("Подпись полоски пути")]
        [SerializeField]
        private Text? progressLabel = null;

        [Tooltip("Строка еды и воды")]
        [SerializeField]
        private Text? needsLabel = null;

        [Tooltip("Слоты фигур на дороге")]
        [SerializeField]
        private WalkFigureView[] figures = new WalkFigureView[WalkMetrics.FigureCount];

        /// <summary>Анимация игрока; пусто — фигура без кадров.</summary>
        public WalkPlayerView? PlayerView => playerView;

        /// <summary>Небо похода: светила и оттенок сцены.</summary>
        public WalkSkyView? SkyView => skyView;

        /// <summary>Кнопка «Идти».</summary>
        public Button? Forward => forward;

        /// <summary>Кнопка «Бег»: движение с ускорением и двойным расходом.</summary>
        public Button? Run => run;

        /// <summary>Кнопка «В хаб».</summary>
        public Button? ReturnToHub => returnToHub;

        /// <summary>Кнопка взаимодействия.</summary>
        public Button? Interact => interact;

        /// <summary>Кнопка «Вверх».</summary>
        public Button? Up => up;

        /// <summary>Слой затемнения перехода.</summary>
        public CanvasGroup? Group => group;

        /// <summary>Дорога внизу.</summary>
        public Image? Road => road;

        /// <summary>Основной слой фона; пусто — фон не задан.</summary>
        public Image? Backdrop => backdrops.Length > 0 ? backdrops[0] : null;

        /// <summary>Переставляемые части фона; у локации без частей не показываются.</summary>
        public IReadOnlyList<Image> BackdropParts => backdropParts;

        /// <summary>Слоты фигур.</summary>
        public WalkFigureView[] Figures => figures;

        /// <summary>Показывает или скрывает слой.</summary>
        /// <param name="visible">Показывать ли слой.</param>
        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        /// <summary>Настраивает длину дороги, позицию игрока и высоту земли.</summary>
        /// <param name="laneLength">Длина дороги в единицах канваса.</param>
        /// <param name="playerX">Позиция игрока по X.</param>
        /// <param name="groundY">Высота линии земли.</param>
        /// <param name="indoor">Локация внутри помещения: полосы неба нет, кадр выше на её долю.</param>
        public void Configure(float laneLength, float playerX, float groundY, bool indoor = false)
        {
            if (world != null)
            {
                world.sizeDelta = new Vector2(laneLength + WalkMetrics.WorldTail, 0f);
                world.anchoredPosition = Vector2.zero;
            }

            var top = indoor ? 1f : 1f - WalkMetrics.SkyBandHeight;
            SetBackdropTop(top);
            var laneHeight = ((RectTransform)transform).rect.height;
            var bottom = indoor && laneHeight > 0f
                ? Mathf.Clamp(WalkMetrics.FloorStripHeight / laneHeight, 0f, 0.5f)
                : 0f;
            var partWidth = (laneLength + WalkMetrics.WorldTail) / WalkMetrics.BackdropPartCount;
            for (var index = 0; index < backdropParts.Length; index++)
            {
                var part = backdropParts[index];
                if (part == null)
                {
                    continue;
                }

                // Сегмент занимает всю высоту слоя под полосой неба: при другой высоте
                // экрана он растягивается вместе с ней, поэтому щели на стыке нет.
                // В помещении неба нет: верх — кромка экрана, низ — кромка полосы пола.
                var rect = part.rectTransform;
                rect.anchorMin = new Vector2(0f, bottom);
                rect.anchorMax = new Vector2(0f, top);
                rect.offsetMin = new Vector2(index * partWidth, 0f);
                rect.offsetMax = new Vector2((index + 1) * partWidth, 0f);
            }

            if (player != null)
            {
                player.anchoredPosition = new Vector2(playerX, groundY);
            }

            if (ground != null)
            {
                ground.sizeDelta = new Vector2(0f, groundY + WalkMetrics.GroundExtra);
            }
        }

        /// <summary>
        /// Показывает один кадр на весь экран: в помещении неба нет, поэтому кадр
        /// занимает всю высоту, а отдельные полосы дороги и земли убираются.
        /// </summary>
        /// <param name="sprite">Кадр помещения; <c>null</c> — запасной цвет.</param>
        public void SetFullBackdrop(Sprite? sprite)
        {
            SetBackdrop(sprite);
            SetBackdropTop(1f);
            if (road != null)
            {
                road.gameObject.SetActive(false);
            }

            if (ground != null)
            {
                ground.gameObject.SetActive(false);
            }
        }

        private void SetBackdropTop(float top)
        {
            foreach (var copy in backdrops)
            {
                if (copy == null)
                {
                    continue;
                }

                var rect = copy.rectTransform;
                rect.anchorMin = new Vector2(rect.anchorMin.x, 0f);
                rect.anchorMax = new Vector2(rect.anchorMax.x, top);
            }
        }

        /// <summary>Ставит нарисованную дорогу внизу; <c>null</c> — возвращает линию земли.</summary>
        /// <param name="sprite">Картинка дороги.</param>
        public void SetRoad(Sprite? sprite)
        {
            if (road != null)
            {
                road.sprite = sprite;
                road.color = Color.white;
                road.gameObject.SetActive(sprite != null);
            }

            if (ground != null)
            {
                ground.gameObject.SetActive(sprite == null);
            }
        }

        /// <summary>Ставит картинку фона во все копии.</summary>
        /// <param name="sprite">Картинка; <c>null</c> — запасной цвет.</param>
        public void SetBackdrop(Sprite? sprite)
        {
            if (fallback != null)
            {
                fallback.gameObject.SetActive(sprite == null);
            }

            foreach (var part in backdropParts)
            {
                if (part != null)
                {
                    part.gameObject.SetActive(false);
                }
            }

            foreach (var copy in backdrops)
            {
                if (copy == null)
                {
                    continue;
                }

                copy.gameObject.SetActive(sprite != null);
                copy.sprite = sprite;
                copy.color = Color.white;
                copy.preserveAspect = false;
            }
        }

        /// <summary>
        /// Показывает три переставляемые части фона в заданном порядке. Когда части заданы,
        /// нижняя полоса дороги и полоса земли скрываются: земля и дорога нарисованы в кадре.
        /// </summary>
        /// <param name="parts">Три спрайта по порядку слева направо; <c>null</c> или неполный набор — части скрываются.</param>
        public void SetBackdropParts(IReadOnlyList<Sprite?>? parts)
        {
            var complete = parts != null && parts.Count == backdropParts.Length;
            if (complete)
            {
                for (var index = 0; index < parts!.Count; index++)
                {
                    if (parts[index] == null)
                    {
                        complete = false;
                        break;
                    }
                }
            }

            for (var index = 0; index < backdropParts.Length; index++)
            {
                var part = backdropParts[index];
                if (part == null)
                {
                    continue;
                }

                part.gameObject.SetActive(complete);
                part.sprite = complete ? parts![index] : null;
                part.color = Color.white;
                part.preserveAspect = false;
            }

            if (!complete)
            {
                return;
            }

            if (fallback != null)
            {
                fallback.gameObject.SetActive(false);
            }

            foreach (var copy in backdrops)
            {
                if (copy != null)
                {
                    copy.gameObject.SetActive(false);
                }
            }

            if (road != null)
            {
                road.gameObject.SetActive(false);
            }

            if (ground != null)
            {
                ground.gameObject.SetActive(false);
            }
        }

        /// <summary>Сдвигает мир под камеру и медленно ведёт фон.</summary>
        /// <param name="cameraX">Пройденное расстояние.</param>
        public void SetWorldOffset(float cameraX)
        {
            if (world != null)
            {
                world.anchoredPosition = new Vector2(-cameraX, 0f);
            }

            if (backdrops.Length > 0 && backdrops[0] != null)
            {
                backdrops[0].rectTransform.anchoredPosition =
                    new Vector2(-cameraX * WalkMetrics.BackdropParallax, 0f);
            }
        }

        /// <summary>Ставит игрока на линию земли.</summary>
        /// <param name="groundY">Высота линии земли.</param>
        public void SetPlayerY(float groundY)
        {
            if (player != null)
            {
                player.anchoredPosition = new Vector2(player!.anchoredPosition.x, groundY);
            }
        }

        /// <summary>Переключает анимацию игрока: покой, ходьба, бег или шаг назад.</summary>
        /// <param name="moving">Идёт ли игрок.</param>
        /// <param name="running">Бежит ли игрок.</param>
        /// <param name="backward">Идёт ли игрок назад.</param>
        public void SetPlayerState(bool moving, bool running, bool backward)
        {
            if (playerView != null)
            {
                playerView.SetState(moving, running, backward);
            }
        }

        /// <summary>Показывает небо по часам: светило на дуге и оттенок сцены.</summary>
        /// <param name="state">Состояние неба из кривой часов.</param>
        /// <param name="indoor">Локация внутри помещения.</param>
        public void SetSky(SkyState state, bool indoor)
        {
            if (skyView != null)
            {
                skyView.Show(state, indoor);
            }
        }

        /// <summary>Заполняет полоску пути.</summary>
        /// <param name="progress">Прогресс 0..1.</param>
        /// <param name="text">Готовая подпись.</param>
        public void SetProgress(float progress, string text)
        {
            if (progressFill != null && progressFill.transform.parent is RectTransform parent)
            {
                progressFill.rectTransform.sizeDelta = new Vector2(parent.rect.width * progress, 0f);
            }

            if (progressLabel != null)
            {
                progressLabel.text = text;
            }
        }

        /// <summary>Показывает еду и воду.</summary>
        /// <param name="text">Готовая строка.</param>
        public void SetNeeds(string text)
        {
            if (needsLabel != null)
            {
                needsLabel.text = text;
            }
        }

        /// <summary>Меняет подпись кнопки взаимодействия.</summary>
        /// <param name="text">Текст кнопки.</param>
        public void SetInteractLabel(string text)
        {
            if (interactLabel != null)
            {
                interactLabel.text = text;
            }
        }

        /// <summary>Показывает или прячет кнопку «Вверх».</summary>
        /// <param name="visible">Показывать ли кнопку.</param>
        /// <param name="text">Подпись кнопки.</param>
        public void SetUp(bool visible, string text)
        {
            if (upLabel != null)
            {
                upLabel.text = text;
                upLabel.color = new Color(0.2f, 0.13f, 0.07f, 1f);
            }

            if (up != null)
            {
                up.gameObject.SetActive(visible);
            }
        }

        /// <summary>Ставит указатель ответвления на дорогу.</summary>
        /// <param name="index">Номер указателя.</param>
        /// <param name="title">Название зоны.</param>
        /// <param name="worldX">Позиция на дороге.</param>
        /// <param name="groundY">Высота земли.</param>
        /// <param name="objectSprite">Вырезка локации-цели.</param>
        /// <param name="pathSprite">Тропа в глубину.</param>
        public void ShowBranch(
            int index,
            string title,
            float worldX,
            float groundY,
            Sprite? objectSprite,
            Sprite? pathSprite)
        {
            if (index < 0 || index >= branches.Length || branches[index] == null)
            {
                return;
            }

            var branch = branches[index];
            branch.Rect.anchoredPosition = new Vector2(worldX, groundY);
            branch.Show(objectSprite, pathSprite, title);
        }

        /// <summary>Скрывает все указатели ответвлений кроме указанного количества.</summary>
        /// <param name="count">Сколько указателей оставить.</param>
        public void HideBranchesFrom(int count)
        {
            for (var index = 0; index < branches.Length; index++)
            {
                if (index >= count && branches[index] != null)
                {
                    branches[index].Hide();
                }
            }
        }
    }
}
