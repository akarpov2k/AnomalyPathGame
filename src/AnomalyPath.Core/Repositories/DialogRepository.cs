#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Dialog;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий диалогов в памяти.</summary>
    public sealed class DialogRepository : IDialogRepository
    {
        private readonly TemplateStore<DialogTemplate> _store;

        /// <summary>Создаёт репозиторий из готовых диалогов.</summary>
        /// <param name="dialogs">Диалоги.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public DialogRepository(IEnumerable<DialogTemplate> dialogs)
        {
            _store = new TemplateStore<DialogTemplate>(
                dialogs,
                dialog => dialog.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.DialogNotFound, id));
        }

        /// <inheritdoc />
        public IReadOnlyList<DialogTemplate> All => _store.All;

        /// <inheritdoc />
        public bool TryGet(string id, out DialogTemplate dialog) => _store.TryGet(id, out dialog!);

        /// <inheritdoc />
        public bool Contains(string id) => _store.Contains(id);

        /// <inheritdoc />
        public DialogTemplate Get(string id) => _store.Get(id);

        /// <summary>Строит репозиторий из загруженных сущностей вида <c>dialogs</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        public static DialogRepository FromLoadedData(LoadedData data) =>
            new DialogRepository(TemplateStore<DialogTemplate>.Load(
                data,
                "dialogs",
                DialogTemplate.FromJson));
    }
}
