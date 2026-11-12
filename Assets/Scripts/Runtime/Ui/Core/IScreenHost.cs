#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Settings;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEngine;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Общая часть хоста экрана: навигация, модальные окна, тексты, спрайты, сессия и настройки.
    /// Доменные сервисы сюда не входят — экран получает только свою роль (см. <c>I*ScreenHost</c>).
    /// </summary>
    public interface IScreenHost
    {
        /// <summary>Фабрика элементов интерфейса.</summary>
        UiFactory Ui { get; }

        /// <summary>Переключатель экранов.</summary>
        ScreenRouter Router { get; }

        /// <summary>Текущая игровая сессия или <c>null</c>.</summary>
        GameSession? Session { get; set; }

        /// <summary>Действия сессии: новая игра, сохранение, загрузка.</summary>
        IGameSessionService Sessions { get; }

        /// <summary>Слоты сохранений.</summary>
        ISaveService Saves { get; }

        /// <summary>Машинные настройки: разрешение, громкость, чувствительность и горячие клавиши.</summary>
        GameSettingsService Settings { get; }

        /// <summary>Тексты игры по ключу.</summary>
        IGameTexts Texts { get; }

        /// <summary>Каталог спрайтов: иконки предметов и фоны локаций по id из данных.</summary>
        ISpriteCatalog Sprites { get; }

        /// <summary>Модальный диалог отдыха: общий для экрана локации и экрана хаба.</summary>
        RestDialog RestDialog { get; }

        /// <summary>Создаёт модальное подтверждение из префаба под панелью экрана.</summary>
        /// <param name="parent">Родитель, обычно панель экрана.</param>
        /// <returns>Готовый диалог, скрытый до показа.</returns>
        ConfirmDialog CreateConfirmDialog(Transform parent);

        /// <summary>Создаёт окно диалога из префаба под указанным родителем.</summary>
        /// <param name="parent">Родитель, обычно контент экрана или канвас.</param>
        /// <param name="rootName">Имя корневого объекта окна в иерархии.</param>
        /// <param name="menu">Полосы меню в порядке показа.</param>
        /// <param name="choose">Что выполнить с выбранной фразой разговора.</param>
        /// <param name="blockClicks">Перекрывать ли клики мимо окна прозрачной плашкой.</param>
        /// <returns>Готовое скрытое окно диалога.</returns>
        DialogOverlayView CreateDialogOverlay(
            Transform parent,
            string rootName,
            IReadOnlyList<DialogOverlayView.MenuEntry> menu,
            Action<string> choose,
            bool blockClicks);

        /// <summary>Показать стартовое меню.</summary>
        void ShowMainMenu();

        /// <summary>Показать выбор набора и настроек новой игры.</summary>
        void ShowNewGame();

        /// <summary>Показать список сохранений.</summary>
        /// <param name="fromGame">Пришли из игры, а не из стартового меню.</param>
        void ShowLoad(bool fromGame);

        /// <summary>Показать экран сохранения: ячейки, название и существующие сохранения.</summary>
        void ShowSave();

        /// <summary>Показать экран настроек: разрешение, громкость и переход к клавишам.</summary>
        void ShowSettings();

        /// <summary>Показать экран настройки клавиш.</summary>
        void ShowKeyBindings();

        /// <summary>Показать основной игровой экран.</summary>
        /// <param name="announce">Показать сообщение о начале игры.</param>
        void ShowGame(bool announce = true);

        /// <summary>Показать меню игры: продолжить, сохранить, загрузить, выйти.</summary>
        void ShowGameMenu();

        /// <summary>Показать карту мира: хабы и переходы между ними.</summary>
        void ShowMap();

        /// <summary>
        /// Показать экран-заглушку «Игра в стадии разработки»: его открывает попытка перехода
        /// в локацию, контент которой ещё не готов.
        /// </summary>
        void ShowDevelopmentStub();

        /// <summary>Показать окно «инвентарь»: персонаж со снаряжением, полосой состояния и сетка инвентаря.</summary>
        void ShowInventory();

        /// <summary>Показать окно взлома тайника: код, попытки и история.</summary>
        void ShowLockMinigame();

        /// <summary>Показать окно радара поиска артефакта в аномалии.</summary>
        /// <param name="anomaly">Объект-аномалия текущей локации.</param>
        void ShowRadar(AnomalyLocationObject anomaly);

        /// <summary>Показать боевой экран; без боя ничего не происходит.</summary>
        void ShowCombat();

        /// <summary>Показать малый хаб — комнату выбранного торговца.</summary>
        /// <param name="actorId">Идентификатор торговца.</param>
        void ShowTraderRoom(string actorId);

        /// <summary>Показать окно торговли: товары торговца и инвентарь игрока.</summary>
        /// <param name="actorId">Идентификатор торговца.</param>
        /// <param name="returnToDialog">Вернуть в меню диалога торговца после закрытия окна.</param>
        void ShowTrade(string actorId, bool returnToDialog = false);

        /// <summary>Показать диалог с персонажем.</summary>
        /// <param name="actorId">С кем говорят.</param>
        /// <param name="dialogId">Идентификатор диалога.</param>
        void ShowDialog(string actorId, string dialogId);

        /// <summary>Показать доску объявлений хаба или журнал активных заданий.</summary>
        /// <param name="boardLocationId">Экземпляр хаба с доской или <c>null</c> для журнала.</param>
        void ShowQuests(string? boardLocationId);

        /// <summary>Показать дерево талантов.</summary>
        void ShowTalents();

        /// <summary>Показать экран авторов: заголовок игры, идущие титры и выход в стартовое меню.</summary>
        void ShowAuthors();

        /// <summary>Показать модальный диалог выхода.</summary>
        void ShowExitDialog();

        /// <summary>Скрыть модальный диалог выхода.</summary>
        void HideExitDialog();

        /// <summary>Сохранить игру в слот выхода.</summary>
        void SaveExit();

        /// <summary>Показать всплывающее сообщение.</summary>
        /// <param name="text">Текст сообщения.</param>
        void Toast(string text);

        /// <summary>
        /// Показать экран наград: трофеи противника, содержимое тайника, лут объекта или награду за задание.
        /// Предметы попадают в инвентарь только по нажатию игрока.
        /// </summary>
        /// <param name="loot">Предметы, ещё не попавшие в инвентарь.</param>
        /// <param name="money">Деньги награды, уже зачисленные игроку; 0 — не показывать.</param>
        /// <param name="source">Откуда пришла награда: подпись левой колонки.</param>
        /// <param name="onClosed">Что выполнить после закрытия; <c>null</c> — вернуть игровой экран.</param>
        void ShowRewards(
            IReadOnlyList<ItemInstance> loot,
            int money,
            RewardSource source,
            Action? onClosed = null,
            bool ignoreWeight = false);

        /// <summary>
        /// Показать лут объекта локации и сообщать о каждом забранном предмете,
        /// чтобы объект убрал его из своего состава.
        /// </summary>
        /// <param name="loot">Лут, оставшийся в объекте.</param>
        /// <param name="onTaken">Что выполнить с забранным предметом.</param>
        /// <param name="onClosed">Что выполнить после закрытия; <c>null</c> — ничего.</param>
        void ShowLootFromObject(
            IReadOnlyList<ItemInstance> loot,
            Action<ItemInstance> onTaken,
            Action? onClosed = null);

        /// <summary>Выйти из игры.</summary>
        void Quit();

        /// <summary>Запускает корутину, привязанную к объекту интерфейса.</summary>
        /// <param name="routine">Корутина.</param>
        /// <returns>Запущенная корутина.</returns>
        Coroutine RunCoroutine(IEnumerator routine);
    }
}
