#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Talents;
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Talents
{
    /// <summary>
    /// Раскладка дерева талантов по координатам <c>row</c>/<c>column</c>: положение карточек,
    /// связи к предыдущей строке и высота контента. Одна и та же математика для экрана и кадра.
    /// </summary>
    public static class TalentLayout
    {
        /// <summary>Высота контента под все строки дерева.</summary>
        /// <param name="nodes">Таланты дерева.</param>
        /// <returns>Высота в единицах канваса; у пустого дерева 0.</returns>
        public static float ContentHeight(IReadOnlyList<TalentNodeTemplate> nodes)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            var rows = 0;
            foreach (var node in nodes)
            {
                rows = Math.Max(rows, node.Row + 1);
            }

            return rows == 0 ? 0f : (rows * TalentMetrics.RowStep) - TalentMetrics.RowGap;
        }

        /// <summary>Положение левого верхнего угла карточки в контенте дерева.</summary>
        /// <param name="node">Талант.</param>
        /// <returns>Позиция для <c>anchoredPosition</c> при якоре и пивоте в левом верхнем углу.</returns>
        public static Vector2 NodeTopLeft(TalentNodeTemplate node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return new Vector2(
                node.Column * TalentMetrics.ColumnStep,
                -(node.Row * TalentMetrics.RowStep));
        }

        /// <summary>Центр карточки в контенте дерева.</summary>
        /// <param name="node">Талант.</param>
        /// <returns>Координаты центра при якоре в левом верхнем углу.</returns>
        public static Vector2 NodeCenter(TalentNodeTemplate node)
        {
            var topLeft = NodeTopLeft(node);
            return new Vector2(
                topLeft.x + (TalentMetrics.NodeWidth / 2f),
                topLeft.y - (TalentMetrics.NodeHeight / 2f));
        }

        /// <summary>Таланты предыдущей строки, к которым рисуется связь.</summary>
        /// <param name="nodes">Таланты дерева.</param>
        /// <param name="node">Талант, для которого ищутся родители.</param>
        /// <returns>Соседи по колонке из предыдущей строки; если их нет — ближайший.</returns>
        public static List<TalentNodeTemplate> Parents(
            IReadOnlyList<TalentNodeTemplate> nodes,
            TalentNodeTemplate node)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var parents = new List<TalentNodeTemplate>();
            var previousRow = -1;
            foreach (var candidate in nodes)
            {
                if (candidate.Row >= node.Row)
                {
                    continue;
                }

                if (candidate.Row > previousRow)
                {
                    previousRow = candidate.Row;
                    parents.Clear();
                }

                if (candidate.Row == previousRow && Math.Abs(candidate.Column - node.Column) <= 1)
                {
                    parents.Add(candidate);
                }
            }

            if (parents.Count == 0 && previousRow >= 0)
            {
                TalentNodeTemplate? nearest = null;
                var best = int.MaxValue;
                foreach (var candidate in nodes)
                {
                    if (candidate.Row != previousRow)
                    {
                        continue;
                    }

                    var distance = Math.Abs(candidate.Column - node.Column);
                    if (distance < best)
                    {
                        best = distance;
                        nearest = candidate;
                    }
                }

                if (nearest != null)
                {
                    parents.Add(nearest);
                }
            }

            return parents;
        }

        /// <summary>
        /// Колено связи между карточками: вниз от нижней грани родителя, по середине зазора
        /// до колонки потомка и вниз к его верхней грани. Так связи не режут карточки и не
        /// сходятся в одну точку посередине пустой строки.
        /// </summary>
        /// <param name="parent">Карточка выше.</param>
        /// <param name="node">Карточка ниже.</param>
        /// <returns>Отрезки связи: вертикаль, перемычка и вертикаль к потомку.</returns>
        public static List<LinkGeometry> LinkSegments(TalentNodeTemplate parent, TalentNodeTemplate node)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var start = new Vector2(
                NodeCenter(parent).x,
                NodeTopLeft(parent).y - TalentMetrics.NodeHeight);
            var end = new Vector2(
                NodeCenter(node).x,
                NodeTopLeft(node).y);
            var middle = (start.y + end.y) / 2f;
            var segments = new List<LinkGeometry>();
            AddSegment(segments, start, new Vector2(start.x, middle));
            if (Mathf.Abs(start.x - end.x) > 0.01f)
            {
                AddSegment(segments, new Vector2(start.x, middle), new Vector2(end.x, middle));
            }

            AddSegment(segments, new Vector2(end.x, middle), end);
            return segments;
        }

        private static void AddSegment(List<LinkGeometry> segments, Vector2 start, Vector2 end)
        {
            var delta = end - start;
            var length = delta.magnitude;
            if (length <= 0.01f)
            {
                return;
            }

            var center = new Vector2(start.x + (delta.x / 2f), start.y + (delta.y / 2f));
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            segments.Add(new LinkGeometry(center, length, angle));
        }

        /// <summary>Центр, длина и угол полосы связи между карточками.</summary>
        public readonly struct LinkGeometry
        {
            /// <summary>Создаёт геометрию связи.</summary>
            /// <param name="center">Центр полосы в контенте дерева.</param>
            /// <param name="length">Длина полосы.</param>
            /// <param name="angle">Угол поворота в градусах.</param>
            public LinkGeometry(Vector2 center, float length, float angle)
            {
                Center = center;
                Length = length;
                Angle = angle;
            }

            /// <summary>Центр полосы в контенте дерева.</summary>
            public Vector2 Center { get; }

            /// <summary>Длина полосы.</summary>
            public float Length { get; }

            /// <summary>Угол поворота в градусах.</summary>
            public float Angle { get; }
        }
    }
}
