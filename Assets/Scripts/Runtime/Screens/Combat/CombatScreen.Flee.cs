#nullable enable
using AnomalyPath.Runtime.Ui.Core;

namespace AnomalyPath.Runtime.Screens.Combat
{
    /// <summary>Часть класса <see cref="CombatScreen"/>: подтверждение и выполнение побега из боя.</summary>
    public sealed partial class CombatScreen
    {
        /// <summary>Спрашивает подтверждение побега: без него ход противнику не передаётся.</summary>
        private void AskFlee()
        {
            var combat = Host.Session?.Combat;
            if (!IsPlayerTurn(combat))
            {
                return;
            }

            CloseMenus();
            HideTooltip();
            _confirm.Show(
                UiText.Get(Host.Texts, UiTextKey.Common_Confirm_Title),
                UiText.Get(Host.Texts, UiTextKey.Combat_Flee_Confirm),
                UiText.Get(Host.Texts, UiTextKey.Combat_Flee_Confirm_Button),
                Flee);
        }

        /// <summary>
        /// Побег: ход переходит противнику и он атакует. При выживании бой закрывается без
        /// победы, опыта и лута, экран возвращается на локацию, где противник уже исчез.
        /// При смерти бой остаётся завершённым, а итог поражения применяет «Вернуться».
        /// </summary>
        private void Flee()
        {
            var session = Host.Session;
            if (session == null || !Host.Sessions.TryFlee(session, out var result))
            {
                return;
            }

            if (result.Attack is { } attack)
            {
                AddLine(attack.Hit
                    ? UiText.Format(Host.Texts, UiTextKey.Combat_Log_Enemy_Hit, attack.Damage)
                    : UiText.Get(Host.Texts, UiTextKey.Combat_Log_Enemy_Miss));
            }

            if (result.Died)
            {
                AddLine(UiText.Get(Host.Texts, UiTextKey.Combat_Log_Flee_Failed));
                Refresh();
                return;
            }

            AddLine(UiText.Get(Host.Texts, UiTextKey.Combat_Log_Flee));
            Host.ShowGame();
            Host.Toast(UiText.Get(Host.Texts, UiTextKey.Combat_Result_Escaped));
        }
    }
}
