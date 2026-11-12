#nullable enable
using AnomalyPath.Runtime.Ui.Core;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Часть класса <see cref="PlayScreen"/>: бой с противником, появившимся на локации.
    /// Противник — фигура по центру сцены; клик открывает бой.
    /// </summary>
    public sealed partial class PlayScreen
    {
        /// <summary>
        /// Начинает бой с противником локации и открывает боевой экран.
        /// Сценарий — обычный бой, игрок ходит первым: засаду и сюжет задаёт сцена, а не объект пула.
        /// </summary>
        /// <param name="actorId">Id актёра типа <c>enemy</c>.</param>
        private void EnterCombat(string actorId)
        {
            var session = _host.Session;
            if (session == null || session.InCombat || !_host.Actors.Contains(actorId))
            {
                Show();
                return;
            }

            var enemy = _host.Actors.Get(actorId);
            var playerActor = _host.Actors.Get(_host.Rules.Require().PlayerActorId);
            if (!_host.Sessions.StartCombat(session, playerActor, enemy))
            {
                ShowToast(UiText.Get(_host.Texts, UiTextKey.Game_Enemy_Failed), error: true);
                Show();
                return;
            }

            _host.ShowCombat();
        }
    }
}
