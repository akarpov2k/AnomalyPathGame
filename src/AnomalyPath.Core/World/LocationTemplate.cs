#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Stage;

namespace AnomalyPath.Core.World
{
    /// <summary>
    /// Авторский шаблон локации: подтип и роль содержания, теги, «длина» цепочки и шанс появления.
    /// </summary>
    public sealed class LocationTemplate
    {
        /// <summary>Создаёт шаблон локации.</summary>
        /// <param name="id">Идентификатор.</param>
        /// <param name="titleKey">Ключ названия.</param>
        /// <param name="locationKind">Тип локации из <see cref="LocationKind"/>.</param>
        /// <param name="subtype">Подтип маршрута из <see cref="LocationSubtype"/>.</param>
        /// <param name="role">Роль содержания внутри подтипа; <c>null</c> — не уточняется.</param>
        /// <param name="tags">Теги совместимости.</param>
        /// <param name="tier">Тир опасности.</param>
        /// <param name="backgrounds">Пул фонов.</param>
        /// <param name="objectCount">Сколько объектов генерировать.</param>
        /// <param name="danger">Сложность от 0 до 1.</param>
        /// <param name="chainLength">Сколько локаций шаблона стоит подряд.</param>
        /// <param name="fixedEvents">События, которые появляются всегда.</param>
        /// <param name="rarity">Редкость локации из <see cref="LocationRarity"/>.</param>
        /// <param name="spawnChance">Шанс появления от 0 до 1.</param>
        /// <param name="unique">Квестовая уникальная локация.</param>
        /// <param name="traders">Id торговцев, привязанных к этому хабу.</param>
        /// <param name="spawnPool">Id записей спавна, из которых локация выбирает объекты.</param>
        /// <param name="questBoard">Доска объявлений хаба или <c>null</c>.</param>
        /// <param name="sceneArt">Id вырезки локации для сцены перехода; <c>null</c> — общий указатель.</param>
        /// <param name="roadArt">Id дороги на экране локации; <c>null</c> — дорога по умолчанию.</param>
        /// <param name="hubView">Вид экрана хаба; по умолчанию общий экран локации.</param>
        /// <param name="worldMapSpot">Точка хаба на карте мира; <c>null</c> — хаба на карте нет.</param>
        /// <param name="ambience">Id эмбиента локации в каталоге звуков; <c>null</c> — локация без эмбиента.</param>
        /// <param name="stage">Сцена локации: точки земли и тон постановки фигур; <c>null</c> — плашка.</param>
        /// <param name="walkBackdrop">Id фона режима похода; <c>null</c> — фон по умолчанию.</param>
        /// <param name="walkPathX">Доля ширины широкого фона, где уже нарисована тропа; <c>null</c> — тропа накладкой.</param>
        /// <param name="interiorId">Id локации-внутренности, куда ведёт дверь этой локации; <c>null</c> — без внутренности.</param>
        /// <param name="walkRoad">Id дороги режима похода под типом локации; <c>null</c> — общая дорога префаба.</param>
        /// <param name="enemyCount">Сколько противников максимум появляется на локации; по умолчанию 3.</param>
        /// <param name="interior">Локация внутри помещения: небо и светила на ней не показываются.</param>
        /// <param name="walkParts">Три переставляемые части фона похода; пусто — фон одним кадром.</param>
        /// <param name="walkPaths">
        /// Для каждой части — доля ширины, где нарисована вертикальная тропа; пусто — центр сегмента.
        /// </param>
        /// <param name="stubAfterQuest">
        /// Id квеста, после завершения которого локация становится заглушкой: переход в неё
        /// не выполняется, игра показывает экран «контент будет позже».
        /// </param>
        /// <exception cref="ArgumentException">Поле пусто, значение перечисления не определено или хаб не совпадает с подтипом.</exception>
        public LocationTemplate(
            string id,
            string titleKey,
            LocationKind locationKind,
            LocationSubtype subtype,
            string? role,
            IReadOnlyList<LocationTag> tags,
            int tier,
            IReadOnlyList<string> backgrounds,
            IntRange objectCount,
            double danger,
            WeightedRange chainLength,
            IReadOnlyList<string> fixedEvents,
            LocationRarity rarity,
            double spawnChance,
            bool unique,
            IReadOnlyList<string>? traders = null,
            IReadOnlyList<string>? spawnPool = null,
            QuestBoardTemplate? questBoard = null,
            string? sceneArt = null,
            string? roadArt = null,
            HubView hubView = HubView.None,
            WorldMapSpot? worldMapSpot = null,
            string? ambience = null,
            LocationStage? stage = null,
            string? walkBackdrop = null,
            double? walkPathX = null,
            string? interiorId = null,
            string? walkRoad = null,
            int enemyCount = 3,
            bool interior = false,
            IReadOnlyList<string>? walkParts = null,
            IReadOnlyList<double>? walkPaths = null,
            string? stubAfterQuest = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(CoreText.Format(CoreErrors.RequiredField, nameof(id)), nameof(id));
            }

