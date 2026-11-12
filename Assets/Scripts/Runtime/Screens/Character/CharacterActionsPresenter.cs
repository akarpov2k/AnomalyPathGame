#nullable enable
using System;
using AnomalyPath.Core.Character;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Runtime.Ui.Core;

namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>Выполняет действия с предметами и сообщает результат на экран.</summary>
    public sealed class CharacterActionsPresenter
    {
        private readonly IUiHost _host;
        private readonly Action<string, bool> _notify;
        private readonly Action _refresh;

        /// <summary>Создаёт исполнитель действий.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="notify">Куда отдать сообщение: текст и признак успеха.</param>
        /// <param name="refresh">Что вызвать после действия.</param>
        public CharacterActionsPresenter(IUiHost host, Action<string, bool> notify, Action refresh)
        {
            _host = host;
            _notify = notify;
            _refresh = refresh;
        }

        /// <summary>Надевает предмет из инвентаря в указанный слот.</summary>
        /// <param name="instanceId">Id экземпляра.</param>
        /// <param name="slot">Слот.</param>
        public void Equip(string instanceId, EquipmentSlot slot) =>
            Run(
                player => _host.Actions.Equip(player.Inventory, player.Equipment, instanceId, slot),
                UiTextKey.Character_Action_Equipped);

        /// <summary>Надевает предмет в подходящий слот (или в столбец артефактов).</summary>
        /// <param name="instanceId">Id экземпляра.</param>
        public void EquipAuto(string instanceId)
        {
            var player = Player;
            if (player == null)
            {
                NotifyNoGame();
                return;
            }

            var item = player.Inventory.Find(instanceId);
            if (item == null)
            {
                Finish(CharacterActionResult.ItemMissing, UiTextKey.Character_Action_Equipped);
                return;
            }

            var template = CharacterWidgets.Template(_host, item);
            if (template != null && !string.IsNullOrWhiteSpace(template.ArtifactType))
            {
                EquipArtifact(instanceId);
                return;
            }

            var slot = _host.Actions.DefaultSlot(player.Equipment, item);
            if (slot == null)
            {
                Finish(CharacterActionResult.WrongSlot, UiTextKey.Character_Action_Equipped);
                return;
            }

            Equip(instanceId, slot.Value);
        }

        /// <summary>Надевает артефакт из инвентаря.</summary>
        /// <param name="instanceId">Id экземпляра.</param>
        public void EquipArtifact(string instanceId) =>
            Run(
                player => _host.Actions.EquipArtifact(player.Inventory, player.Equipment, instanceId),
                UiTextKey.Character_Action_Artifact_Equipped);

        /// <summary>Снимает предмет из слота.</summary>
        /// <param name="slot">Слот.</param>
        public void Unequip(EquipmentSlot slot) =>
            Run(
                player => _host.Actions.Unequip(player.Inventory, player.Equipment, slot),
                UiTextKey.Character_Action_Unequipped);

        /// <summary>Снимает артефакт из слота.</summary>
        /// <param name="index">Индекс слота.</param>
        public void UnequipArtifact(int index) =>
            Run(
                player => _host.Actions.UnequipArtifact(player.Inventory, player.Equipment, index),
                UiTextKey.Character_Action_Unequipped);

        /// <summary>Кладёт предмет в быстрый слот (из сетки или из другого быстрого слота).</summary>
        /// <param name="instanceId">Id экземпляра.</param>
        /// <param name="index">Индекс быстрого слота.</param>
        public void PutQuick(string instanceId, int index) =>
            Run(
                player => _host.Actions.PutQuick(player.Inventory, player.Equipment, instanceId, index),
                UiTextKey.Character_Action_Quick_Assigned);

        /// <summary>Убирает предмет из быстрого слота в инвентарь.</summary>
        /// <param name="index">Индекс быстрого слота.</param>
        public void TakeQuick(int index) =>
            Run(
                player => _host.Actions.TakeQuick(player.Inventory, player.Equipment, index),
                UiTextKey.Character_Action_Quick_Removed);

        /// <summary>Заряжает оружие из инвентаря, начиная с самых маленьких стопок.</summary>
        /// <param name="instanceId">Id экземпляра.</param>
        public void Load(string instanceId)
        {
            var player = Player;
            if (player == null)
            {
                NotifyNoGame();
                return;
            }

            var result = _host.Actions.Load(player.Inventory, player.Equipment, instanceId, out var rounds);
            Finish(result, UiTextKey.Character_Action_Loaded, rounds);
        }

        /// <summary>Заряжает оружие из конкретной стопки патронов.</summary>
        /// <param name="weaponInstanceId">Id оружия.</param>
        /// <param name="ammoInstanceId">Id стопки патронов.</param>
        public void LoadFrom(string weaponInstanceId, string ammoInstanceId)
        {
            var player = Player;
            if (player == null)
            {
                NotifyNoGame();
                return;
            }

            var result = _host.Actions.LoadFrom(
                player.Inventory,
                player.Equipment,
                weaponInstanceId,
                ammoInstanceId,
                out var rounds);
            Finish(result, UiTextKey.Character_Action_Loaded, rounds);
        }

        /// <summary>Разряжает оружие в инвентарь.</summary>
        /// <param name="instanceId">Id экземпляра.</param>
        public void Unload(string instanceId)
        {
            var player = Player;
            if (player == null)
            {
                NotifyNoGame();
                return;
            }

            var result = _host.Actions.Unload(player.Inventory, player.Equipment, instanceId, out var rounds);
            Finish(result, UiTextKey.Character_Action_Unloaded, rounds);
        }

        /// <summary>
        /// Применяет расходник вне боя: медицина лечит здоровье и травмы, еда и вода
        /// восполняют потребности. Не расходник — тихий отказ без сообщения.
        /// </summary>
        /// <param name="instanceId">Id экземпляра.</param>
        public void Use(string instanceId)
        {
            var player = Player;
            if (player == null)
            {
                NotifyNoGame();
                return;
            }

            var item = FindItem(player, instanceId);
            if (item == null)
            {
                Report(UiTextKey.Character_Result_Item_Missing);
                return;
            }

            var template = CharacterWidgets.Template(_host, item);
            if (template?.Consumable == null)
            {
                return;
            }

            if (!_host.Consumables.CanUse(item, inCombat: false))
            {
                Report(UiTextKey.Character_Result_Not_Usable);
                return;
            }

            var result = _host.Consumables.UseOutOfCombat(player, item, CharacterStats.MaxHealth(_host, player));
            if (!result.Succeeded)
            {
                Report(UiTextKey.Character_Result_Nothing_To_Restore);
                return;
            }

            Finish(CharacterActionResult.Ok, UiTextKey.Character_Action_Used);
        }

        /// <summary>Разбирает предмет (тестовый режим).</summary>
        /// <param name="instanceId">Id экземпляра.</param>
        public void Disassemble(string instanceId) =>
            Run(
                player => _host.Actions.Disassemble(player.Inventory, player.Equipment, instanceId),
                UiTextKey.Character_Action_Disassembled);

        /// <summary>Выбрасывает предмет.</summary>
        /// <param name="instanceId">Id экземпляра.</param>
        public void Discard(string instanceId) =>
            Run(
                player => _host.Actions.Discard(player.Inventory, player.Equipment, instanceId),
                UiTextKey.Character_Action_Discarded);

        /// <summary>Перекладывает предмет внутри сетки на указанную клетку.</summary>
        /// <param name="instanceId">Id экземпляра.</param>
        /// <param name="x">Колонка.</param>
        /// <param name="y">Строка.</param>
        /// <param name="rotated">Поворот.</param>
        public void Move(string instanceId, int x, int y, bool rotated)
        {
            var player = Player;
            if (player == null)
            {
                NotifyNoGame();
                return;
            }

            var result = _host.Actions.Move(player.Inventory, instanceId, x, y, rotated);
            if (result == CharacterActionResult.Ok)
            {
                _notify(string.Empty, true);
                _refresh();
                return;
            }

            _notify(MessageFor(result), false);
            _refresh();
        }

        private PlayerState? Player => _host.Session?.State.Player;

        private void NotifyNoGame() => _notify(UiText.Get(_host.Texts, UiTextKey.Common_No_Game), false);

        private void Run(Func<PlayerState, CharacterActionResult> action, UiTextKey okKey)
        {
            var player = Player;
            if (player == null)
            {
                NotifyNoGame();
                return;
            }

            Finish(action(player), okKey);
        }

        private void Finish(CharacterActionResult result, UiTextKey okKey, params object[] args)
        {
            var ok = result == CharacterActionResult.Ok;
            var text = ok
                ? args.Length == 0
                    ? UiText.Get(_host.Texts, okKey)
                    : UiText.Format(_host.Texts, okKey, args)
                : MessageFor(result);
            _notify(text, ok);
            _refresh();
        }

        /// <summary>Показывает сообщение об отказе и обновляет экран.</summary>
        private void Report(UiTextKey key)
        {
            _notify(UiText.Get(_host.Texts, key), false);
            _refresh();
        }

        /// <summary>Ищет экземпляр в сетке, быстрых слотах и слотах снаряжения.</summary>
        private static ItemInstance? FindItem(PlayerState player, string instanceId)
        {
            var inGrid = player.Inventory.Find(instanceId);
            if (inGrid != null)
            {
                return inGrid;
            }

            var equipment = player.Equipment;
            for (var index = 0; index < equipment.QuickSlotCount; index++)
            {
                var quick = equipment.GetQuick(index);
                if (quick != null && string.Equals(quick.InstanceId, instanceId, StringComparison.Ordinal))
                {
                    return quick;
                }
            }

            foreach (var pair in equipment.Slots)
            {
                if (string.Equals(pair.Value.InstanceId, instanceId, StringComparison.Ordinal))
                {
                    return pair.Value;
                }
            }

            return null;
        }

        private string MessageFor(CharacterActionResult result) => result switch
        {
            CharacterActionResult.Ok => UiText.Get(_host.Texts, UiTextKey.Character_Result_Ok),
            CharacterActionResult.ItemMissing => UiText.Get(_host.Texts, UiTextKey.Character_Result_Item_Missing),
            CharacterActionResult.WrongSlot => UiText.Get(_host.Texts, UiTextKey.Character_Result_Wrong_Slot),
            CharacterActionResult.NoRoom => UiText.Get(_host.Texts, UiTextKey.Character_Result_No_Room),
            CharacterActionResult.NotArtifact => UiText.Get(_host.Texts, UiTextKey.Character_Result_Not_Artifact),
            CharacterActionResult.NoFreeArtifactSlot =>
                UiText.Get(_host.Texts, UiTextKey.Character_Result_No_Free_Artifact_Slot),
            CharacterActionResult.NotQuickItem =>
                UiText.Get(_host.Texts, UiTextKey.Character_Result_Not_Quick_Item),
            CharacterActionResult.NotAWeapon => UiText.Get(_host.Texts, UiTextKey.Character_Result_Not_A_Weapon),
            CharacterActionResult.NothingToUnload =>
                UiText.Get(_host.Texts, UiTextKey.Character_Result_Nothing_To_Unload),
            CharacterActionResult.MagazineFull =>
                UiText.Get(_host.Texts, UiTextKey.Character_Result_Magazine_Full),
            CharacterActionResult.NotAmmo =>
                UiText.Get(_host.Texts, UiTextKey.Character_Result_Not_Ammo),
            CharacterActionResult.AmmoCaliberMismatch =>
                UiText.Get(_host.Texts, UiTextKey.Character_Result_Ammo_Caliber_Mismatch),
            CharacterActionResult.AmmoTypeMismatch =>
                UiText.Get(_host.Texts, UiTextKey.Character_Result_Ammo_Type_Mismatch),
            CharacterActionResult.NoAmmo =>
                UiText.Get(_host.Texts, UiTextKey.Character_Result_No_Ammo),
            CharacterActionResult.AmmoTemplateMissing =>
                UiText.Get(_host.Texts, UiTextKey.Character_Result_Ammo_Missing),
            _ => UiText.Get(_host.Texts, UiTextKey.Character_Result_Failed),
        };
    }
}
