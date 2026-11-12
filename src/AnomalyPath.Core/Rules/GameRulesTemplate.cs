#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Minigames;

namespace AnomalyPath.Core.Rules
{
    /// <summary>
    /// Правила прохождения из данных: страховка надетых вещей, базовая сетка,
    /// число тиров и настройки новой игры.
    /// </summary>
    public sealed class GameRulesTemplate
    {
        /// <summary>Единственный допустимый id записи правил.</summary>
        public const string IdValue = "rules";

        /// <summary>Создаёт правила прохождения.</summary>
        /// <param name="id">Идентификатор записи, всегда <c>rules</c>.</param>
        /// <param name="insuranceChance">Коэффициент страховки надетой вещи по умолчанию, от 0 до 1.</param>
        /// <param name="insuranceStep">Шаг изменения коэффициента страховки, больше 0.</param>
        /// <param name="baseRows">Высота сетки инвентаря в строках.</param>
        /// <param name="tierCount">Число тиров.</param>
        /// <param name="playerActorId">Актёр игрока для стартовых характеристик.</param>
        /// <param name="gridWidth">Ширина сетки инвентаря.</param>
        /// <param name="baseCarryWeight">Базовый лимит веса инвентаря без рюкзака, кг.</param>
        /// <param name="overloadKg">Разрешённый перегруз сверх лимита веса, кг.</param>
        /// <param name="needsMax">Максимум шкал потребностей.</param>
        /// <param name="startingMoney">Деньги на персонаже на старте.</param>
        /// <param name="tradeDifficulty">Коэффициент усложнения торговли по умолчанию.</param>
        /// <param name="tradeDifficultyMax">Максимум усложнения торговли.</param>
        /// <param name="tradeDifficultyStep">Шаг изменения усложнения торговли.</param>
        /// <param name="nicknameMaxLength">Максимальная длина ника.</param>
        /// <param name="previewMaxRows">Предел рядов в окне предпросмотра набора.</param>
        /// <param name="maxRows">Предел высоты сетки инвентаря при авторасширении, строк.</param>
        /// <param name="savePageSize">Число ячеек сохранения на странице.</param>
        /// <param name="respawnPercent">Доля здоровья и потолка еды/воды после смерти, 0..1.</param>
        /// <param name="xpLossChance">Доля опыта текущего уровня, теряемая при смерти, 0..1.</param>
        /// <param name="xpLossStep">Шаг изменения доли потери опыта, больше 0.</param>
        /// <param name="xpLossMax">Максимум доли потери опыта, 0..1.</param>
        /// <param name="needs">Правила расхода еды и воды и штрафов потребностей.</param>
        /// <param name="maxActiveQuests">Предел одновременно взятых квестов.</param>
        /// <param name="questCooldownHours">Откат повторяемого квеста по умолчанию.</param>
        /// <param name="nightAccuracyPenalty">
        /// Влияние ночи на точность по умолчанию: доля базовой меткости, вычитаемая ночью, 0..1.
        /// </param>
        /// <param name="nightAccuracyPenaltyMax">Максимум влияния ночи в меню новой игры, 0..1.</param>
        /// <param name="nightAccuracyPenaltyStep">Шаг изменения влияния ночи, больше 0.</param>
        /// <param name="mainStoryLine">Линия сюжетных квестов, которая считается основной.</param>
        /// <param name="stashLock">Правила кодового замка тайника; <c>null</c> — без замка.</param>
        /// <param name="anomalyField">Правила радара поиска артефакта; <c>null</c> — без радара.</param>
        /// <param name="audio">Правила звука: музыка меню и боя, громкости и переход; <c>null</c> — без музыки.</param>
        /// <param name="walk">Правила бокового прохода рейда; <c>null</c> — значения по умолчанию.</param>
        /// <param name="sky">Кривая неба по часам; <c>null</c> — значения по умолчанию.</param>
        /// <exception cref="ArgumentException">Строковые поля пусты.</exception>
        public GameRulesTemplate(
            string id,
            double insuranceChance,
            double insuranceStep,
            int baseRows,
            int tierCount,
            string playerActorId,
            int gridWidth,
            double baseCarryWeight,
            int needsMax,
            int startingMoney,
            double tradeDifficulty,
            double tradeDifficultyMax,
            double tradeDifficultyStep,
            int nicknameMaxLength,
            int previewMaxRows,
            int maxRows,
            int savePageSize,
            double respawnPercent,
            double xpLossChance,
            double xpLossStep,
            double xpLossMax,
            NeedsSettings needs,
            string mainStoryLine,
            int maxActiveQuests = 3,
            int questCooldownHours = 48,
            double nightAccuracyPenalty = 0.2,
            double nightAccuracyPenaltyMax = 0.5,
            double nightAccuracyPenaltyStep = 0.05,
            StashLockSettings? stashLock = null,
            AnomalyFieldSettings? anomalyField = null,
            AudioSettings? audio = null,
            WalkSettings? walk = null,
            SkySettings? sky = null,
            double overloadKg = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(id)),
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(playerActorId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(playerActorId)),
                    nameof(playerActorId));
            }

            Id = id;
            InsuranceChance = insuranceChance;
            InsuranceStep = insuranceStep;
            BaseRows = baseRows;
            TierCount = tierCount;
            PlayerActorId = playerActorId;
            GridWidth = gridWidth;
            BaseCarryWeight = baseCarryWeight;
            OverloadKg = overloadKg;
            NeedsMax = needsMax;
            StartingMoney = startingMoney;
            TradeDifficulty = tradeDifficulty;
            TradeDifficultyMax = tradeDifficultyMax;
            TradeDifficultyStep = tradeDifficultyStep;
            NicknameMaxLength = nicknameMaxLength;
            PreviewMaxRows = previewMaxRows;
            MaxRows = maxRows;
            SavePageSize = savePageSize;
            RespawnPercent = respawnPercent;
            XpLossChance = xpLossChance;
            XpLossStep = xpLossStep;
            XpLossMax = xpLossMax;
            Needs = needs ?? throw new ArgumentNullException(nameof(needs));
            MaxActiveQuests = maxActiveQuests;
            QuestCooldownHours = questCooldownHours;
            NightAccuracyPenalty = nightAccuracyPenalty;
            NightAccuracyPenaltyMax = nightAccuracyPenaltyMax;
            NightAccuracyPenaltyStep = nightAccuracyPenaltyStep;
            StashLock = stashLock;
            AnomalyField = anomalyField;
            Audio = audio;
            Walk = walk ?? WalkSettings.Default;
            Sky = sky ?? SkySettings.Default;
            MainStoryLine = string.IsNullOrWhiteSpace(mainStoryLine)
                ? throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(mainStoryLine)),
                    nameof(mainStoryLine))
                : mainStoryLine;
        }

        /// <summary>Идентификатор записи, всегда <c>rules</c>.</summary>
        public string Id { get; }

        /// <summary>
        /// Коэффициент страховки надетой вещи по умолчанию, от 0 до 1.
        /// Применяется к каждому надетому предмету отдельным броском; 0 — не страхуется ничего,
        /// 1 — страхуется всё.
        /// </summary>
        public double InsuranceChance { get; }

        /// <summary>Шаг изменения коэффициента страховки, больше 0.</summary>
        public double InsuranceStep { get; }

        /// <summary>Высота сетки инвентаря в строках.</summary>
        public int BaseRows { get; }

        /// <summary>Число тиров.</summary>
        public int TierCount { get; }

        /// <summary>Актёр игрока для стартовых характеристик.</summary>
        public string PlayerActorId { get; }

        /// <summary>Ширина сетки инвентаря.</summary>
        public int GridWidth { get; }

        /// <summary>Базовый лимит веса инвентаря без рюкзака, килограммы.</summary>
        public double BaseCarryWeight { get; }

        /// <summary>Разрешённый перегруз сверх лимита веса, килограммы; при перегрузе еда и вода тратятся вдвое.</summary>
        public double OverloadKg { get; }

        /// <summary>Максимум шкал потребностей.</summary>
        public int NeedsMax { get; }

        /// <summary>Деньги на персонаже на старте игры.</summary>
        public int StartingMoney { get; }

        /// <summary>Коэффициент усложнения торговли по умолчанию.</summary>
        public double TradeDifficulty { get; }

        /// <summary>Максимум усложнения торговли.</summary>
        public double TradeDifficultyMax { get; }

        /// <summary>Шаг изменения усложнения торговли.</summary>
        public double TradeDifficultyStep { get; }

        /// <summary>Максимальная длина ника.</summary>
        public int NicknameMaxLength { get; }

        /// <summary>Предел рядов в окне предпросмотра набора.</summary>
        public int PreviewMaxRows { get; }

        /// <summary>
        /// Предел высоты сетки инвентаря при авторасширении, строк. Минимум — <see cref="BaseRows"/>;
        /// дальше предметы уходят в переполнение, поэтому лимит по клеткам на практике — предохранитель.
        /// </summary>
        public int MaxRows { get; }

        /// <summary>Число ячеек сохранения на странице.</summary>
        public int SavePageSize { get; }

        /// <summary>Доля здоровья и потолка еды/воды после смерти, 0..1.</summary>
        public double RespawnPercent { get; }

        /// <summary>
        /// Доля опыта, накопленного на текущем уровне, которая теряется при смерти. Потеря
        /// не опускает прогресс ниже нуля и не понижает уровень.
        /// </summary>
        public double XpLossChance { get; }

        /// <summary>Шаг изменения доли потери опыта в меню новой игры, больше 0.</summary>
        public double XpLossStep { get; }

        /// <summary>Максимум доли потери опыта в меню новой игры, 0..1.</summary>
        public double XpLossMax { get; }

        /// <summary>Правила расхода еды и воды и штрафов потребностей.</summary>
        public NeedsSettings Needs { get; }

        /// <summary>Предел одновременно взятых квестов.</summary>
        public int MaxActiveQuests { get; }

        /// <summary>Откат повторяемого квеста по умолчанию в часах мира.</summary>
        public int QuestCooldownHours { get; }

        /// <summary>
        /// Влияние ночи на точность по умолчанию, 0..1: доля базовой меткости, вычитаемая из
        /// суммы меткости атакующего до множителей позиции и скилла.
        /// </summary>
        public double NightAccuracyPenalty { get; }

        /// <summary>Максимум влияния ночи в меню новой игры, 0..1.</summary>
        public double NightAccuracyPenaltyMax { get; }

        /// <summary>Шаг изменения влияния ночи в меню новой игры, больше 0.</summary>
        public double NightAccuracyPenaltyStep { get; }

        /// <summary>
        /// Линия сюжетных квестов, которая считается основной: журнал заданий показывает её
        /// в разделе сюжетных, остальные линии — в разделе побочных.
        /// </summary>
        public string MainStoryLine { get; }

        /// <summary>
        /// Правила кодового замка тайника: число попыток и сложность по тирам;
        /// <c>null</c> — взлом не настроен, тайник открывается сразу.
        /// </summary>
        public StashLockSettings? StashLock { get; }

        /// <summary>
        /// Правила радара поиска артефакта: размер поля, сканы и пороги сигнала по тирам;
        /// <c>null</c> — радар не настроен.
        /// </summary>
        public AnomalyFieldSettings? AnomalyField { get; }

        /// <summary>
        /// Правила звука: музыка меню и боя, громкости эмбиента и музыки и длительность перехода;
        /// <c>null</c> — музыка не настроена, играет только эмбиент локаций.
        /// </summary>
        public AudioSettings? Audio { get; }

        /// <summary>Правила бокового прохода рейда; старые данные получают значения по умолчанию.</summary>
        public WalkSettings Walk { get; }

        /// <summary>Кривая неба по часам; старые данные получают значения по умолчанию.</summary>
        public SkySettings Sky { get; }

        /// <summary>Читает правила из записи данных.</summary>
        /// <param name="value">Запись вида <c>rules</c>.</param>
        /// <param name="path">Путь для сообщения об ошибке.</param>
        /// <returns>Правила прохождения.</returns>
        /// <exception cref="DataException">Поля отсутствуют, значения неизвестны или вне границ.</exception>
        public static GameRulesTemplate FromJson(JsonValue value, string path)
        {
            var id = DataMap.RequireString(value, "id", path);
            if (id != IdValue)
            {
                throw new DataException(path, CoreErrors.RulesId, id);
            }

            var insuranceChance = DataMap.RequireDouble(value, "insuranceChance", path);
            if (insuranceChance < 0 || insuranceChance > 1)
            {
                throw new DataException(path, CoreErrors.RulesChance, "insuranceChance");
            }

            var insuranceStep = DataMap.RequireDouble(value, "insuranceStep", path);
            if (insuranceStep <= 0)
            {
                throw new DataException(path, CoreErrors.PositiveValue, "insuranceStep");
            }

            var baseRows = DataMap.RequireInt(value, "baseRows", path);
            if (baseRows < 1)
            {
                throw new DataException(path, CoreErrors.RulesRange, "baseRows");
            }

            var tierCount = DataMap.RequireInt(value, "tierCount", path);
            if (tierCount < 1)
            {
                throw new DataException(path, CoreErrors.RulesRange, "tierCount");
            }

            var playerActorId = DataMap.RequireString(value, "playerActorId", path);
            var gridWidth = DataMap.RequireInt(value, "gridWidth", path);
            if (gridWidth < 1)
            {
                throw new DataException(path, CoreErrors.RulesRange, "gridWidth");
            }

            var baseCarryWeight = DataMap.RequireDouble(value, "baseCarryWeight", path);
            if (baseCarryWeight <= 0 || double.IsNaN(baseCarryWeight) || double.IsInfinity(baseCarryWeight))
            {
                throw new DataException(path, CoreErrors.PositiveValue, "baseCarryWeight");
            }

            var overloadKg = DataMap.OptionalDouble(value, "overloadKg") ?? 0;
            if (overloadKg < 0 || double.IsNaN(overloadKg) || double.IsInfinity(overloadKg))
            {
                throw new DataException(path, CoreErrors.NonNegative, "overloadKg");
            }

            var needsMax = DataMap.RequireInt(value, "needsMax", path);
            if (needsMax < 1)
            {
                throw new DataException(path, CoreErrors.RulesRange, "needsMax");
            }

            var startingMoney = DataMap.RequireInt(value, "startingMoney", path);
            if (startingMoney < 0)
            {
                throw new DataException(path, CoreErrors.RulesRange, "startingMoney");
            }

            var tradeDifficulty = DataMap.RequireDouble(value, "tradeDifficulty", path);
            if (tradeDifficulty < 0)
            {
                throw new DataException(path, CoreErrors.RulesChance, "tradeDifficulty");
            }

            var tradeDifficultyMax = DataMap.RequireDouble(value, "tradeDifficultyMax", path);
            if (tradeDifficultyMax < tradeDifficulty)
            {
                throw new DataException(path, CoreErrors.RangeOrder, "tradeDifficultyMax");
            }

            var tradeDifficultyStep = DataMap.RequireDouble(value, "tradeDifficultyStep", path);
            if (tradeDifficultyStep <= 0)
            {
                throw new DataException(path, CoreErrors.PositiveValue, "tradeDifficultyStep");
            }

            var nicknameMaxLength = DataMap.RequireInt(value, "nicknameMaxLength", path);
            if (nicknameMaxLength < 1)
            {
                throw new DataException(path, CoreErrors.RulesRange, "nicknameMaxLength");
            }

            var previewMaxRows = DataMap.RequireInt(value, "previewMaxRows", path);
            if (previewMaxRows < 1)
            {
                throw new DataException(path, CoreErrors.RulesRange, "previewMaxRows");
            }

            var maxRows = DataMap.RequireInt(value, "maxRows", path);
            if (maxRows < 1)
            {
                throw new DataException(path, CoreErrors.RulesRange, "maxRows");
            }

            if (maxRows < baseRows)
            {
                throw new DataException(path, CoreErrors.RangeOrder, "maxRows");
            }

            var savePageSize = DataMap.RequireInt(value, "savePageSize", path);
            if (savePageSize < 1)
            {
                throw new DataException(path, CoreErrors.RulesRange, "savePageSize");
            }

            var respawnPercent = DataMap.RequireDouble(value, "respawnPercent", path);
            if (respawnPercent <= 0 || respawnPercent > 1)
            {
                throw new DataException(path, CoreErrors.RulesChance, "respawnPercent");
            }

            var xpLossChance = DataMap.RequireDouble(value, "xpLossChance", path);
            if (xpLossChance < 0 || xpLossChance > 1)
            {
                throw new DataException(path, CoreErrors.RulesChance, "xpLossChance");
            }

            var xpLossStep = DataMap.RequireDouble(value, "xpLossStep", path);
            if (xpLossStep <= 0)
            {
                throw new DataException(path, CoreErrors.PositiveValue, "xpLossStep");
            }

            var xpLossMax = DataMap.RequireDouble(value, "xpLossMax", path);
            if (xpLossMax <= 0 || xpLossMax > 1)
            {
                throw new DataException(path, CoreErrors.RulesChance, "xpLossMax");
            }

            if (xpLossChance > xpLossMax)
            {
                throw new DataException(path, CoreErrors.RangeOrder, "xpLossMax");
            }

            var maxActiveQuests = DataMap.OptionalInt(value, "maxActiveQuests", 3);
            if (maxActiveQuests < 1)
            {
                throw new DataException(path, CoreErrors.RulesRange, "maxActiveQuests");
            }

            var questCooldownHours = DataMap.OptionalInt(value, "questCooldownHours", 48);
            if (questCooldownHours < 0)
            {
                throw new DataException(path, CoreErrors.RulesRange, "questCooldownHours");
            }

            var nightAccuracyPenalty = DataMap.RequireDouble(value, "nightAccuracyPenalty", path);
            if (nightAccuracyPenalty < 0 || nightAccuracyPenalty > 1)
            {
                throw new DataException(path, CoreErrors.RulesChance, "nightAccuracyPenalty");
            }

            var nightAccuracyPenaltyMax = DataMap.RequireDouble(value, "nightAccuracyPenaltyMax", path);
            if (nightAccuracyPenaltyMax <= 0 || nightAccuracyPenaltyMax > 1)
            {
                throw new DataException(path, CoreErrors.RulesChance, "nightAccuracyPenaltyMax");
            }

            if (nightAccuracyPenalty > nightAccuracyPenaltyMax)
            {
                throw new DataException(path, CoreErrors.RangeOrder, "nightAccuracyPenaltyMax");
            }

            var nightAccuracyPenaltyStep = DataMap.RequireDouble(value, "nightAccuracyPenaltyStep", path);
            if (nightAccuracyPenaltyStep <= 0)
            {
                throw new DataException(path, CoreErrors.PositiveValue, "nightAccuracyPenaltyStep");
            }

            if (!value.TryGet("needs", out var needsValue))
            {
                throw new DataException(path, CoreErrors.MissingArrayField, "needs");
            }

            var needs = NeedsSettings.FromJson(needsValue, path);
            var mainStoryLine = DataMap.RequireString(value, "mainStoryLine", path);

            StashLockSettings? stashLock = null;
            if (value.TryGet("stashLock", out var stashLockValue))
            {
                stashLock = StashLockSettings.FromJson(stashLockValue, path);
            }

            AnomalyFieldSettings? anomalyField = null;
            if (value.TryGet("anomalyField", out var anomalyFieldValue))
            {
                anomalyField = AnomalyFieldSettings.FromJson(anomalyFieldValue, path);
            }

            AudioSettings? audio = null;
            if (value.TryGet("audio", out var audioValue))
            {
                audio = AudioSettings.FromJson(audioValue, path);
            }

            WalkSettings? walk = null;
            if (value.TryGet("walk", out var walkValue))
            {
                walk = WalkSettings.FromJson(walkValue, path);
            }

            SkySettings? sky = null;
            if (value.TryGet("sky", out var skyValue))
            {
                sky = SkySettings.FromJson(skyValue, path);
            }

            return new GameRulesTemplate(
                id,
                insuranceChance,
                insuranceStep,
                baseRows,
                tierCount,
                playerActorId,
                gridWidth,
                baseCarryWeight,
                needsMax,
                startingMoney,
                tradeDifficulty,
                tradeDifficultyMax,
                tradeDifficultyStep,
                nicknameMaxLength,
                previewMaxRows,
                maxRows,
                savePageSize,
                respawnPercent,
                xpLossChance,
                xpLossStep,
                xpLossMax,
                needs,
                mainStoryLine,
                maxActiveQuests,
                questCooldownHours,
                nightAccuracyPenalty,
                nightAccuracyPenaltyMax,
                nightAccuracyPenaltyStep,
                stashLock,
                anomalyField,
                audio,
                walk,
                sky,
                overloadKg);
        }
    }
}
