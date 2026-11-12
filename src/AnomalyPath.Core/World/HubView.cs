#nullable enable
namespace AnomalyPath.Core.World
{
    /// <summary>
    /// Вид экрана хаба: какой экран показывает мирная зона. Код в данных — имя члена
    /// в нижнем регистре; поле необязательное, по умолчанию <see cref="None"/>.
    /// </summary>
    public enum HubView
    {
        /// <summary>Общий экран локации: фон, сводка и панель действий.</summary>
        None,

        /// <summary>Интерьер хаба: картинка с кликабельными предметами, разговор с торговцами.</summary>
        House
    }
}
