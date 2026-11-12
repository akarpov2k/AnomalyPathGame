#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Trade;

namespace AnomalyPath.Core.Dialog
{
    /// <summary>Диалог: начальный узел, узлы с вариантами ответа и общие условия входа.</summary>
    public sealed class DialogTemplate
    {
        /// <summary>Создаёт диалог.</summary>
        /// <param name="id">Идентификатор диалога.</param>
        /// <param name="startNodeId">Начальный узел.</param>
        /// <param name="nodes">Узлы диалога; не пусто.</param>
        /// <param name="requirements">Условия, при которых диалог доступен.</param>
        /// <exception cref="ArgumentException">Идентификатор или начальный узел пуст.</exception>
        /// <exception cref="ArgumentNullException">Узлы не заданы.</exception>
        public DialogTemplate(
            string id,
            string startNodeId,
            IReadOnlyList<DialogNode> nodes,
            RequirementsTemplate requirements)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(id)),
                    nameof(id));
            }

            Id = id;
            StartNodeId = startNodeId;
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            Requirements = requirements ?? RequirementsTemplate.Empty;
        }

        /// <summary>Идентификатор диалога.</summary>
        public string Id { get; }

        /// <summary>Начальный узел.</summary>
        public string StartNodeId { get; }

        /// <summary>Узлы диалога.</summary>
        public IReadOnlyList<DialogNode> Nodes { get; }

        /// <summary>Условия, при которых диалог доступен.</summary>
        public RequirementsTemplate Requirements { get; }

        /// <summary>Ищет узел по идентификатору.</summary>
        /// <param name="nodeId">Идентификатор узла.</param>
        /// <param name="node">Найденный узел или <c>null</c>.</param>
        /// <returns><c>true</c>, если узел найден.</returns>
        public bool TryGetNode(string nodeId, out DialogNode? node)
        {
            foreach (var item in Nodes)
            {
                if (string.Equals(item.Id, nodeId, StringComparison.Ordinal))
                {
                    node = item;
                    return true;
                }
            }

            node = null;
            return false;
        }

        /// <summary>Читает диалог из записи данных.</summary>
        /// <param name="value">Запись диалога.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Диалог.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static DialogTemplate FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            var startNodeId = DataMap.RequireString(value, "startNodeId", sourcePath);
            var requirements = RequirementsTemplate.Read(value, sourcePath);
            if (!value.TryGet("nodes", out var nodes) || !nodes.IsArray || nodes.Items.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.DialogNodeMissing, id);
            }

            var result = new List<DialogNode>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in nodes.Items)
            {
                if (!item.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                var node = DialogNode.FromJson(item, sourcePath);
                if (!ids.Add(node.Id))
                {
                    throw new DataException(sourcePath, CoreErrors.DialogNodeDuplicate, node.Id);
                }

                result.Add(node);
            }

            if (!ids.Contains(startNodeId))
            {
                throw new DataException(sourcePath, CoreErrors.DialogNodeMissing, startNodeId);
            }

            return new DialogTemplate(id, startNodeId, result, requirements);
        }
    }
}
