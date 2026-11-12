#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="SessionTests"/>: время прохода и возврат в хаб.</summary>
    public sealed partial class SessionTests
    {
        private void WalkSessionTests(ServiceContainer services, IGameSessionService sessions)
        {
            var rules = services.Resolve<IGameRulesRepository>().Require();

            Check("проход: назад не больше трети пройденного", () =>
            {
                Equal(3.0, rules.Walk.BacktrackFactor, "коэффициент возврата");
                Equal(2.0, rules.Walk.BackLimit(3), "из трёх единиц назад можно на одну");
                Equal(0.0, rules.Walk.BackLimit(0), "без прохода назад нельзя");
            });

            Check("проход: час пути тратит еду и воду и двигает часы", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var player = session.State.Player!;
                var food = player.Needs.Hunger;
                var water = player.Needs.Thirst;
                var hours = session.State.World.Clock.AbsoluteHours;

                var damage = sessions.AdvanceWalk(session, 60);

                Equal(hours + 1, session.State.World.Clock.AbsoluteHours, "час прошёл");
                Equal(food - rules.Needs.FoodPerHour, player.Needs.Hunger, "еда за час");
                Equal(water - rules.Needs.WaterPerHour, player.Needs.Thirst, "вода за час");
                True(damage == null, "жажды нет");
            });

            Check("проход: полчаса копятся и дают час расхода", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var player = session.State.Player!;
                var food = player.Needs.Hunger;

                sessions.AdvanceWalk(session, 30);
                sessions.AdvanceWalk(session, 30);

                Equal(food - rules.Needs.FoodPerHour, player.Needs.Hunger, "еда за два получаса");
            });

            Check("проход: возврат из здания — фиксированные минуты без выносливости", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                var building = FindSubtype(world.Map, LocationSubtype.Building);
                True(building != null, "ветка в здание построена");
                world.MoveTo(building!.InstanceId);
                var hours = world.Clock.AbsoluteHours;
                var stamina = world.Stamina;

                var result = sessions.ReturnToHub(session);

                True(result.Succeeded, "возврат выполнен");
                Equal(
                    hours + (rules.Walk.ReturnMinutes / 60.0),
                    world.Clock.AbsoluteHours,
                    "фиксированное время возврата");
                Equal(stamina, world.Stamina, "выносливость не тратится");
            });

            Check("проход: без выносливости возврат из здания работает", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                var building = FindSubtype(world.Map, LocationSubtype.Building);
                True(building != null, "ветка в здание построена");
                world.MoveTo(building!.InstanceId);
                while (world.Stamina > 0)
                {
                    world.TrySpendStamina(1);
                }

                True(sessions.ReturnToHub(session).Succeeded, "выносливость не блокирует");
            });
        }
    }
}
