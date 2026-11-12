#nullable enable
using System;

namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>Код типа цели сюжетного квеста в JSON: camelCase, а не имя члена перечисления.</summary>
    public static class QuestObjectiveTypeCodec
    {
        /// <summary>Возвращает код типа цели для JSON.</summary>
        /// <param name="type">Тип цели.</param>
        /// <returns>Код.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Тип неизвестен.</exception>
        public static string ToCode(QuestObjectiveType type) => type switch
        {
            QuestObjectiveType.ClearLocations => "clearLocations",
            QuestObjectiveType.KillActors => "killActors",
            QuestObjectiveType.InspectObjects => "inspectObjects",
            QuestObjectiveType.OpenStashes => "openStashes",
            QuestObjectiveType.ClearChain => "clearChain",
            QuestObjectiveType.ReachLocation => "reachLocation",
            QuestObjectiveType.TalkToActor => "talkToActor",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        /// <summary>Разбирает код типа цели.</summary>
        /// <param name="code">Код из JSON.</param>
        /// <param name="type">Разобранный тип.</param>
        /// <returns><c>true</c>, если код известен.</returns>
        public static bool TryParse(string? code, out QuestObjectiveType type)
        {
            switch (code)
            {
                case "clearLocations":
                    type = QuestObjectiveType.ClearLocations;
                    return true;
                case "killActors":
                    type = QuestObjectiveType.KillActors;
                    return true;
                case "inspectObjects":
                    type = QuestObjectiveType.InspectObjects;
                    return true;
                case "openStashes":
                    type = QuestObjectiveType.OpenStashes;
                    return true;
                case "clearChain":
                    type = QuestObjectiveType.ClearChain;
                    return true;
                case "reachLocation":
                    type = QuestObjectiveType.ReachLocation;
                    return true;
                case "talkToActor":
                    type = QuestObjectiveType.TalkToActor;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }
    }
}
