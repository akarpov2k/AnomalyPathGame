#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Раскладка объектов-переходов: продолжение пути всегда стоит по центру дороги,
    /// а ответвления — по сторонам (первое справа ниже, второе слева выше). Без продолжения
    /// пути два ответвления стоят по сторонам, третье занимает центр.
    /// </summary>
    public static class PlaySceneSlots
    {
        private static readonly PlaySceneSlot[] BranchOrder =
        {
            PlaySceneSlot.RightLow,
            PlaySceneSlot.LeftHigh,
            PlaySceneSlot.Center
        };

        /// <summary>
        /// Прямоугольник центрального перехода (продолжения пути или единственного): низ по центру
        /// у точки схода дороги, размер — долями сцены. Так объект стоит на дороге, а не висит над ней.
        /// </summary>
        /// <param name="point">Точка схода дороги в единицах сцены, от левого нижнего угла.</param>
        /// <param name="sceneSize">Размер сцены в единицах канваса.</param>
        /// <param name="widthFraction">Ширина перехода, доля ширины сцены.</param>
        /// <param name="heightFraction">Высота перехода, доля высоты сцены.</param>
        /// <returns>Прямоугольник перехода в единицах сцены.</returns>
        public static Rect CenterRect(
            Vector2 point,
            Vector2 sceneSize,
            float widthFraction,
            float heightFraction)
        {
            var width = sceneSize.x * widthFraction;
            var height = sceneSize.y * heightFraction;
            return new Rect(point.x - (width * 0.5f), point.y, width, height);
        }

        /// <summary>Раскладывает переходы по местам сцены.</summary>
        /// <param name="count">Сколько переходов показывать.</param>
        /// <param name="forwardIndex">Номер перехода «дальше по дороге»; <c>-1</c> — такого нет.</param>
        /// <returns>Место для каждого перехода в исходном порядке.</returns>
        public static IReadOnlyList<PlaySceneSlot> Layout(int count, int forwardIndex)
        {
            if (count <= 0)
            {
                return Array.Empty<PlaySceneSlot>();
            }

            if (count == 1)
            {
                return new[] { PlaySceneSlot.Center };
            }

            var hasForward = forwardIndex >= 0 && forwardIndex < count;
            var slots = new PlaySceneSlot[count];
            for (var index = 0; index < count; index++)
            {
                slots[index] = PlaySceneSlot.None;
            }

            if (hasForward)
            {
                slots[forwardIndex] = PlaySceneSlot.Center;
            }

            var branch = 0;
            for (var index = 0; index < count; index++)
            {
                if (slots[index] != PlaySceneSlot.None)
                {
                    continue;
                }

                while (branch < BranchOrder.Length &&
                       BranchOrder[branch] == PlaySceneSlot.Center &&
                       hasForward)
                {
                    branch++;
                }

                slots[index] = branch < BranchOrder.Length ? BranchOrder[branch++] : PlaySceneSlot.Center;
            }

            return slots;
        }
    }
}