            if (!Enum.IsDefined(typeof(LocationKind), locationKind))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.WorldUnknownLocationKind, locationKind),
                    nameof(locationKind));
            }

            if (!Enum.IsDefined(typeof(LocationSubtype), subtype))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.WorldUnknownSubtype, subtype),
                    nameof(subtype));
            }

            if (locationKind == LocationKind.Hub && subtype != LocationSubtype.Hub)
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.WorldHubAccess, id),
                    nameof(locationKind));
            }

            if (locationKind != LocationKind.Hub && subtype == LocationSubtype.Hub)
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.WorldUnknownSubtype, subtype),
                    nameof(subtype));
            }

            if (!Enum.IsDefined(typeof(LocationRarity), rarity))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.WorldUnknownRarity, rarity),
                    nameof(rarity));
            }

            if (tier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tier));
            }

            if (danger < 0 || danger > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(danger));
            }

            if (spawnChance < 0 || spawnChance > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(spawnChance));
            }

            if (walkPathX is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(walkPathX));
            }

            if (enemyCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyCount));
            }

            Id = id;
            TitleKey = titleKey;
            LocationKind = locationKind;
            Subtype = subtype;
            Role = string.IsNullOrWhiteSpace(role) ? null : role;
            Tags = tags;
            Tier = tier;
            Backgrounds = backgrounds;
            ObjectCount = objectCount;
            Danger = danger;
            ChainLength = chainLength ?? throw new ArgumentNullException(nameof(chainLength));
            FixedEvents = fixedEvents;
            Rarity = rarity;
            SpawnChance = spawnChance;
            Unique = unique;
            Traders = traders ?? Array.Empty<string>();
            SpawnPool = spawnPool ?? Array.Empty<string>();
            QuestBoard = questBoard;
            SceneArt = string.IsNullOrWhiteSpace(sceneArt) ? null : sceneArt;
            RoadArt = string.IsNullOrWhiteSpace(roadArt) ? null : roadArt;
            HubView = hubView;
            WorldMapSpot = worldMapSpot;
            Ambience = string.IsNullOrWhiteSpace(ambience) ? null : ambience;
            Stage = stage;
            WalkBackdrop = string.IsNullOrWhiteSpace(walkBackdrop) ? null : walkBackdrop;
            WalkPathX = walkPathX;
            InteriorId = string.IsNullOrWhiteSpace(interiorId) ? null : interiorId;
            WalkRoad = string.IsNullOrWhiteSpace(walkRoad) ? null : walkRoad;
            EnemyCount = enemyCount;
            Interior = interior;
            WalkParts = walkParts ?? Array.Empty<string>();
            WalkPaths = walkPaths ?? Array.Empty<double>();
            StubAfterQuest = string.IsNullOrWhiteSpace(stubAfterQuest) ? null : stubAfterQuest;
            foreach (var part in WalkParts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    throw new ArgumentException(
                        CoreText.Format(CoreErrors.RequiredField, "walkParts"),
                        nameof(walkParts));
                }
            }

            if (WalkParts.Count > 0 && WalkParts.Count != 3)
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.WorldRange, "walkParts"),
                    nameof(walkParts));
            }

            if (WalkPaths.Count > 0 && WalkPaths.Count != 3)
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.WorldRange, "walkPaths"),
                    nameof(walkPaths));
            }

            foreach (var path in WalkPaths)
            {
                if (path is < 0 or > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(walkPaths));
                }
            }
        }

        /// <summary>Идентификатор.</summary>
        public string Id { get; }

        /// <summary>Ключ названия.</summary>
        public string TitleKey { get; }

        /// <summary>Тип локации.</summary>
        public LocationKind LocationKind { get; }

        /// <summary>Подтип маршрута.</summary>
        public LocationSubtype Subtype { get; }

        /// <summary>Роль содержания внутри подтипа; <c>null</c> — не уточняется.</summary>
        public string? Role { get; }

        /// <summary>Теги совместимости.</summary>
        public IReadOnlyList<LocationTag> Tags { get; }

        /// <summary>Тир опасности.</summary>
        public int Tier { get; }

        /// <summary>Пул фонов.</summary>
        public IReadOnlyList<string> Backgrounds { get; }

        /// <summary>Сколько объектов генерировать.</summary>
        public IntRange ObjectCount { get; }

        /// <summary>Сложность от 0 до 1.</summary>
        public double Danger { get; }

        /// <summary>Сколько локаций шаблона стоит подряд.</summary>
        public WeightedRange ChainLength { get; }

        /// <summary>События, которые появляются всегда.</summary>
        public IReadOnlyList<string> FixedEvents { get; }

        /// <summary>Редкость локации.</summary>
        public LocationRarity Rarity { get; }

        /// <summary>Шанс появления от 0 до 1.</summary>
        public double SpawnChance { get; }

        /// <summary>Квестовая уникальная локация.</summary>
        public bool Unique { get; }

        /// <summary>Id торговцев, привязанных к хабу.</summary>
        public IReadOnlyList<string> Traders { get; }

        /// <summary>Пул записей спавна: какие объекты могут появиться именно на этой локации.</summary>
        public IReadOnlyList<string> SpawnPool { get; }

        /// <summary>Доска объявлений хаба или <c>null</c>, если её нет.</summary>
        public QuestBoardTemplate? QuestBoard { get; }

        /// <summary>Id вырезки локации для сцены перехода; <c>null</c> — общий указатель.</summary>
        public string? SceneArt { get; }

        /// <summary>Id дороги на экране локации; <c>null</c> — дорога по умолчанию.</summary>
        public string? RoadArt { get; }

        /// <summary>Вид экрана хаба: интерьер или общий экран локации.</summary>
        public HubView HubView { get; }

        /// <summary>Точка хаба на карте мира; <c>null</c> — хаба на карте нет.</summary>
        public WorldMapSpot? WorldMapSpot { get; }

        /// <summary>Id эмбиента локации в каталоге звуков; <c>null</c> — локация без эмбиента.</summary>
        public string? Ambience { get; }

        /// <summary>Сцена локации: точки земли и тон постановки фигур; <c>null</c> — прежняя плашка.</summary>
        public LocationStage? Stage { get; }

        /// <summary>Id фона режима похода; <c>null</c> — фон по умолчанию (дорога локации).</summary>
        public string? WalkBackdrop { get; }

        /// <summary>Доля ширины широкого фона, где уже нарисована тропа; <c>null</c> — тропа накладкой.</summary>
        public double? WalkPathX { get; }

        /// <summary>Id локации-внутренности, куда ведёт дверь; <c>null</c> — без внутренности.</summary>
        public string? InteriorId { get; }

        /// <summary>Id дороги режима похода под тип локации; <c>null</c> — общая дорога префаба.</summary>
        public string? WalkRoad { get; }

        /// <summary>Сколько противников максимум появляется на локации.</summary>
        public int EnemyCount { get; }

        /// <summary>Три переставляемые части фона похода; пусто — фон одним кадром.</summary>
        public IReadOnlyList<string> WalkParts { get; }

        /// <summary>
        /// Доля ширины каждой части, где нарисована вертикальная тропа от нижней дороги;
        /// пусто — переход ставится в центр сегмента.
        /// </summary>
        public IReadOnlyList<double> WalkPaths { get; }

        /// <summary>Локация внутри помещения: небо и светила на ней не показываются.</summary>
        public bool Interior { get; }

        /// <summary>
        /// Id квеста, после завершения которого локация становится заглушкой: переход сюда
        /// не выполняется, игра показывает экран «Игра в стадии разработки».
        /// <c>null</c> — локация доступна всегда.
        /// </summary>
        public string? StubAfterQuest { get; }

        /// <summary>Мирная зона: в неё можно попасть только с пути.</summary>
        public bool IsHub => Subtype == LocationSubtype.Hub;

        /// <summary>Читает шаблон локации из записи данных.</summary>
        /// <param name="value">Загруженная запись.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Шаблон локации.</returns>
        /// <exception cref="DataException">Поля отсутствуют, значения неизвестны или вне границ.</exception>
        public static LocationTemplate FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            var titleKey = DataMap.RequireString(value, "titleKey", sourcePath);

            var locationKindCode = DataMap.RequireString(value, "locationKind", sourcePath);
            if (!SchemaEnum.TryParse(locationKindCode, out LocationKind locationKind))
            {
                throw new DataException(sourcePath, CoreErrors.WorldUnknownLocationKind, locationKindCode);
            }

            var subtypeCode = DataMap.RequireString(value, "subtype", sourcePath);
            if (!SchemaEnum.TryParse(subtypeCode, out LocationSubtype subtype))
            {
                throw new DataException(sourcePath, CoreErrors.WorldUnknownSubtype, subtypeCode);
            }

            var role = DataMap.OptionalString(value, "role");

            var tags = ReadTags(value, sourcePath);
            var backgrounds = DataMap.OptionalStringArray(value, "backgrounds");
            if (backgrounds.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "backgrounds");
            }

            var tier = DataMap.OptionalInt(value, "tier", 0);
            if (tier < 0)
            {
                throw new DataException(sourcePath, CoreErrors.WorldRange, "tier");
            }

            var objectCount = IntRange.Optional(value, "objectCount", sourcePath, new IntRange(0, 0));
            var danger = DataMap.OptionalDouble(value, "danger") ?? 0;
            if (danger < 0 || danger > 1)
            {
                throw new DataException(sourcePath, CoreErrors.WorldChance, "danger");
            }

            var chainLength = WeightedRange.Optional(
                value,
                "chainLength",
                "chainWeights",
                sourcePath,
                new IntRange(1, 1));
            if (chainLength.Range.Min < 1)
            {
                throw new DataException(sourcePath, CoreErrors.WorldRange, "chainLength");
            }

            var fixedEvents = DataMap.OptionalStringArray(value, "fixedEvents");

            var rarity = LocationRarity.Common;
            var rarityCode = DataMap.OptionalString(value, "rarity");
            if (rarityCode != null && !SchemaEnum.TryParse(rarityCode, out rarity))
            {
                throw new DataException(sourcePath, CoreErrors.WorldUnknownRarity, rarityCode);
            }

            var spawnChance = DataMap.OptionalDouble(value, "spawnChance") ?? 1;
            if (spawnChance < 0 || spawnChance > 1)
            {
                throw new DataException(sourcePath, CoreErrors.WorldChance, "spawnChance");
            }

            var unique = DataMap.OptionalBool(value, "unique", false);
            if (unique && rarity != LocationRarity.Unique)
            {
                throw new DataException(sourcePath, CoreErrors.WorldUniqueRarity, id);
            }

            var traders = DataMap.OptionalStringArray(value, "traders");
            var spawnPool = DataMap.OptionalStringArray(value, "spawnPool");
            var sceneArt = DataMap.OptionalString(value, "sceneArt");
            var roadArt = DataMap.OptionalString(value, "roadArt");
            var ambience = DataMap.OptionalString(value, "ambience");
            var walkBackdrop = DataMap.OptionalString(value, "walkBackdrop");
            var walkPathX = DataMap.OptionalDouble(value, "walkPathX");
            var interiorId = DataMap.OptionalString(value, "interiorId");
            var walkRoad = DataMap.OptionalString(value, "walkRoad");
            var enemyCount = DataMap.OptionalInt(value, "enemyCount", 3);
            var interior = DataMap.OptionalBool(value, "interior", false);
            var walkParts = DataMap.OptionalStringArray(value, "walkParts");
            var walkPaths = DataMap.OptionalDoubleArray(value, "walkPaths", sourcePath);
            var stubAfterQuest = DataMap.OptionalString(value, "stubAfterQuest");

            foreach (var part in walkParts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    throw new DataException(sourcePath, CoreErrors.MissingStringField, "walkParts");
                }
            }

            if (walkParts.Count > 0 && walkParts.Count != 3)
            {
                throw new DataException(sourcePath, CoreErrors.WorldRange, "walkParts");
            }

            if (walkPaths.Count > 0 && walkPaths.Count != 3)
            {
                throw new DataException(sourcePath, CoreErrors.WorldRange, "walkPaths");
            }

            foreach (var path in walkPaths)
            {
                if (path is < 0 or > 1)
                {
                    throw new DataException(sourcePath, CoreErrors.WorldRange, "walkPaths");
                }
            }

            var hubView = HubView.None;            var hubViewCode = DataMap.OptionalString(value, "hubView");
            if (hubViewCode != null && !SchemaEnum.TryParse(hubViewCode, out hubView))
            {
                throw new DataException(sourcePath, CoreErrors.WorldUnknownHubView, hubViewCode);
            }

            WorldMapSpot? worldMapSpot = null;
            if (value.TryGet("worldMap", out var spotValue) && spotValue.Kind != JsonKind.Null)
            {
                if (!spotValue.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                worldMapSpot = WorldMapSpot.FromJson(spotValue, sourcePath);
            }
            QuestBoardTemplate? questBoard = null;
            if (value.TryGet("questBoard", out var boardValue) && boardValue.Kind != JsonKind.Null)
            {
                if (!boardValue.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                questBoard = QuestBoardTemplate.FromJson(boardValue, sourcePath);
            }

            LocationStage? stage = null;
            if (value.TryGet("stage", out var stageValue) && stageValue.Kind != JsonKind.Null)
            {
                if (!stageValue.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                stage = LocationStage.FromJson(stageValue, sourcePath);
            }

            var poolIds = new List<string>(spawnPool.Count);
            foreach (var spawnId in spawnPool)
            {
                if (string.IsNullOrWhiteSpace(spawnId))
                {
                    throw new DataException(sourcePath, CoreErrors.MissingStringField, "spawnPool");
                }

                if (poolIds.Contains(spawnId))
                {
                    throw new DataException(sourcePath, CoreErrors.SpawnPoolDuplicate, spawnId);
                }

                poolIds.Add(spawnId);
            }

            try
            {
                return new LocationTemplate(
                    id,
                    titleKey,
                    locationKind,
                    subtype,
                    role,
                    tags,
                    tier,
                    backgrounds,
                    objectCount,
                    danger,
                    chainLength,
                    fixedEvents,
                    rarity,
                    spawnChance,
                    unique,
                    traders,
                    poolIds,
                    questBoard,
                    sceneArt,
                    roadArt,
                    hubView,
                    worldMapSpot,
                    ambience,
                    stage,
                    walkBackdrop,
                    walkPathX,
                    interiorId,
                    walkRoad,
                    enemyCount,
                    interior,
                    walkParts,
                    walkPaths,
                    stubAfterQuest);
            }
            catch (ArgumentException error)
            {
                throw new DataException(sourcePath, CoreErrors.WorldUnknownSubtype, error.Message);
            }
        }

        private static IReadOnlyList<LocationTag> ReadTags(JsonValue value, string sourcePath)
        {
            var result = new List<LocationTag>();
            foreach (var code in DataMap.OptionalStringArray(value, "tags"))
            {
                if (!SchemaEnum.TryParse(code, out LocationTag tag))
                {
                    throw new DataException(sourcePath, CoreErrors.WorldUnknownLocationTag, code);
                }

                result.Add(tag);
            }

            return result;
        }
    }
}
