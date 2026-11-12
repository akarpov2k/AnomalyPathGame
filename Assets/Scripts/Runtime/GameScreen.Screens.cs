#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AnomalyPath.Runtime
{
    /// <summary>Часть класса <see cref="GameScreen"/>: переключение экранов и модальные окна.</summary>
    public sealed partial class GameScreen
    {
        /// <inheritdoc />
        public void ShowMainMenu()
        {
            Session = null;
            _exitDialog.Hide();
            PlayMenuAudio();
            Router.Show(_mainMenu.Panel);
        }

        /// <inheritdoc />
        public void ShowNewGame()
        {
            _exitDialog.Hide();
            if (Session == null)
            {
                PlayMenuAudio();
            }

            _newGame.Show();
        }

        /// <inheritdoc />
        public void ShowLoad(bool fromGame)
        {
            _exitDialog.Hide();
            if (Session == null)
            {
                PlayMenuAudio();
            }

            _load.Show(fromGame);
        }

        /// <inheritdoc />
        public void ShowSave()
        {
            _exitDialog.Hide();
            if (Session?.InCombat == true)
            {
                return;
            }

            if (Session == null)
            {
                PlayMenuAudio();
            }

            _saveGame.Show();
        }

        /// <inheritdoc />
        public void ShowSettings()
        {
            _exitDialog.Hide();
            if (Session == null)
            {
                PlayMenuAudio();
            }

            _settingsScreen.Show();
        }

        /// <inheritdoc />
        public void ShowKeyBindings()
        {
            _exitDialog.Hide();
            if (Session == null)
            {
                PlayMenuAudio();
            }

            _keyBindings.Show();
        }

        /// <inheritdoc />
        public void ShowGame(bool announce = true)
        {
            _exitDialog.Hide();
            PlayLocationAudio();
            if (Session?.State.World.CurrentLocation.Subtype == LocationSubtype.Hub)
            {
                _bigHub.Show();
            }
            else
            {
                _play.Show();
            }

            if (announce)
            {
                Toast(UiText.Get(Texts, UiTextKey.Game_Started));
            }
        }

        /// <summary>
        /// Есть ли у текущего хаба комната торговца: она открывается только при взаимодействии
        /// с торговцем, а основной экран хаба — общий экран локации.
        /// </summary>
        /// <returns><c>true</c>, если у хаба задан интерьер комнаты торговца.</returns>
        private bool HasTraderRoom()
        {
            var location = Session?.State.World.CurrentLocation;
            if (location == null)
            {
                return false;
            }

            return Locations.Get(location.TemplateId).HubView != HubView.None;
        }

        /// <inheritdoc />
        public void ShowGameMenu()
        {
            _exitDialog.Hide();
            _gameMenu.Show();
        }

        /// <inheritdoc />
        public void ShowMap()
        {
            _exitDialog.Hide();
            _map.Show();
        }

        /// <inheritdoc />
        public void ShowDevelopmentStub()
        {
            _exitDialog.Hide();
            if (_developmentStub != null)
            {
                _developmentStub.Show();
                return;
            }

            Toast(UiText.Get(Texts, UiTextKey.Development_Stub_Message));
        }

        /// <inheritdoc />
        public void ShowInventory()
        {
            _exitDialog.Hide();
            _inventoryScreen.Show();
        }

        /// <inheritdoc />
        public void ShowLockMinigame()
        {
            _exitDialog.Hide();
            _lock.Show();
        }

        /// <inheritdoc />
        public void ShowRadar(AnomalyLocationObject anomaly)
        {
            _exitDialog.Hide();
            _radar.Show(anomaly);
        }

        /// <inheritdoc />
        public void ShowCombat()
        {
            _exitDialog.Hide();
            PlayCombatAudio();
            _combat.Show();
        }

        /// <inheritdoc />
        public void ShowTraderRoom(string actorId)
        {
            _exitDialog.Hide();
            if (HasTraderRoom())
            {
                _hub.Show(actorId);
                return;
            }

            _trade.Show(actorId);
        }

        /// <summary>Показывает окно торговли: товары торговца и инвентарь игрока.</summary>
        /// <param name="actorId">Идентификатор торговца.</param>
        /// <param name="returnToDialog">Вернуть в меню диалога торговца после закрытия окна.</param>
        public void ShowTrade(string actorId, bool returnToDialog = false)
        {
            _exitDialog.Hide();
            _trade.Show(actorId, returnToDialog);
        }

        /// <inheritdoc />
        public void ShowDialog(string actorId, string dialogId)
        {
            _exitDialog.Hide();
            _npcDialog.Show(actorId, dialogId);
        }

        /// <summary>Завершающая фраза диалога NPC уводит на следующую локацию маршрута.</summary>
        private void OnNpcDialogEnded() => _play.TravelToNextLocation();

        /// <inheritdoc />
        public void ShowQuests(string? boardLocationId)
        {
            _exitDialog.Hide();
            if (boardLocationId != null)
            {
                _questBoard.Show(boardLocationId);
                return;
            }

            _questJournal.Show();
        }

        /// <inheritdoc />
        public void ShowTalents()
        {
            _exitDialog.Hide();
            _talents.Show();
        }

        /// <inheritdoc />
        public void ShowAuthors()
        {
            _exitDialog.Hide();
            if (Session == null)
            {
                PlayMenuAudio();
            }

            _authors.Show();
        }

        /// <inheritdoc />
        public void ShowExitDialog() => _exitDialog.Show();

        /// <summary>Показывает лут объекта локации с уведомлением о каждом забранном предмете.</summary>
        /// <param name="loot">Лут, оставшийся в объекте.</param>
        /// <param name="onTaken">Что выполнить с забранным предметом.</param>
        /// <param name="onClosed">Что выполнить после закрытия; <c>null</c> — ничего.</param>
        public void ShowLootFromObject(
            IReadOnlyList<ItemInstance> loot,
            Action<ItemInstance> onTaken,
            Action? onClosed = null)
        {
            if (loot == null || loot.Count == 0)
            {
                return;
            }

            if (Session?.State.Player == null)
            {
                return;
            }

            _rewards.Show(loot, 0, RewardSource.Loot, onClosed, onTaken);
        }

        /// <summary>Показывает экран наград: трофеи, содержимое тайника или награду за задание.</summary>
        /// <param name="loot">Предметы, ещё не попавшие в инвентарь.</param>
        /// <param name="money">Деньги награды, уже зачисленные игроку.</param>
        /// <param name="source">Откуда пришла награда.</param>
        /// <param name="onClosed">Что выполнить после закрытия; <c>null</c> — вернуть игровой экран.</param>
        public void ShowRewards(
            IReadOnlyList<ItemInstance> loot,
            int money,
            RewardSource source,
            Action? onClosed = null,
            bool ignoreWeight = false)
        {
            var items = loot ?? Array.Empty<ItemInstance>();
            if (items.Count == 0 && money <= 0)
            {
                return;
            }

            if (Session?.State.Player == null)
            {
                return;
            }

            _audio.SetMusic(null);
            _rewards.Show(items, money, source, onClosed, ignoreWeight: ignoreWeight);
        }

        /// <inheritdoc />
        public void HideExitDialog() => _exitDialog.Hide();

        /// <inheritdoc />
        public void Toast(string text)
        {
            if (_bigHub != null && _bigHub.Panel.activeInHierarchy)
            {
                _bigHub.ShowToast(text);
                return;
            }

            if (_hub != null && _hub.Panel.activeInHierarchy)
            {
                _hub.ShowToast(text);
                return;
            }

            if (_map != null && _map.Panel.activeInHierarchy)
            {
                _map.ShowToast(text);
                return;
            }

            _play?.ShowToast(text);
        }

        /// <summary>Сохраняет игру в слот выхода; в бою выход не сохраняет.</summary>
        public void SaveExit()
        {
            if (Session == null || Session.InCombat)
            {
                return;
            }

            try
            {
                Sessions.Save(Session, SaveSlots.Exit);
            }
            catch (Exception error)
            {
                Debug.LogError(CoreText.Format(CoreErrors.ExitSaveFailed, error.Message));
            }
        }

        /// <inheritdoc />
        public void Quit() => Application.Quit();

        /// <inheritdoc />
        public Coroutine RunCoroutine(IEnumerator routine) => StartCoroutine(routine);
    }
}
