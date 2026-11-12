#nullable enable
namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>Экран, который хост привязывает к себе: получает общий хост и хост виджетов.</summary>
    public interface IScreenPanel
    {
        /// <summary>Привязывает панель к хосту.</summary>
        /// <param name="host">Общий хост навигации и общих сервисов.</param>
        /// <param name="widgetHost">Полный хост для виджетов и модальных окон.</param>
        void BindHost(IScreenHost host, IUiHost widgetHost);
    }
}
