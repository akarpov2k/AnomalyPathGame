#nullable enable
using System;

namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>Код завершения цепочки в JSON.</summary>
    public static class ChainCompletionCodec
    {
        /// <summary>Возвращает код завершения цепочки для JSON.</summary>
        /// <param name="completion">Способ завершения.</param>
        /// <returns>Код.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Способ неизвестен.</exception>
        public static string ToCode(ChainCompletion completion) => completion switch
        {
            ChainCompletion.TalkToActor => "talkToActor",
            _ => throw new ArgumentOutOfRangeException(nameof(completion))
        };

        /// <summary>Разбирает код завершения цепочки.</summary>
        /// <param name="code">Код из JSON.</param>
        /// <param name="completion">Разобранный способ.</param>
        /// <returns><c>true</c>, если код известен.</returns>
        public static bool TryParse(string? code, out ChainCompletion completion)
        {
            switch (code)
            {
                case "talkToActor":
                    completion = ChainCompletion.TalkToActor;
                    return true;
                default:
                    completion = default;
                    return false;
            }
        }
    }
}
