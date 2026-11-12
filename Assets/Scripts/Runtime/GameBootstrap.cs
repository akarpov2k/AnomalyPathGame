#nullable enable
using System.IO;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Validation;
using UnityEngine;

namespace AnomalyPath.Runtime
{
    /// <summary>
    /// Точка входа в игру: до инициализации проверяет данные и отдаёт готовые сервисы.
    /// Вешается на объект в загрузочной сцене; при ошибках игра не стартует.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Писать сводку проверки данных в консоль")]
        [SerializeField]
        private bool logReport = true;

        /// <summary>Результат проверки данных: отчёт и сервисы.</summary>
        public PreflightResult Preflight { get; private set; } = null!;

        /// <summary>Контейнер сервисов; <c>null</c>, если данные невалидны.</summary>
        public ServiceContainer? Services => Preflight?.Services;

        private void Awake()
        {
            Preflight = Core.Preflight.Run(CreateDataSource());

            if (!Preflight.CanStart)
            {
                Debug.LogError(CoreText.Format(CoreErrors.DataCheckFailed, Preflight.Report.Summary));
                foreach (var issue in Preflight.Report.Errors)
                {
                    Debug.LogError(CoreText.Format(CoreErrors.ReportLine, issue.Path, issue.Message));
                }

                enabled = false;
                return;
            }

            if (logReport)
            {
                Debug.Log($"AnomalyPath: {Preflight.Report.Summary}");
            }

            var saveRoot = Path.Combine(Application.persistentDataPath, "Saves");
            var items = Services!.Resolve<IItemRepository>();
            var saves = new SaveService(
                new FileSaveStorage(saveRoot),
                items,
                Services.Resolve<IGameRulesRepository>(),
                Services.Resolve<ISpawnRepository>(),
                Services.Resolve<IEquipmentService>());
            Services.RegisterSingleton<ISaveService>(saves);
            Services.RegisterSingleton<IGameSessionService>(Core.Bootstrap.CreateSession(
                Services.Resolve<ITravelService>(),
                Services.Resolve<ICombatService>(),
                saves,
                Services.Resolve<INewGameService>(),
                Services.Resolve<IDeathService>(),
                Services.Resolve<ICombatOutcomeService>(),
                Services.Resolve<IQuestService>(),
                Services.Resolve<ITalentService>(),
                Services.Resolve<IProgressionService>(),
                Services.Resolve<IStashService>(),
                Services.Resolve<ILocationObjectService>(),
                Services.Resolve<IWorldGenerator>(),
                Services.Resolve<ITopologyRepository>(),
                Services.Resolve<ICombatAi>(),
                Services.Resolve<IAnomalyService>(),
                Services.Resolve<IArtifactService>(),
                Services.Resolve<IStoryQuestService>(),
                Services.Resolve<ISpawnRepository>(),
                Services.Resolve<IActorRepository>(),
                Services.Resolve<IGameRulesRepository>(),
                Services.Resolve<INeedsService>(),
                Services.Resolve<ILocationRepository>()));
        }

        /// <summary>
        /// Откуда читать данные: в редакторе — папка проекта, в сборке с упакованными данными
        /// (Android) — пакет из ресурсов, иначе — папка <c>StreamingAssets</c> рядом с игрой.
        /// </summary>
        /// <returns>Источник файлов данных для preflight.</returns>
        private static IDataSource CreateDataSource()
        {
#if UNITY_EDITOR
            return new FileSystemDataSource(Path.Combine(Application.dataPath, "Data"));
#else
            var pack = Resources.Load<TextAsset>(DataPack.ResourceName);
            if (pack != null)
            {
                return DataPack.ToSource(pack.text);
            }

            return new FileSystemDataSource(Path.Combine(Application.streamingAssetsPath, "Data"));
#endif
        }
    }
}
