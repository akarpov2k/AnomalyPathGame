#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий скиллов в памяти: строится один раз при загрузке данных.</summary>
    public sealed class SkillRepository : ISkillRepository
    {
        private readonly TemplateStore<SkillTemplate> _store;

        /// <summary>Создаёт репозиторий из готовых скиллов.</summary>
        /// <param name="skills">Скиллы.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public SkillRepository(IEnumerable<SkillTemplate> skills)
        {
            _store = new TemplateStore<SkillTemplate>(
                skills,
                skill => skill.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.SkillNotFound, id));
        }

        /// <inheritdoc />
        public IReadOnlyList<SkillTemplate> All => _store.All;

        /// <inheritdoc />
        public bool Contains(string id) => _store.Contains(id);

        /// <inheritdoc />
        public SkillTemplate Get(string id) => _store.Get(id);

        /// <inheritdoc />
        public bool TryGet(string id, out SkillTemplate? skill) => _store.TryGet(id, out skill);

        /// <summary>Строит репозиторий из загруженных сущностей вида <c>skills</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        /// <exception cref="DataException">Сущность не соответствует схеме скилла.</exception>
        public static SkillRepository FromLoadedData(LoadedData data) =>
            new SkillRepository(TemplateStore<SkillTemplate>.Load(data, "skills", SkillTemplate.FromJson));
    }
}
