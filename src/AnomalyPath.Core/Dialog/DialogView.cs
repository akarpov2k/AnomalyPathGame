#nullable enable
using System.Collections.Generic;

namespace AnomalyPath.Core.Dialog
{
    /// <summary>Текущий узел диалога для показа: кто говорит, текст и доступные ответы.</summary>
    public sealed class DialogView
    {
        /// <summary>Создаёт представление узла.</summary>
        /// <param name="dialogId">Идентификатор диалога.</param>
        /// <param name="nodeId">Идентификатор узла.</param>
        /// <param name="speakerActorId">Кто говорит или <c>null</c>.</param>
        /// <param name="textKey">Ключ текста.</param>
        /// <param name="options">Видимые варианты ответа.</param>
        public DialogView(
            string dialogId,
            string nodeId,
            string? speakerActorId,
            string textKey,
            IReadOnlyList<DialogOptionView> options)
        {
            DialogId = dialogId;
            NodeId = nodeId;
            SpeakerActorId = speakerActorId;
            TextKey = textKey;
            Options = options;
        }

        /// <summary>Идентификатор диалога.</summary>
        public string DialogId { get; }

        /// <summary>Идентификатор узла.</summary>
        public string NodeId { get; }

        /// <summary>Кто говорит или <c>null</c>.</summary>
        public string? SpeakerActorId { get; }

        /// <summary>Ключ текста.</summary>
        public string TextKey { get; }

        /// <summary>Видимые варианты ответа.</summary>
        public IReadOnlyList<DialogOptionView> Options { get; }
    }
}
