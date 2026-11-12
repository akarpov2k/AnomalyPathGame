#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Anomalies;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Death;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Действия игровой сессии: сохранение, загрузка, переходы, бой и ходы.</summary>
    public interface IGameSessionService
    {
        /// <summary>Сохраняет состояние сессии в слот.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="slot">Имя слота.</param>
        /// <param name="playtime">Наигранное время в секундах.</param>
        /// <param name="createdAt">Дата создания; по умолчанию текущая.</param>
        /// <param name="title">Название сохранения, заданное игроком.</param>
        /// <exception cref="ArgumentNullException">Сессия не задана.</exception>
        /// <exception cref="InvalidOperationException">Идёт бой: в бою сохраняться нельзя.</exception>
        void Save(
            GameSession session,
            string slot,
            double playtime = 0,
            DateTimeOffset? createdAt = null,
            string? title = null);

        /// <summary>Загружает сессию из слота; бой не восстанавливается.</summary>
        /// <param name="slot">Имя слота.</param>
        /// <returns>Сессия с загруженным состоянием.</returns>
        GameSession Load(string slot);

        /// <summary>Создаёт новую игру.</summary>
        /// <param name="seed">Seed генерации мира.</param>
        /// <param name="loadoutId">Id выбранного набора снаряжения.</param>
        /// <param name="settings">Настройки сложности.</param>
        /// <returns>Новая сессия.</returns>
        GameSession StartNew(int seed, string loadoutId, NewGameSettings settings);

        /// <summary>
        /// Применяет последствия смерти к вещам игрока: инвентарь теряется полностью,
        /// кроме неутрачиваемых предметов, а каждый надетый предмет страхуется отдельным броском.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Что потеряно, что осталось и что спасла страховка.</returns>
        DeathResult ApplyDeath(GameSession session);

        /// <summary>Завершает бой и применяет его последствия: опыт и лут либо смерть и респавн.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Результат боя; повторный вызов на завершённом бое ничего не меняет.</returns>
        CombatOutcome ApplyCombatOutcome(GameSession session);

        /// <summary>Рёбра, видимые из текущей локации; в бою список пуст.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Видимые рёбра.</returns>
        IReadOnlyList<WorldEdge> AvailableEdges(GameSession session);

        /// <summary>Выполняет переход, если он разрешён и игрок не в бою.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="edgeId">Id ребра.</param>
        /// <returns>Результат перехода.</returns>
        TravelResult Travel(GameSession session, string edgeId);

        /// <summary>
        /// Покидает хаб: строит новый маршрут (каждый выход даёт другой) и сразу ведёт игрока
        /// в первую локацию пути первого звена. Доступно только из хаба и вне боя.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Результат перехода; <see cref="TravelStatus.WrongLocation"/>, если игрок не в хабе.</returns>
        TravelResult LeaveHub(GameSession session);

        /// <summary>
        /// Возвращает игрока в последний хаб. Доступно только с пути: цена —
        /// <c>route.hubReturn</c> × <c>routeStep</c> выносливости и часов, переход мгновенный.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Результат возврата.</returns>
        TravelResult ReturnToHub(GameSession session);

        /// <summary>
        /// Предложение перехода между хабами: шаги (разница тиров), цена и шанс встречи.
        /// Ничего не меняет; на карте мира показывает, что будет стоить переход.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="targetHubId">Экземпляр целевого хаба.</param>
        /// <param name="mode">Способ перехода: пешком или с проводником.</param>
        /// <returns>Предложение перехода; недоступный переход объясняет статус.</returns>
        HubTravelQuote GetHubTravelQuote(GameSession session, string targetHubId, HubTravelMode mode);

        /// <summary>
        /// Действующая цена перехода в выносливости с учётом потребностей: при нуле еды
        /// цена увеличивается на долю из правил и округляется вверх.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="baseStamina">Базовая цена в выносливости.</param>
        /// <returns>Цена, которую заплатит игрок.</returns>
        int GetTravelStaminaCost(GameSession session, int baseStamina);

        /// <summary>
        /// Переход между хабами из текущего хаба в любой другой. Пешком списываются часы
        /// и выносливость, затем бросается встреча; с проводником — часы и деньги, без встречи.
        /// Встреча начинает бой на дороге, а прибытие происходит после победы или побега;
        /// поражение возвращает в хаб отправления. Прибытие можно получить после боя.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="targetHubId">Экземпляр целевого хаба.</param>
        /// <param name="mode">Способ перехода: пешком или с проводником.</param>
        /// <returns>Итог перехода: прибытие, начатая встреча или причина отказа.</returns>
        HubTravelResult TravelToHub(GameSession session, string targetHubId, HubTravelMode mode);

        /// <summary>
        /// Предложение отдыха по текущему состоянию: скорость восстановления, предел часов
        /// и риск нападения. Ничего не меняет; основной экран решает по нему, доступна ли кнопка.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Предложение отдыха; недоступный отдых объясняет статус.</returns>
        RestQuote GetRestQuote(GameSession session);

        /// <summary>
        /// Отдыхает выбранное число часов: восстанавливает выносливость, двигает часы мира
        /// и тратит часовые травмы. В хабе нападения нет; вне хаба шанс равен прибавке за час,
        /// умноженной на часы, и одним броском может начать бой после отдыха.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="hours">Сколько часов отдыхать; от 0 до предела предложения.</param>
        /// <returns>Итог отдыха: восстановленная выносливость и возможное нападение.</returns>
        RestResult Rest(GameSession session, int hours);

        /// <summary>
        /// Проверяет появление тайника в текущей локации. Хаб не проверяется, одна локация
        /// проверяется один раз; повторный вызов на уже проверенной локации ничего не делает.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns><c>true</c>, если на локации появился тайник.</returns>
        bool RollLocationStash(GameSession session);

        /// <summary>Вскрывает тайник текущей локации: пул по тиру либо пул джекпота.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Статус, лут и признак джекпота.</returns>
        StashOpenResult OpenStash(GameSession session);

        /// <summary>Состояние кодового замка текущего тайника; <c>null</c> — замок не настроен.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Снимок состояния замка или <c>null</c>.</returns>
        StashLockState? LockState(GameSession session);

        /// <summary>Проверяет введённый код замка текущего тайника.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="guess">Введённый код.</param>
        /// <returns>Состояние замка после попытки; <c>null</c> — замок не настроен.</returns>
        StashLockState? GuessStashLock(GameSession session, string guess);

        /// <summary>Открывает радар аномалии и при первом обращении раскладывает артефакты.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="anomaly">Объект-аномалия на текущей локации.</param>
        /// <returns>Состояние радара или <c>null</c>, если радар не настроен.</returns>
        RadarState? OpenAnomalyRadar(GameSession session, AnomalyLocationObject anomaly);

        /// <summary>Сканирует клетку радара: бесплатно, показывает силу сигнала.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="anomaly">Объект-аномалия.</param>
        /// <param name="cell">Номер клетки.</param>
        /// <returns>Сигнал и остаток сканов.</returns>
        RadarScanResult ScanAnomaly(GameSession session, AnomalyLocationObject anomaly, int cell);

        /// <summary>
        /// Обыскивает клетку аномалии: тратит попытку, наносит урон, при совпадении отдаёт артефакт.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="anomaly">Объект-аномалия.</param>
        /// <param name="cell">Номер клетки.</param>
        /// <returns>Урон, находка и остаток попыток.</returns>
        AnomalySearchResult SearchAnomalyCell(GameSession session, AnomalyLocationObject anomaly, int cell);

        /// <summary>Начинает бой с противником.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="playerActor">Актёр игрока.</param>
        /// <param name="enemy">Противник.</param>
        /// <param name="scenario">Сценарий боя; <c>null</c> — обычный бой, игрок ходит первым.</param>
        /// <param name="context">Контекст боя вне локации; <c>null</c> — обычный бой на локации.</param>
        /// <returns><c>true</c>, если бой начат.</returns>
        bool StartCombat(
            GameSession session,
            ActorTemplate playerActor,
            ActorTemplate enemy,
            CombatScenario? scenario = null,
            CombatEncounterContext? context = null);

        /// <summary>Выполняет атаку текущим участником.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="weapon">Оружие атакующего.</param>
        /// <param name="result">Результат атаки.</param>
        /// <returns><c>true</c>, если атака выполнена.</returns>
        bool TryAttack(GameSession session, ItemInstance weapon, out CombatAttackResult result);

        /// <summary>
        /// Действия игрока в бою: скиллы его активного оружия
        /// с ценой в ОД и патронах и причиной недоступности. Вне боя список пуст.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Действия в порядке показа; пустой список, если боя нет.</returns>
        /// <exception cref="ArgumentNullException">Сессия не задана.</exception>
        IReadOnlyList<CombatAction> GetCombatActions(GameSession session);

        /// <summary>
        /// Применяет скилл активного оружия игрока: обычный выстрел или очередь, если он есть
        /// в данных и доступен. Смерть цели засчитывается квестам.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="skillId">Идентификатор скилла из данных оружия.</param>
        /// <param name="result">Выстрелы, урон и смерть цели.</param>
        /// <returns><c>true</c>, если скилл выполнен; <c>false</c> — боя нет или скилл недоступен.</returns>
        /// <exception cref="ArgumentNullException">Сессия не задана.</exception>
        bool TryUseCombatSkill(GameSession session, string skillId, out CombatSkillResult result);

        /// <summary>Перезаряжает оружие из инвентаря игрока.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="weapon">Оружие.</param>
        /// <returns><c>true</c>, если перезарядка выполнена.</returns>
        bool TryReload(GameSession session, ItemInstance weapon);

        /// <summary>Завершает ход и передаёт его противнику.</summary>
        /// <param name="session">Игровая сессия.</param>
        void EndTurn(GameSession session);

        /// <summary>
        /// Ход противника: ИИ выполняет действия по одному, выбирая каждое следующее
        /// по изменившемуся бою, затем ход возвращается игроку.
        /// Если игрок погиб, ход не передаётся — вызывающий применяет итог боя.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Действия противника; пустой ход, если боя нет или ход не его.</returns>
        EnemyTurnResult RunEnemyTurn(GameSession session);

        /// <summary>
        /// Выполняет одно действие хода противника: ИИ выбирает его заново по текущему
        /// состоянию. Ход не передаётся — вызывающий решает, когда закончить.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="action">Выполненное действие; при <c>false</c> значение не определено.</param>
        /// <returns>
        /// <c>true</c>, если действие выполнено; <c>false</c> — боя нет, ход не противника
        /// или действий больше не осталось.
        /// </returns>
        bool TryRunEnemyTurnStep(GameSession session, out EnemyTurnAction action);

        /// <summary>Меняет активное оружие игрока между основным и запасным слотом за ОД.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns><c>true</c>, если оружие сменено.</returns>
        bool TrySwapWeapon(GameSession session);

        /// <summary>
        /// Достаёт оружие из быстрого слота: оно встаёт в активный слот, прежнее активное оружие —
        /// в этот быстрый слот. Стоит столько же ОД, сколько обычная смена оружия.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="quickIndex">Индекс быстрого слота.</param>
        /// <returns><c>true</c>, если оружие сменено.</returns>
        bool TryUseQuickWeapon(GameSession session, int quickIndex);

        /// <summary>
        /// Проверяет, можно ли применить медицину из быстрого слота прямо сейчас: свой ход,
        /// боевой расходник с зарядом и хватает ОД на <c>consumable.useTime</c>.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="item">Экземпляр расходника из быстрого слота.</param>
        /// <returns><c>true</c>, если применение пройдёт.</returns>
        /// <exception cref="ArgumentNullException">Сессия или предмет не заданы.</exception>
        bool CanUseMedicine(GameSession session, ItemInstance item);

        /// <summary>
        /// Применяет медицину из быстрого слота в бою: только в свой ход, за ОД из
        /// <c>consumable.useTime</c>, тратит одно применение. Лечение остаётся в боевом
        /// состоянии и переносится в игрока итогом боя.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="item">Экземпляр расходника из быстрого слота.</param>
        /// <param name="result">Сколько вылечено, восстановлено ОД, зарядов и травм.</param>
        /// <returns><c>true</c>, если применение состоялось.</returns>
        /// <exception cref="ArgumentNullException">Сессия не задана.</exception>
        bool TryUseMedicine(GameSession session, ItemInstance item, out ConsumableUseResult result);

        /// <summary>
        /// Побег из боя: ход переходит противнику, он атакует, если может; при выживании
        /// игрока бой закрывается без победы, здоровье и износ сохраняются, противник
        /// исчезает с текущей локации. При смерти бой остаётся завершённым — вызывающий
        /// применяет обычный итог поражения.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="result">Исход побега, атака противника и здоровье игрока.</param>
        /// <returns><c>true</c>, если побег выполнен; <c>false</c>, если он невозможен.</returns>
        bool TryFlee(GameSession session, out FleeResult result);

        /// <summary>
        /// Двигает время мира на минуты прохода: травмы, еда и вода, сроки квестов и жажда.
        /// В бою и при неположительных минутах ничего не меняет.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="minutes">Сколько минут мира прошло.</param>
        /// <returns>Урон жажды или <c>null</c>, если воды хватило.</returns>
        /// <exception cref="ArgumentNullException">Сессия не задана.</exception>
        PlayerDamageResult? AdvanceWalk(GameSession session, double minutes);

        /// <summary>Завершает бой без победителя.</summary>
        /// <param name="session">Игровая сессия.</param>
        void EndCombat(GameSession session);
    }
}
