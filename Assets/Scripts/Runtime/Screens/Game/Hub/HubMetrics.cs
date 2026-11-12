#nullable enable
namespace AnomalyPath.Runtime.Screens.Game.Hub
{
    /// <summary>Числа экрана хаба, которые задаёт код: задержка подсказки и размеры подписи.</summary>
    public static class HubMetrics
    {
        /// <summary>Сколько секунд курсор стоит на объекте, прежде чем показать его название.</summary>
        public const float TooltipDelay = 0.6f;

        /// <summary>Отступ подсказки о названии объекта от верхнего края сцены.</summary>
        public const float TooltipTop = 16f;

        /// <summary>Размер шрифта подсказки с названием объекта.</summary>
        public const int TooltipFontSize = 22;

        /// <summary>Во сколько раз крупнее значок при наведении: заметно, но не прыгает.</summary>
        public const float IconHoverScale = 1.08f;
    }
}
