#nullable enable
namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Часть класса <see cref="PlayScreen"/>: отдых. Кнопка «Отдых» открывает диалог с ползунком
    /// часов; в хабе нападений нет, вне хаба отдых может закончиться боем.
    /// </summary>
    public sealed partial class PlayScreen
    {
        private void OpenRest() => GameFlow.TryOpenRest(_host, _host.RestDialog, ConfirmRest, ShowToast);

        private void ConfirmRest(int hours) => GameFlow.Rest(_host, hours, Show, ShowToast);
    }
}
