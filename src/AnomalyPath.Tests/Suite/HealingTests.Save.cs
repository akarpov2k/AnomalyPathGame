#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Save;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="HealingTests"/>: заряды расходника в сейве.</summary>
    public sealed partial class HealingTests
    {
        private void SaveTests(IItemRepository items, ServiceContainer services)
        {
            Check("сейв: заряды расходника переживают запись", () =>
            {
                var json = new ItemSaveData("med", "itm_medkit_normal", 1, null, null, null, null, 1).ToJson();
                var restored = ItemSaveData.FromJson(json, "save");

                Equal(1, restored.Charges ?? -1, "заряды");
            });

            Check("сейв: заряды переживают снимок игрока", () =>
            {
                var player = NewPlayer(50);
                AddToInventory(player, items, "itm_splint");
                var snapshot = PlayerSaveMapper.ToSaveData(player);
                var restored = PlayerSaveMapper.FromSaveData(snapshot, items, "save");
                var item = restored.Inventory.Find("inst_itm_splint");

                True(item != null, "предмет на месте");
                Equal(1, item!.Charges ?? -1, "заряды");
            });
        }
    }
}
