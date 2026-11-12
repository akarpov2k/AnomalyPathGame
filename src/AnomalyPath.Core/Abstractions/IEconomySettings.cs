#nullable enable
namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Правила округления, нижней границы цен и стоимость ремонта из данных.</summary>
    public interface IEconomySettings
    {
        /// <summary>Шаг округления цены, не меньше единицы.</summary>
        int Rounding { get; }

        /// <summary>Минимальная цена, не меньше нуля.</summary>
        int MinPrice { get; }

        /// <summary>Доля цены предмета, которую стоит полный ремонт; больше нуля.</summary>
        double RepairCostFactor { get; }
    }
}
