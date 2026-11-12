#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Ui.Core;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Часть класса <see cref="PlayScreen"/>: переходы по маршруту, возврат в хаб,
    /// открытие тайника и сообщения в нижней полосе.
    /// </summary>
    public sealed partial class PlayScreen
    {
        /// <summary>Показывает всплывающее сообщение в нижней полосе.</summary>
        /// <param name="text">Текст сообщения.</param>
        /// <param name="error">Показать сообщение как ошибку.</param>
        public void ShowToast(string text, bool error = false)
        {
            _toast.text = text;
            _toast.color = error ? PlayPalette.Error : UiFactory.ToastColor;
        }

        private void OpenStash() => _host.ShowLockMinigame();

        private void Travel(string edgeId)
        {
            var session = _host.Session;
            if (session == null)
            {
                ShowToast(UiText.Get(_host.Texts, UiTextKey.Common_No_Game), error: true);
                return;
            }

            TravelResult result;
            try
            {
                result = _host.Sessions.Travel(session, edgeId);
            }
            catch (Exception error)
            {
                ShowToast(UiText.Format(_host.Texts, UiTextKey.Game_Travel_Error, error.Message), error: true);
                return;
            }

            if (!result.Succeeded)
            {
                ShowToast(UiText.Get(_host.Texts, PlayText.Travel(result.Status)), error: true);
                return;
            }

            _host.ShowGame(false);
            if (GameFlow.ShowThirst(_host, result.Damage, ShowToast))
            {
                return;
            }

            ShowToast(UiText.Get(_host.Texts, UiTextKey.Game_Travel_Success));
        }

        /// <summary>
        /// Уводит с текущей локации на следующую по маршруту: переход «дальше» — цель
        /// подтипа <see cref="LocationSubtype.Path"/>; если такой один, он и выбирается.
        /// На хабе и без доступного перехода экран просто обновляется на текущей локации.
        /// </summary>
        public void TravelToNextLocation()
        {
            var session = _host.Session;
            if (session == null ||
                session.InCombat ||
                session.State.World.CurrentLocation.Subtype == LocationSubtype.Hub)
            {
                _host.ShowGame(false);
                return;
            }

            var edge = NextEdge(session.State.World, _host.Sessions.AvailableEdges(session));
            if (edge == null)
            {
                _host.ShowGame(false);
                return;
            }

            Travel(edge.EdgeId);
        }

        private static WorldEdge? NextEdge(WorldState world, IReadOnlyList<WorldEdge> edges)
        {
            foreach (var edge in edges)
            {
                if (world.Map.GetLocation(edge.ToId).Subtype == LocationSubtype.Path)
                {
                    return edge;
                }
            }

            return edges.Count == 1 ? edges[0] : null;
        }

        private void ReturnToHub()
        {
            var session = _host.Session;
            if (session == null)
            {
                ShowToast(UiText.Get(_host.Texts, UiTextKey.Common_No_Game), error: true);
                return;
            }

            var result = _host.Sessions.ReturnToHub(session);
            if (!result.Succeeded)
            {
                ShowToast(UiText.Get(_host.Texts, PlayText.Travel(result.Status)), error: true);
                return;
            }

            _host.ShowGame(false);
            if (!GameFlow.ShowThirst(_host, result.Damage, (text, _) => _host.Toast(text)))
            {
                _host.Toast(UiText.Get(_host.Texts, UiTextKey.Game_Travel_Success));
            }
        }
    }
}
