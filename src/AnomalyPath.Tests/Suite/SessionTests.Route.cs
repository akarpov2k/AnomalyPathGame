#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="SessionTests"/>: возврат в хаб, выход из хаба и переходы по маршруту.</summary>
    public sealed partial class SessionTests
    {
        private void HubReturnTests(ServiceContainer services, IGameSessionService sessions)
        {
            var returnHours = services.Resolve<IGameRulesRepository>().Require().Walk.ReturnMinutes / 60.0;

            Check("сессия: возврат в хаб с первого шага не тратит выносливость", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                var edge = FirstEdgeTo(world, LocationSubtype.Path);
                True(sessions.Travel(session, edge.EdgeId).Succeeded, "переход на путь");
                var stamina = world.Stamina;
                var hours = world.Clock.Hours;

                var result = sessions.ReturnToHub(session);

                True(result.Succeeded, "возврат выполнен");
                Equal(world.LastHubId, world.CurrentLocationId, "снова в хабе");
                Equal(stamina, world.Stamina, "выносливость не тратится");
                Equal(hours + returnHours, world.Clock.Hours, "фиксированные минуты возврата");
            });

            Check("сессия: переход по маршруту не тратит выносливость", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                var edge = FirstEdgeTo(world, LocationSubtype.Path);
                var stamina = world.Stamina;

                True(sessions.Travel(session, edge.EdgeId).Succeeded, "переход");

                Equal(stamina, world.Stamina, "выносливость не тратится");
            });

            Check("сессия: цена возврата не зависит от номера шага", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                True(sessions.Travel(session, FirstEdgeTo(world, LocationSubtype.Path).EdgeId).Succeeded, "шаг 1");
                True(sessions.Travel(session, FirstEdgeTo(world, LocationSubtype.Path).EdgeId).Succeeded, "шаг 2");
                Equal(2, world.CurrentLocation.RouteStep, "номер шага");
                var stamina = world.Stamina;
                var hours = world.Clock.Hours;

                True(sessions.ReturnToHub(session).Succeeded, "возврат выполнен");

                Equal(stamina, world.Stamina, "выносливость не тратится");
                Equal(hours + returnHours, world.Clock.Hours, "фиксированные минуты возврата");
            });

            Check("сессия: возврат с первого шага нового звена тоже фиксированный", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                var guard = 0;
                while (world.CurrentLocation.RouteEdge < 2)
                {
                    world.RestoreStamina(world.StaminaMax);
                    True(sessions.Travel(session, FirstEdgeTo(world, LocationSubtype.Path).EdgeId).Succeeded, "шаг по маршруту");
                    guard++;
                    True(guard < 10, "маршрут не зациклился");
                }

                Equal(2, world.CurrentLocation.RouteEdge, "новое звено");
                Equal(1, world.CurrentLocation.RouteStep, "шаг считается внутри звена");
                var stamina = world.Stamina;
                var hours = world.Clock.Hours;

                True(sessions.ReturnToHub(session).Succeeded, "возврат выполнен");

                Equal(stamina, world.Stamina, "выносливость не тратится");
                Equal(hours + returnHours, world.Clock.Hours, "фиксированные минуты возврата");
            });

            Check("сессия: из здания в хаб теперь можно вернуться", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                var building = FindSubtype(world.Map, LocationSubtype.Building);
                True(building != null, "ветка в здание построена");
                world.MoveTo(building!.InstanceId);
                var hub = world.LastHubId;

                var result = sessions.ReturnToHub(session);

                True(result.Succeeded, "возврат выполнен");
                Equal(hub, world.CurrentLocationId, "снова в хабе");
            });

            Check("сессия: без выносливости в хаб всё равно можно вернуться", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                True(sessions.Travel(session, FirstEdgeTo(world, LocationSubtype.Path).EdgeId).Succeeded, "переход");
                while (world.Stamina > 0)
                {
                    world.TrySpendStamina(1);
                }

                True(sessions.ReturnToHub(session).Succeeded, "возврат не блокируется");
            });

            Check("сессия: выносливость не мешает переходу и возврату", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                True(sessions.Travel(session, FirstEdgeTo(world, LocationSubtype.Path).EdgeId).Succeeded, "переход на путь");
                while (world.Stamina > 0)
                {
                    world.TrySpendStamina(1);
                }

                var edges = sessions.AvailableEdges(session);
                var edge = edges[0];
                foreach (var candidate in edges)
                {
                    if (candidate.Cost.Stamina > 0)
                    {
                        edge = candidate;
                        break;
                    }
                }

                True(sessions.Travel(session, edge.EdgeId).Succeeded, "переход не блокируется");
                True(sessions.ReturnToHub(session).Succeeded, "возврат не блокируется");
            });
        }

        private void LeaveHubTests(ServiceContainer services, IGameSessionService sessions)
        {
            Check("сессия: выход из хаба строит маршрут и ведёт на первую дорогу", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                Equal(0, world.Map.RouteSerial, "номер выхода до начала");

                var result = sessions.LeaveHub(session);

                True(result.Succeeded, "выход выполнен");
                Equal(1, world.Map.RouteSerial, "первый выход");
                Equal(LocationSubtype.Path, world.CurrentLocation.Subtype, "стоим на дороге");
                Equal(1, world.CurrentLocation.RouteEdge, "первое звено");
                Equal(1, world.CurrentLocation.RouteStep, "первый шаг");
                Equal(3, world.RouteEdges, "окно звеньев");
                True(sessions.AvailableEdges(session).Count > 0, "из дороги есть переходы");
            });

            Check("сессия: из хаба назад по ребру не пройти", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                True(sessions.LeaveHub(session).Succeeded, "выход");

                foreach (var edge in sessions.AvailableEdges(session))
                {
                    False(
                        world.Map.GetLocation(edge.ToId).Subtype == LocationSubtype.Hub,
                        "ребро назад в хаб не должно остаться");
                }
            });

            Check("сессия: новый маршрут начинается только из хаба", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                var path = FindSubtype(world.Map, LocationSubtype.Path);
                True(path != null, "на карте есть путь");
                world.MoveTo(path!.InstanceId);

                var result = sessions.LeaveHub(session);

                Equal(TravelStatus.WrongLocation, result.Status, "статус");
                Equal(0, world.Map.RouteSerial, "маршрут не перестраивался");
            });

            Check("сессия: каждый выход из хаба даёт другой маршрут", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                True(sessions.LeaveHub(session).Succeeded, "первый выход");
                var first = RouteSignature(world.Map);

                True(sessions.ReturnToHub(session).Succeeded, "возврат в хаб");
                True(sessions.LeaveHub(session).Succeeded, "второй выход");

                Equal(2, world.Map.RouteSerial, "номер выхода");
                True(RouteSignature(world.Map) != first, "маршрут другой");
            });

            Check("сессия: после перехода пройденная локация снимается с карты", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                True(sessions.LeaveHub(session).Succeeded, "выход");
                var passed = world.CurrentLocation;
                var edge = FirstEdgeTo(world, LocationSubtype.Path);

                True(sessions.Travel(session, edge.EdgeId).Succeeded, "шаг вперёд");

                False(world.Map.ContainsLocation(passed.InstanceId), "пройденная локация снята");
            });

            Check("сессия: на новом звене маршрут достраивается", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var world = session.State.World;
                True(sessions.LeaveHub(session).Succeeded, "выход");
                var before = world.RouteEdges;
                var guard = 0;
                while (world.CurrentLocation.RouteEdge < 2)
                {
                    True(
                        sessions.Travel(session, FirstEdgeTo(world, LocationSubtype.Path).EdgeId).Succeeded,
                        "шаг по маршруту");
                    guard++;
                    True(guard < 10, "маршрут не зациклился");
                }

                True(world.RouteEdges > before, "звенья достроены");
            });
        }

        private static string RouteSignature(WorldMap map)
        {
            var parts = new List<string>();
            foreach (var location in map.Locations)
            {
                if (location.RouteEdge >= 1)
                {
                    parts.Add(location.InstanceId + ":" + location.TemplateId + ":" + location.Seed);
                }
            }

            parts.Sort(StringComparer.Ordinal);
            return string.Join("|", parts);
        }

        private static WorldEdge FirstEdgeTo(WorldState world, LocationSubtype subtype)        {
            foreach (var edge in world.Map.EdgesFrom(world.CurrentLocationId))
            {
                if (world.Map.GetLocation(edge.ToId).Subtype == subtype)
                {
                    return edge;
                }
            }

            throw new InvalidOperationException("Нет ребра к подтипу " + subtype);
        }

        private static LocationInstance? FindSubtype(WorldMap map, LocationSubtype subtype)
        {
            foreach (var location in map.Locations)
            {
                if (location.Subtype == subtype)
                {
                    return location;
                }
            }

            return null;
        }

    }
}
