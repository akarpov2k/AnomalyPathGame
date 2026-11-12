#nullable enable
namespace AnomalyPath.Core.Localization
{
    /// <summary>Часть класса <see cref="CoreErrors"/>: ошибки мира.</summary>
    public static partial class CoreErrors
    {
        /// <summary>Неизвестный подтип локации.</summary>
        public const string WorldUnknownSubtype = "error.world_unknown_subtype";

        /// <summary>Веса диапазона не покрывают значения или сумма нулевая.</summary>
        public const string WorldWeights = "error.world_weights";

        /// <summary>Роль перехода не встречается ни у одного шаблона.</summary>
        public const string WorldUnknownRole = "error.world_unknown_role";

        /// <summary>В данных нет ни одного шаблона пути.</summary>
        public const string WorldNoPath = "error.world_no_path";

        /// <summary>Диапазон числа веток задан неверно.</summary>
        public const string WorldBranchRange = "error.world_branch_range";

        /// <summary>В хаб можно попасть только с пути.</summary>
        public const string WorldHubAccess = "error.world_hub_access";

        /// <summary>Неизвестный тип локации.</summary>
        public const string WorldUnknownLocationKind = "error.world_unknown_location_kind";

        /// <summary>Неизвестный тег локации.</summary>
        public const string WorldUnknownLocationTag = "error.world_unknown_location_tag";

        /// <summary>Неизвестная редкость локации.</summary>
        public const string WorldUnknownRarity = "error.world_unknown_location_rarity";

        /// <summary>Неизвестный способ перехода.</summary>
        public const string WorldUnknownVia = "error.world_unknown_via";

        /// <summary>Переход ведёт на несуществующую локацию.</summary>
        public const string WorldConnectionTarget = "error.world_connection_target";

        /// <summary>Переход запрещён топологией.</summary>
        public const string WorldTransitionForbidden = "error.world_transition_forbidden";

        /// <summary>Некорректный диапазон.</summary>
        public const string WorldRange = "error.world_range";

        /// <summary>Значение должно быть в диапазоне 0..1.</summary>
        public const string WorldChance = "error.world_chance";

        /// <summary>Нет стоимости для способа перехода.</summary>
        public const string WorldCost = "error.world_cost";

        /// <summary>В данных нет ни одного хаба.</summary>
        public const string WorldNoHub = "error.world_no_hub";

        /// <summary>Для локации нельзя построить маршрут.</summary>
        public const string WorldRouteUnavailable = "error.world_route_unavailable";

        /// <summary>Новый маршрут начинается только из хаба.</summary>
        public const string WorldRouteNotFromHub = "error.world_route_not_from_hub";

        /// <summary>Из цепочки некуда выйти на путь: нет разрешённого перехода.</summary>
        public const string WorldBranchExit = "error.world_branch_exit";

        /// <summary>Локация повторяется цепочкой, но перехода в такой же подтип нет.</summary>
        public const string WorldChainLoop = "error.world_chain_loop";

        /// <summary>Часы мира вне диапазона 0..24.</summary>
        public const string WorldClockHours = "error.world_clock_hours";

        /// <summary>Локация не найдена в сгенерированном мире.</summary>
        public const string WorldInstanceNotFound = "error.world_instance_not_found";

        /// <summary>Уникальная локация должна иметь редкость unique.</summary>
        public const string WorldUniqueRarity = "error.world_unique_rarity";

        /// <summary>Неизвестный вид объекта локации.</summary>
        public const string SpawnUnknownObjectKind = "error.spawn_unknown_object_kind";

        /// <summary>Запись спавна не найдена.</summary>
        public const string SpawnNotFound = "error.spawn_not_found";

        /// <summary>Тег объекта одновременно требуется и запрещён.</summary>
        public const string SpawnTagConflict = "error.spawn_tag_conflict";

        /// <summary>Актёр NPC-объекта должен быть NPC.</summary>
        public const string SpawnActorType = "error.spawn_actor_type";

        /// <summary>objectCount локации вне допустимого диапазона.</summary>
        public const string SpawnObjectCount = "error.spawn_object_count";

        /// <summary>Пул объектов локации содержит повтор.</summary>
        public const string SpawnPoolDuplicate = "error.spawn_pool_duplicate";

        /// <summary>Объект пула не подходит локации по тегам, тиру или подтипу.</summary>
        public const string SpawnPoolIncompatible = "error.spawn_pool_incompatible";

        /// <summary>У хаба не может быть пула объектов.</summary>
        public const string SpawnPoolOnHub = "error.spawn_pool_on_hub";

        /// <summary>Объект-противник должен ссылаться на актёра типа enemy.</summary>
        public const string SpawnEnemyActor = "error.spawn_enemy_actor";

        /// <summary>Тайник не входит в пул объектов локации: у него отдельный бросок.</summary>
        public const string SpawnPoolStash = "error.spawn_pool_stash";

        /// <summary>Запись спавна вида «аномалия» без ссылки на аномалию.</summary>
        public const string SpawnAnomalyRequired = "error.spawn_anomaly_required";

        /// <summary>Аномалия не найдена в данных.</summary>
        public const string AnomalyNotFound = "error.anomaly_not_found";

        /// <summary>У записи спавна аномалии заданы чужие поля.</summary>
        public const string AnomalyFields = "error.anomaly_fields";

        /// <summary>Тир локации не входит в тиры аномалии.</summary>
        public const string SpawnAnomalyTier = "error.spawn_anomaly_tier";

        /// <summary>Тиры аномалии заданы неверно.</summary>
        public const string AnomalyTiers = "error.anomaly_tiers";

        /// <summary>В пуле артефактов аномалии лежит не артефакт.</summary>
        public const string AnomalyPoolItem = "error.anomaly_pool_item";

        /// <summary>Пул артефактов аномалии пуст.</summary>
        public const string AnomalyPoolEmpty = "error.anomaly_pool_empty";

        /// <summary>Тип артефакта пула не совпадает с типом аномалии.</summary>
        public const string AnomalyArtifactType = "error.anomaly_artifact_type";

        /// <summary>Тир хаба задан неверно: меньше 1 или повторяется у двух хабов.</summary>
        public const string WorldHubTier = "error.world_hub_tier";

        /// <summary>Неизвестный вид экрана хаба.</summary>
        public const string WorldUnknownHubView = "error.world_unknown_hub_view";

        /// <summary>Вид экрана хаба задан у локации, которая не является хабом.</summary>
        public const string WorldHubViewNotHub = "error.world_hub_view_not_hub";

        /// <summary>Граница точки на карте мира вне диапазона 0..1.</summary>
        public const string WorldMapSpotRange = "error.world_map_spot_range";

        /// <summary>Границы точки на карте мира заданы в неверном порядке.</summary>
        public const string WorldMapSpotOrder = "error.world_map_spot_order";

        /// <summary>Точка на карте мира задана у локации, которая не является хабом.</summary>
        public const string WorldMapSpotNotHub = "error.world_map_spot_not_hub";

        /// <summary>Предел числа объектов локации больше двух мест сцены.</summary>
        public const string WorldObjectCountMax = "error.world_object_count_max";
    }
}
