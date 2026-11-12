#nullable enable
namespace AnomalyPath.Core.Dialog
{
    /// <summary>Вариант ответа для показа: доступен ли он и виден ли вообще.</summary>
    public sealed class DialogOptionView
    {
        /// <summary>Создаёт представление варианта.</summary>
        /// <param name="optionId">Идентификатор варианта.</param>
        /// <param name="textKey">Ключ текста.</param>
        /// <param name="available">Доступен ли вариант.</param>
        public DialogOptionView(string optionId, string textKey, bool available)
        {
            OptionId = optionId;
            TextKey = textKey;
            Available = available;
        }

        /// <summary>Идентификатор варианта.</summary>
        public string OptionId { get; }

        /// <summary>Ключ текста.</summary>
        public string TextKey { get; }

        /// <summary>Доступен ли вариант.</summary>
        public bool Available { get; }
    }
}
