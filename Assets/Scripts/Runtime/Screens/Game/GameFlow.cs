#nullable enable
using System;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Общий ход действий, одинаковых на игровом экране и в хабе: выход из хаба, отдых
    /// и сообщения о потере здоровья от жажды. Экран передаёт свои обновление и показ сообщений,
    /// поэтому правила и тексты остаются в одном месте.
    /// </summary>
    public static class GameFlow
    {
        /// <summary>Выходит из хаба и ведёт по построенному маршруту.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="refresh">Перерисовать экран после перехода.</param>
        /// <param name="toast">Показать сообщение: текст и признак ошибки.</param>
        /// <exception cref="ArgumentNullException">Хост, обновление или сообщение не заданы.</exception>
        public static void LeaveHub(IScreenHost host, Action refresh, Action<string, bool> toast)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (refresh == null)
            {
                throw new ArgumentNullException(nameof(refresh));
            }

            if (toast == null)
            {
                throw new ArgumentNullException(nameof(toast));
            }

            var session = host.Session;
            if (session == null)
            {
                toast(UiText.Get(host.Texts, UiTextKey.Common_No_Game), true);
                return;
            }

            TravelResult result;
            try
            {
                result = host.Sessions.LeaveHub(session);
            }
            catch (Exception error)
            {
                toast(UiText.Format(host.Texts, UiTextKey.Game_Travel_Error, error.Message), true);
                return;
            }

            if (!result.Succeeded)
            {
                toast(UiText.Get(host.Texts, PlayText.Travel(result.Status)), true);
                return;
            }

            refresh();
            if (ShowThirst(host, result.Damage, toast))
            {
                return;
            }

            toast(UiText.Get(host.Texts, UiTextKey.Game_Travel_Success), false);
        }

        /// <summary>Открывает диалог отдыха, если отдых доступен.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="dialog">Диалог с ползунком часов.</param>
        /// <param name="confirm">Что выполнить с выбранным числом часов.</param>
        /// <param name="toast">Показать сообщение: текст и признак ошибки.</param>
        /// <returns><c>true</c>, если диалог открыт.</returns>
        /// <exception cref="ArgumentNullException">Хост, диалог, подтверждение или сообщение не заданы.</exception>
        public static bool TryOpenRest(
            IScreenHost host,
            RestDialog dialog,
            Action<int> confirm,
            Action<string, bool> toast)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (dialog == null)
            {
                throw new ArgumentNullException(nameof(dialog));
            }

            if (confirm == null)
            {
                throw new ArgumentNullException(nameof(confirm));
            }

            if (toast == null)
            {
                throw new ArgumentNullException(nameof(toast));
            }

            var session = host.Session;
            if (session == null)
            {
                toast(UiText.Get(host.Texts, UiTextKey.Common_No_Game), true);
                return false;
            }

            var quote = host.Sessions.GetRestQuote(session);
            if (!quote.Available)
            {
                toast(UiText.Get(host.Texts, PlayText.Rest(quote.Status)), true);
                return false;
            }

            dialog.Show(quote, confirm);
            return true;
        }

        /// <summary>Отдыхает выбранное число часов; нападение вне хаба открывает бой.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="hours">Сколько часов отдыхать.</param>
        /// <param name="refresh">Перерисовать экран после отдыха.</param>
        /// <param name="toast">Показать сообщение: текст и признак ошибки.</param>
        /// <exception cref="ArgumentNullException">Хост, обновление или сообщение не заданы.</exception>
        public static void Rest(IScreenHost host, int hours, Action refresh, Action<string, bool> toast)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (refresh == null)
            {
                throw new ArgumentNullException(nameof(refresh));
            }

            if (toast == null)
            {
                throw new ArgumentNullException(nameof(toast));
            }

            var session = host.Session;
            if (session == null)
            {
                toast(UiText.Get(host.Texts, UiTextKey.Common_No_Game), true);
                return;
            }

            RestResult result;
            try
            {
                result = host.Sessions.Rest(session, hours);
            }
            catch (Exception error)
            {
                toast(UiText.Format(host.Texts, UiTextKey.Rest_Error, error.Message), true);
                return;
            }

            if (result.Status == RestStatus.Encounter)
            {
                host.ShowCombat();
                return;
            }

            refresh();
            if (result.Succeeded)
            {
                toast(
                    UiText.Format(host.Texts, UiTextKey.Rest_Done, result.StaminaRestored, result.Hours),
                    false);
                return;
            }

            toast(UiText.Get(host.Texts, PlayText.Rest(result.Status)), true);
        }

        /// <summary>Показывает потерю здоровья от жажды после перехода.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="damage">Урон перехода или <c>null</c>, если воды хватило.</param>
        /// <param name="toast">Показать сообщение: текст и признак ошибки.</param>
        /// <returns><c>true</c>, если сообщение показано.</returns>
        /// <exception cref="ArgumentNullException">Хост или сообщение не заданы.</exception>
        public static bool ShowThirst(IScreenHost host, PlayerDamageResult? damage, Action<string, bool> toast)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (toast == null)
            {
                throw new ArgumentNullException(nameof(toast));
            }

            if (damage == null)
            {
                return false;
            }

            if (damage.Died)
            {
                toast(UiText.Get(host.Texts, UiTextKey.Game_Thirst_Death), true);
                return true;
            }

            toast(UiText.Format(host.Texts, UiTextKey.Game_Thirst_Hit, damage.Damage), true);
            return true;
        }
    }
}
