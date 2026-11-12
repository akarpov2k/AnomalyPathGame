#nullable enable
using System;
using AnomalyPath.Core.Player;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>
    /// Состояние персонажа из префаба в духе Таркова: четыре строки «пиктограмма — число»
    /// (здоровье, еда, вода, выносливость) и иконки активных травм под строкой здоровья.
    /// Пиктограммы заданы в префабе, код заполняет только значения.
    /// </summary>
    public sealed class CharacterStatsStrip
    {
        private const float InjuryIcon = 16f;
        private const float InjuryIconStep = 18f;

        private readonly IUiHost _host;
        private readonly Text[] _values;
        private readonly RectTransform _injuries;
        private readonly int _rows;

        /// <summary>Создаёт блок характеристик.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="values">Подписи значений из префаба: здоровье, еда, вода, выносливость.</param>
        /// <param name="injuries">Контейнер иконок травм из префаба.</param>
        public CharacterStatsStrip(IUiHost host, Text[] values, RectTransform injuries)
        {
            _host = host;
            _values = values;
            _injuries = injuries;
            _rows = values.Length;
        }

        /// <summary>Заполняет строки текущим состоянием игрока.</summary>
        /// <param name="player">Состояние игрока.</param>
        public void Build(PlayerState player)
        {
            var world = _host.Session?.State.World;
            var stamina = world?.Stamina ?? 0;
            var staminaMax = CharacterStats.StaminaMax(_host, player);

            var rows = new[]
            {
                (player.Health, CharacterStats.MaxHealth(_host, player)),
                (player.Needs.Hunger, player.Needs.FoodMax),
                (player.Needs.Thirst, player.Needs.WaterMax),
                (stamina, staminaMax)
            };

            var count = Math.Min(_rows, rows.Length);
            for (var index = 0; index < count; index++)
            {
                AddRow(index, rows[index].Item1, rows[index].Item2);
            }

            BuildInjuries(player);
        }

        private void AddRow(int index, int value, int max)
        {
            var safeMax = max < 1 ? 1 : max;
            var clamped = value < 0 ? 0 : value > safeMax ? safeMax : value;
            _values[index].text = clamped + "/" + safeMax;
        }

        /// <summary>
        /// Рисует маленькие иконки активных травм: по одной на травму, слева направо.
        /// Без спрайта остаётся цветная плашка, поэтому травма всё равно видна.
        /// </summary>
        private void BuildInjuries(PlayerState player)
        {
            foreach (Transform child in _injuries)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }

            var injuries = player.Injuries;
            for (var index = 0; index < injuries.Count; index++)
            {
                var injury = injuries[index];
                if (!_host.Injuries.TryGet(injury.Id, out var template))
                {
                    continue;
                }

                var x = index * InjuryIconStep;
                var name = "InjuryIcon_" + injury.Id;
                if (_host.Sprites.TryGet(template.Icon, out var sprite) && sprite != null)
                {
                    UiWidgets.Icon(_host, _injuries, name, sprite, x, 0f, InjuryIcon, InjuryIcon);
                }
                else
                {
                    UiWidgets.Box(_host, _injuries, name, x, 0f, InjuryIcon, InjuryIcon, CharacterPalette.Health);
                }
            }
        }
    }
}
