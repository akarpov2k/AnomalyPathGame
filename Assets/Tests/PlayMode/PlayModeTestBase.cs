#nullable enable
using System.Collections;
using AnomalyPath.Runtime;
using AnomalyPath.Runtime.Screens.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Общий каркас PlayMode-тестов интерфейса: загрузка сцены Main, старт новой игры,
    /// нажатия по кнопкам и поиск объектов на сцене. Звук на прогоне глушит
    /// <see cref="TestAudioMute"/> — один раз на весь набор, а не на каждый класс.
    /// </summary>
    public abstract class PlayModeTestBase
    {
        /// <summary>Имя игровой сцены, в которой живут все экраны.</summary>
        protected const string MainSceneName = "Main";

        /// <summary>Загружает сцену Main и даёт кадр на создание объектов.</summary>
        /// <returns>Корутина загрузки.</returns>
        protected static IEnumerator LoadMain()
        {
            PlayScreen.LegacyModeForTests = true;
            yield return SceneManager.LoadSceneAsync(MainSceneName);
            yield return null;
            DisableWalkForLegacyTests();
        }

        /// <summary>Старые тесты проверяют обычный экран: режим похода выключается без перерисовки.</summary>
        private static void DisableWalkForLegacyTests()
        {
            foreach (var screen in Object.FindObjectsByType<PlayScreen>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                screen.SetWalkModeFlag(false);
            }
        }

        /// <summary>Загружает Main, открывает экран старта и начинает новую игру.</summary>
        /// <returns>Корутина старта.</returns>
        protected static IEnumerator StartGame()
        {
            yield return LoadMain();
            Click("Новая игра");
            yield return null;
            Click("Начать");
            for (var frame = 0; frame < 5; frame++)
            {
                yield return null;
                DisableWalkForLegacyTests();
            }

            var screen = Object.FindAnyObjectByType<GameScreen>();
            if (screen != null)
            {
                screen.ShowGame(false);
                yield return null;
                DisableWalkForLegacyTests();
            }
        }

        /// <summary>Нажимает кнопку по имени объекта.</summary>
        /// <param name="name">Имя объекта с компонентом Button.</param>
        protected static void Click(string name)
        {
            var go = GameObject.Find(name);
            Assert.IsNotNull(go, $"Кнопка «{name}» не найдена");
            var button = go!.GetComponent<Button>();
            Assert.IsNotNull(button, $"У «{name}» нет Button");
            button!.onClick.Invoke();
        }

        /// <summary>Нажимает кнопку, только если она есть и доступна.</summary>
        /// <param name="name">Имя объекта с компонентом Button.</param>
        protected static void ClickIfInteractable(string name)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                return;
            }

            var button = go.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }

        /// <summary>Правый клик по объекту в точке экрана.</summary>
        /// <param name="target">Объект-приёмник.</param>
        /// <param name="position">Позиция в координатах экрана.</param>
        protected static void RightClick(GameObject target, Vector2 position)
        {
            var data = new PointerEventData(EventSystem.current)
            {
                position = position,
                button = PointerEventData.InputButton.Right
            };
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
        }

        /// <summary>Ищет активный или скрытый объект по точному имени.</summary>
        /// <param name="name">Имя объекта.</param>
        /// <returns>Найденный объект или <c>null</c>.</returns>
        protected static GameObject? FindAnywhere(string name)
        {
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var transform in transforms)
            {
                if (string.Equals(transform.name, name, System.StringComparison.Ordinal))
                {
                    return transform.gameObject;
                }
            }

            return null;
        }

        /// <summary>Ищет первого потомка с именем, начинающимся с префикса.</summary>
        /// <param name="root">Корень поиска.</param>
        /// <param name="prefix">Префикс имени.</param>
        /// <returns>Найденный объект или <c>null</c>.</returns>
        protected static GameObject? FindByPrefix(Transform root, string prefix)
        {
            foreach (Transform child in root)
            {
                if (child.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    return child.gameObject;
                }

                var nested = FindByPrefix(child, prefix);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        /// <summary>Находит игровой экран сцены Main.</summary>
        /// <returns>Единственный в сцене <see cref="GameScreen"/>.</returns>
        protected static GameScreen FindScreen()
        {
            var screen = UnityEngine.Object.FindAnyObjectByType<GameScreen>();
            Assert.IsNotNull(screen, "GameScreen не найден в сцене Main");
            return screen!;
        }

        /// <summary>Открывает окно инвентаря значком рюкзака на игровом поле.</summary>
        protected static void OpenInventory()
        {
            var button = GameObject.Find("Backpack");
            Assert.IsNotNull(button, "На игровом поле нет значка рюкзака");
            var click = button!.GetComponent<Button>();
            Assert.IsNotNull(click, "У значка рюкзака нет Button");
            click!.onClick.Invoke();
        }

        /// <summary>
        /// Активная игровая панель: большой хаб, общий экран локации или комната торговца.
        /// </summary>
        /// <returns>Панель игры или <c>null</c>, если игра не открыта.</returns>
        protected static GameObject? GamePanel() =>
            GameObject.Find("BigHub") ?? GameObject.Find("InGame") ?? GameObject.Find("Hub");

        /// <summary>Кнопка объекта-перехода в указанном месте сцены локации.</summary>
        /// <param name="slot">Часть имени места: <c>Right</c>, <c>Left</c> или <c>Center</c>.</param>
        /// <returns>Кнопка перехода или <c>null</c>, если место пустое.</returns>
        protected static Button? FindTransition(string slot)
        {
            var go = GameObject.Find("Transition_" + slot);
            if (go == null)
            {
                return null;
            }

            var art = go.transform.Find("TransitionArt");
            return art != null ? art.GetComponent<Button>() : null;
        }

        /// <summary>Свечение объекта-перехода, которое включается при наведении.</summary>
        /// <param name="button">Кнопка перехода.</param>
        /// <returns>Объект свечения или <c>null</c>.</returns>
        protected static GameObject? FindTransitionGlow(Button button)
        {
            var glow = button.transform.parent.Find("TransitionGlow");
            return glow != null ? glow.gameObject : null;
        }

        /// <summary>Первый показанный объект-переход: центр, затем правая и левая ветки.</summary>
        /// <returns>Кнопка перехода или <c>null</c>, если переходов нет.</returns>
        protected static Button? FindAnyTransition() =>
            FindTransition("Center") ?? FindTransition("Right") ?? FindTransition("Left");

        /// <summary>Возвращает задник окна по имени панели.</summary>
        /// <param name="panelName">Имя панели окна.</param>
        /// <returns>Изображение задника.</returns>
        protected static Image BackdropOf(string panelName)
        {
            var panel = GameObject.Find(panelName);
            Assert.IsNotNull(panel, $"Панель «{panelName}» не найдена");
            var backdrop = panel.transform.Find("ScreenBackdrop");
            Assert.IsNotNull(backdrop, $"У окна «{panelName}» нет задника");
            var image = backdrop.GetComponent<Image>();
            Assert.IsNotNull(image, $"У задника «{panelName}» нет Image");
            return image;
        }

        /// <summary>Вписывает четыре угла RectTransform в мировой прямоугольник.</summary>
        /// <param name="rect">Прямоугольник интерфейса.</param>
        /// <returns>Прямоугольник по крайним точкам, без отрицательных размеров.</returns>
        protected static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = corners[0];
            var max = corners[0];
            for (var i = 1; i < corners.Length; i++)
            {
                min = Vector3.Min(min, corners[i]);
                max = Vector3.Max(max, corners[i]);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>Центр RectTransform в координатах экрана.</summary>
        /// <param name="rect">Прямоугольник интерфейса.</param>
        /// <returns>Точка в координатах экрана.</returns>
        protected static Vector2 ScreenCenter(RectTransform rect) =>
            RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    }
}
