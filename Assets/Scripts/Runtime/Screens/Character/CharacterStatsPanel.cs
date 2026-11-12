#nullable enable
using System;
using System.Globalization;
using AnomalyPath.Core.Player;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>
    /// Панель защиты на экране персонажа: строка на каждый тип урона с учётом базы актёра,
    /// брони, шлема и артефактов. Строки строятся из неактивного образца префаба, поэтому
    /// новый тип урона появляется сам, а снятая вещь сразу меняет числа при следующем заполнении.
    /// </summary>
    public sealed class CharacterStatsPanel
    {
        private readonly IUiHost _host;
        private readonly RectTransform _panel;
        private readonly RectTransform _defenseRows;
        private readonly RectTransform _rowTemplate;

        /// <summary>Создаёт панель защиты.</summary>
        /// <param name="host">Хост интерфейса: тексты, защита и типы урона.</param>
        /// <param name="panel">Панель из префаба: её высоту считает собственный ContentSizeFitter.</param>
        /// <param name="defenseRows">Контейнер строк защиты из префаба.</param>
        /// <param name="rowTemplate">Неактивный образец строки из префаба.</param>
        /// <exception cref="ArgumentNullException">Не передан хост или один из контейнеров.</exception>
        public CharacterStatsPanel(
            IUiHost host,
            RectTransform panel,
            RectTransform defenseRows,
            RectTransform rowTemplate)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _defenseRows = defenseRows ?? throw new ArgumentNullException(nameof(defenseRows));
            _rowTemplate = rowTemplate ?? throw new ArgumentNullException(nameof(rowTemplate));
        }

        /// <summary>Заполняет панель текущей защитой игрока по типам урона.</summary>
        /// <param name="player">Состояние игрока; берётся ради экипировки в состоянии сессии.</param>
        /// <exception cref="ArgumentNullException">Состояние игрока не передано.</exception>
        public void Build(PlayerState player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            Clear();
            var state = _host.Session?.State;
            foreach (var damageType in _host.DamageTypes.All)
            {
                var value = state == null ? 0 : _host.Damage.Protection(state, damageType.Id);
                Add(_defenseRows, damageType.Id, _host.Texts.Get(damageType.NameKey),
                    value.ToString(CultureInfo.InvariantCulture));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);
        }

        /// <summary>Убирает строки: игра не начата или экран очищается.</summary>
        public void Clear()
        {
            foreach (Transform child in _defenseRows)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private void Add(RectTransform container, string code, string label, string value)
        {
            var row = UnityEngine.Object.Instantiate(_rowTemplate, container);
            row.name = "StatRow_" + code;
            row.gameObject.SetActive(true);

            var labelText = row.Find("Label")?.GetComponent<Text>();
            var valueText = row.Find("Value")?.GetComponent<Text>();
            if (labelText == null || valueText == null)
            {
                Debug.LogError("AnomalyPath: stats row has no Label and Value texts", row);
                return;
            }

            labelText.name = "StatLabel_" + code;
            labelText.text = label;
            valueText.name = "StatValue_" + code;
            valueText.text = value;
        }
    }
}
